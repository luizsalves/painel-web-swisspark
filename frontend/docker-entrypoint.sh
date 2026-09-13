#!/bin/sh
# Gera a configuração de execução do front a partir das variáveis de ambiente.
#
# O Vite embute as VITE_* no bundle durante o build. Sem este arquivo, a imagem
# ficaria amarrada a uma URL de API e cada ambiente exigiria uma imagem própria.
set -eu

escapar() {
  printf '%s' "${1:-}" | sed 's/\\/\\\\/g; s/"/\\"/g'
}

cat > /usr/share/nginx/html/config.js <<ARQUIVO
window.__PAINEL_CONFIG__ = {
  apiUrl: "$(escapar "${PAINEL_API_URL:-/api}")"
};
ARQUIVO

exec /docker-entrypoint.sh "$@"
