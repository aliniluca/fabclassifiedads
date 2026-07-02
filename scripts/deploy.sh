#!/usr/bin/env bash
#
# deploy.sh — one-command build & (re)deploy for AiciGăsești.
#
# Idempotent: run it the first time to set up the service, and every time
# afterwards to ship a new version. It clones or pulls the repo, publishes the
# .NET app, fixes permissions, (re)installs the systemd unit and restarts.
#
# The SQLite database and user uploads are preserved across deploys.
#
# Usage:
#   sudo ./scripts/deploy.sh                 # uses the defaults below
#   BRANCH=main sudo -E ./scripts/deploy.sh  # override any setting via env vars
#
set -euo pipefail

# ---- configuration (override via environment variables) ----
REPO_URL="${REPO_URL:-https://github.com/aliniluca/fabclassifiedads.git}"
BRANCH="${BRANCH:-claude/keen-lovelace-pexrbj}"
SRC_DIR="${SRC_DIR:-/opt/aicigasesti-src}"
APP_DIR="${APP_DIR:-/var/www/aicigasesti}"
SERVICE="${SERVICE:-aicigasesti}"
SERVICE_USER="${SERVICE_USER:-aicigasesti}"
PROJECT="${PROJECT:-src/FabClassifiedAds.Web/FabClassifiedAds.Web.csproj}"
DLL="${DLL:-FabClassifiedAds.Web.dll}"
ASPNETCORE_URLS="${ASPNETCORE_URLS:-http://127.0.0.1:5000}"

log()  { printf '\033[1;35m▶ %s\033[0m\n' "$*"; }
die()  { printf '\033[1;31m✗ %s\033[0m\n' "$*" >&2; exit 1; }

[ "$(id -u)" -eq 0 ] || die "Run with sudo (needs to write /var/www, manage systemd)."

# ---- locate dotnet ----
DOTNET="$(command -v dotnet || true)"
[ -z "$DOTNET" ] && [ -x /usr/local/bin/dotnet ] && DOTNET=/usr/local/bin/dotnet
[ -z "$DOTNET" ] && [ -x /usr/share/dotnet/dotnet ] && DOTNET=/usr/share/dotnet/dotnet
[ -n "$DOTNET" ] || die "dotnet not found. Install the .NET 11 SDK first (see DEPLOY.md step 2)."
log "Using dotnet: $DOTNET ($("$DOTNET" --version))"

# ---- ensure the service user exists ----
if ! id "$SERVICE_USER" >/dev/null 2>&1; then
    log "Creating service user '$SERVICE_USER'"
    useradd -r -s /usr/sbin/nologin "$SERVICE_USER"
fi

# ---- clone or update the source ----
if [ -d "$SRC_DIR/.git" ]; then
    log "Updating source in $SRC_DIR ($BRANCH)"
    git -C "$SRC_DIR" fetch --depth 1 origin "$BRANCH"
    git -C "$SRC_DIR" checkout -B "$BRANCH" "origin/$BRANCH"
else
    log "Cloning $REPO_URL ($BRANCH) into $SRC_DIR"
    git clone --depth 1 -b "$BRANCH" "$REPO_URL" "$SRC_DIR"
fi

# ---- publish ----
log "Publishing to $APP_DIR"
mkdir -p "$APP_DIR"
"$DOTNET" publish "$SRC_DIR/$PROJECT" -c Release -o "$APP_DIR" --nologo

# ---- runtime dirs + permissions (db and uploads survive redeploys) ----
mkdir -p "$APP_DIR/wwwroot/uploads" "$APP_DIR/App_Data"
chown -R "$SERVICE_USER:$SERVICE_USER" "$APP_DIR"

# ---- install / refresh the systemd unit ----
UNIT="/etc/systemd/system/${SERVICE}.service"
NEW_UNIT="$(mktemp)"
cat > "$NEW_UNIT" <<EOF
[Unit]
Description=AiciGasesti (.NET 11) classifieds platform
After=network.target

[Service]
WorkingDirectory=$APP_DIR
ExecStart=$DOTNET $APP_DIR/$DLL
Restart=always
RestartSec=5
KillSignal=SIGINT
SyslogIdentifier=$SERVICE
User=$SERVICE_USER
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=$ASPNETCORE_URLS
Environment=DOTNET_CLI_TELEMETRY_OPTOUT=1
EOF
# Pass through the import API key if provided at deploy time (locks /api/import otherwise)
if [ -n "${IMPORT_API_KEY:-}" ]; then
    echo "Environment=IMPORT_API_KEY=$IMPORT_API_KEY" >> "$NEW_UNIT"
fi
cat >> "$NEW_UNIT" <<EOF

[Install]
WantedBy=multi-user.target
EOF

if ! cmp -s "$NEW_UNIT" "$UNIT" 2>/dev/null; then
    log "Installing systemd unit $UNIT"
    mv "$NEW_UNIT" "$UNIT"
    systemctl daemon-reload
    systemctl enable "$SERVICE"
else
    rm -f "$NEW_UNIT"
fi

# ---- restart ----
log "Restarting $SERVICE"
systemctl restart "$SERVICE"
sleep 2
systemctl --no-pager --lines=0 status "$SERVICE" || true

log "Done. Live logs: journalctl -u $SERVICE -f"
