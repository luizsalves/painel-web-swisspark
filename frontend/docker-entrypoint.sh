#!/bin/sh
# Gera a configuração de execução do front a partir das variáveis de ambiente.
#
# O Vite embute as VITE_* no bundle durante o build. Sem este arquivo, a imagem
# ficaria amarrada a um tenant e a uma URL — e não daria para construir nada
# antes de a TI devolver os dados do Entra ID.
set -eu

escapar() {
  printf '%s' "${1:-}" | sed 's/\\/\\\\/g; s/"/\\"/g'
}

cat > /usr/share/nginx/html/config.js <<ARQUIVO
window.__PAINEL_CONFIG__ = {
  apiUrl: "$(escapar "${PAINEL_API_URL:-/api}")",
  entraTenantId: "$(escapar "${PAINEL_ENTRA_TENANT_ID:-}")",
  entraClientId: "$(escapar "${PAINEL_ENTRA_CLIENT_ID:-}")",
  entraApiScope: "$(escapar "${PAINEL_ENTRA_API_SCOPE:-}")",
  entraRedirectUri: "$(escapar "${PAINEL_ENTRA_REDIRECT_URI:-}")"
};
ARQUIVO

if [ -z "${PAINEL_ENTRA_CLIENT_ID:-}" ]; then
  echo "[painel] Aviso: PAINEL_ENTRA_CLIENT_ID vazio — a tela de login vai avisar que falta configurar o Entra ID." >&2
fi

# Entrega para o entrypoint original da imagem do nginx.
exec /docker-entrypoint.sh "$@"
