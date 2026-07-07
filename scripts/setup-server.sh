#!/usr/bin/env bash
#
# setup-server.sh — one-time provisioning for a fresh Oracle Cloud
# Ubuntu 20.04 instance. Installs .NET 11, Nginx, opens the OS firewall,
# writes the reverse-proxy config and (optionally) obtains a TLS cert.
#
# After this finishes, run scripts/deploy.sh to build & start the app.
#
# Usage:
#   sudo DOMAIN=aicigasesti.ro EMAIL=adresa@ta.ro ./scripts/setup-server.sh
#   sudo ./scripts/setup-server.sh          # skips TLS if DOMAIN/EMAIL unset
#
# NOTE: You must ALSO open ports 80 and 443 in the OCI web console
#       (VCN → Subnet → Security List → Add Ingress Rules). This script
#       only handles the OS-level iptables firewall.
#
set -euo pipefail

DOMAIN="${DOMAIN:-}"
EMAIL="${EMAIL:-}"
APP_PORT="${APP_PORT:-5000}"
SERVICE="${SERVICE:-aicigasesti}"

log() { printf '\033[1;35m▶ %s\033[0m\n' "$*"; }
die() { printf '\033[1;31m✗ %s\033[0m\n' "$*" >&2; exit 1; }

[ "$(id -u)" -eq 0 ] || die "Run with sudo."

# ---- 1. base packages ----
log "Installing base packages"
apt-get update -y
apt-get install -y git nginx ffmpeg curl ca-certificates sqlite3

# ---- 2. .NET 11 SDK ----
if ! command -v dotnet >/dev/null 2>&1 && [ ! -x /usr/local/bin/dotnet ]; then
    log "Installing .NET 11 SDK"
    apt-get install -y libicu66 libssl1.1 zlib1g libgcc-s1 || true
    curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
    chmod +x /tmp/dotnet-install.sh
    /tmp/dotnet-install.sh --channel 11.0 --install-dir /usr/share/dotnet
    ln -sf /usr/share/dotnet/dotnet /usr/local/bin/dotnet
fi
log ".NET version: $(/usr/local/bin/dotnet --version)"

# ---- 3. OS firewall: open 80 and 443 before the OCI REJECT rule ----
open_port() {
    local port="$1"
    if ! iptables -C INPUT -m state --state NEW -p tcp --dport "$port" -j ACCEPT 2>/dev/null; then
        log "Opening TCP port $port in iptables"
        # insert just above the default REJECT rule if present, else append
        local reject_line
        reject_line="$(iptables -L INPUT --line-numbers -n | awk '/REJECT/ {print $1; exit}')"
        if [ -n "${reject_line:-}" ]; then
            iptables -I INPUT "$reject_line" -m state --state NEW -p tcp --dport "$port" -j ACCEPT
        else
            iptables -A INPUT -m state --state NEW -p tcp --dport "$port" -j ACCEPT
        fi
    fi
}
open_port 80
open_port 443
if command -v netfilter-persistent >/dev/null 2>&1; then
    netfilter-persistent save
else
    apt-get install -y iptables-persistent && netfilter-persistent save
fi

# ---- 4. Nginx reverse proxy ----
SERVER_NAME="${DOMAIN:-_}"
[ -n "$DOMAIN" ] && SERVER_NAME="$DOMAIN www.$DOMAIN"
log "Writing Nginx site (server_name: $SERVER_NAME)"
cat > /etc/nginx/sites-available/"$SERVICE" <<EOF
server {
    listen 80;
    server_name $SERVER_NAME;

    # allow user video uploads (app caps at 60 MB)
    client_max_body_size 120M;

    location / {
        proxy_pass         http://127.0.0.1:$APP_PORT;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade \$http_upgrade;
        proxy_set_header   Connection keep-alive;
        proxy_set_header   Host \$host;
        proxy_cache_bypass \$http_upgrade;
        proxy_set_header   X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto \$scheme;
    }
}
EOF
ln -sf /etc/nginx/sites-available/"$SERVICE" /etc/nginx/sites-enabled/"$SERVICE"
rm -f /etc/nginx/sites-enabled/default
nginx -t && systemctl reload nginx

# ---- 5. TLS via Let's Encrypt (optional) ----
if [ -n "$DOMAIN" ] && [ -n "$EMAIL" ]; then
    log "Obtaining Let's Encrypt certificate for $DOMAIN"
    if ! command -v certbot >/dev/null 2>&1; then
        snap install core && snap refresh core
        snap install --classic certbot
        ln -sf /snap/bin/certbot /usr/bin/certbot
    fi
    certbot --nginx -d "$DOMAIN" -d "www.$DOMAIN" --agree-tos -m "$EMAIL" --redirect -n
else
    log "Skipping TLS (set DOMAIN and EMAIL to enable). Site is on http:// for now."
fi

log "Server ready. Next: sudo ./scripts/deploy.sh"
log "Reminder: also open ports 80/443 in the OCI Security List (web console)."
