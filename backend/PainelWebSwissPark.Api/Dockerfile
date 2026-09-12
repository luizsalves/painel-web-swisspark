# syntax=docker/dockerfile:1
#
# Contexto de build: a pasta backend/
#   docker build -f PainelWebSwissPark.Api/Dockerfile .

# ==========================================================================
# Build
# ==========================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Restaura antes de copiar o código: o restore só refaz quando o csproj muda.
COPY PainelWebSwissPark.Api/PainelWebSwissPark.Api.csproj PainelWebSwissPark.Api/
RUN dotnet restore PainelWebSwissPark.Api/PainelWebSwissPark.Api.csproj

COPY . .
RUN dotnet publish PainelWebSwissPark.Api/PainelWebSwissPark.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ==========================================================================
# Runtime
# ==========================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# curl serve ao healthcheck do compose; a imagem base não traz cliente HTTP.
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

# APP_UID vem da imagem base: usuário sem privilégios.
USER $APP_UID

# Só HTTP dentro do container. Quem termina o TLS é o proxy na frente.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "PainelWebSwissPark.Api.dll"]
