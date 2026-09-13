#!/bin/bash
set -euo pipefail

PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin

PROJECT_DIR="/opt/projetos/painel-web-swisspark"
LOG="$PROJECT_DIR/.deploy/deploy.log"

cd "$PROJECT_DIR"

echo "[$(date '+%F %T')] Verificando GitHub..." >> "$LOG"

git fetch origin main >> "$LOG" 2>&1

LOCAL=$(git rev-parse HEAD)
REMOTE=$(git rev-parse origin/main)

if [ "$LOCAL" = "$REMOTE" ]; then
    echo "[$(date '+%F %T')] Nenhuma atualização." >> "$LOG"
    exit 0
fi

echo "[$(date '+%F %T')] Nova versão encontrada." >> "$LOG"

git pull --ff-only origin main >> "$LOG" 2>&1

echo "[$(date '+%F %T')] Reconstruindo Docker..." >> "$LOG"

docker compose up -d --build >> "$LOG" 2>&1

echo "[$(date '+%F %T')] Deploy concluído." >> "$LOG"
