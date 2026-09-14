#!/bin/bash
set -Eeuo pipefail
PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin
PROJECT_DIR="${SWISSPARK_PROJECT_DIR:-/opt/projetos/painel-web-swisspark}"
cd "$PROJECT_DIR"
LOG="$PROJECT_DIR/.deploy/deploy.log"
exec >> "$LOG" 2>&1
trap 'rc=$?; echo "[$(date -Is)] ERRO: deploy falhou (linha $LINENO, cÃ³digo $rc); nova tentativa no prÃ³ximo ciclo."; exit "$rc"' ERR
STATE=$(git rev-parse --git-path swisspark-deployed-commit)
echo "[$(date '+%F %T')] Verificando GitHub..."
git fetch origin main
LOCAL=$(git rev-parse HEAD)
REMOTE=$(git rev-parse origin/main)
if [ "$LOCAL" != "$REMOTE" ]; then
    echo "[$(date '+%F %T')] Atualizando cÃ³digo..."
    git merge --ff-only origin/main
fi
TARGET=$(git rev-parse HEAD)
DEPLOYED=""
if [ -f "$STATE" ]; then DEPLOYED=$(cat "$STATE"); fi
if [ "$TARGET" = "$DEPLOYED" ]; then
    echo "[$(date '+%F %T')] Nenhum deploy pendente."
    exit 0
fi
echo "[$(date '+%F %T')] Reconstruindo Docker para $TARGET..."
docker compose up -d --build --wait --wait-timeout 180
printf '%s\n' "$TARGET" > "$STATE.tmp"
mv "$STATE.tmp" "$STATE"
echo "[$(date '+%F %T')] Deploy concluÃ­do: $TARGET."
