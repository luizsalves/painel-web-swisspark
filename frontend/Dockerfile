# syntax=docker/dockerfile:1

# ==========================================================================
# Build
# ==========================================================================
FROM node:22-alpine AS build

WORKDIR /app

# Instala as dependências antes de copiar o código: o npm ci só refaz quando o
# package-lock muda, não a cada alteração de componente.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

# ==========================================================================
# Runtime — nginx sem privilégios (roda como uid 101, escuta na 8080)
# ==========================================================================
FROM nginxinc/nginx-unprivileged:1.30-alpine AS runtime

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

# O entrypoint reescreve o config.js no start; o dono dos arquivos precisa ser o
# usuário que roda o nginx.
USER root
RUN chown -R 101:101 /usr/share/nginx/html
USER 101

COPY --chmod=0755 docker-entrypoint.sh /painel-entrypoint.sh

EXPOSE 8080

ENTRYPOINT ["/painel-entrypoint.sh"]
CMD ["nginx", "-g", "daemon off;"]
