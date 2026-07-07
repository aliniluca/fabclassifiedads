#!/usr/bin/env bash
#
# backup.sh — back up the AiciGăsești SQLite database and user uploads.
#
# Safe to run while the app is live: uses SQLite's online backup API (via
# `.backup`) so it captures a consistent snapshot without stopping the service.
# Keeps the last N days and prunes older archives.
#
# Usage:
#   sudo ./scripts/backup.sh
#   APP_DIR=/var/www/aicigasesti BACKUP_DIR=/var/backups/aicigasesti KEEP_DAYS=14 sudo -E ./scripts/backup.sh
#
# Cron (daily at 03:30) — run `sudo crontab -e` and add:
#   30 3 * * * /var/www/aicigasesti-src/scripts/backup.sh >> /var/log/aicigasesti-backup.log 2>&1
#
set -euo pipefail

APP_DIR="${APP_DIR:-/var/www/aicigasesti}"
BACKUP_DIR="${BACKUP_DIR:-/var/backups/aicigasesti}"
DB="${DB:-$APP_DIR/aicigasesti.db}"
UPLOADS="${UPLOADS:-$APP_DIR/wwwroot/uploads}"
KEEP_DAYS="${KEEP_DAYS:-14}"

log() { printf '\033[1;35m▶ %s\033[0m\n' "$*"; }
die() { printf '\033[1;31m✗ %s\033[0m\n' "$*" >&2; exit 1; }

stamp="$(date +%Y%m%d-%H%M%S)"
mkdir -p "$BACKUP_DIR"

# ---- database (consistent online snapshot) ----
if [ -f "$DB" ]; then
    db_out="$BACKUP_DIR/db-$stamp.sqlite"
    if command -v sqlite3 >/dev/null 2>&1; then
        log "Backing up database (online) -> $db_out"
        sqlite3 "$DB" ".backup '$db_out'"
    else
        log "sqlite3 not found; copying DB file directly -> $db_out"
        cp "$DB" "$db_out"
    fi
    gzip -f "$db_out"
else
    log "No database at $DB (skipping)"
fi

# ---- uploads (photos + videos) ----
if [ -d "$UPLOADS" ]; then
    up_out="$BACKUP_DIR/uploads-$stamp.tgz"
    log "Backing up uploads -> $up_out"
    tar czf "$up_out" -C "$(dirname "$UPLOADS")" "$(basename "$UPLOADS")"
else
    log "No uploads dir at $UPLOADS (skipping)"
fi

# ---- prune old backups ----
log "Pruning backups older than $KEEP_DAYS days"
find "$BACKUP_DIR" -type f \( -name 'db-*.sqlite.gz' -o -name 'uploads-*.tgz' \) -mtime +"$KEEP_DAYS" -delete

log "Done. Current backups:"
ls -lh "$BACKUP_DIR" | tail -n +2 | awk '{print "  " $9 "  " $5}'
