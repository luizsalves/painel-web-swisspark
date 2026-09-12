# Docker

Dois containers:

| Serviço | Imagem | O que faz |
|---|---|---|
| `api` | .NET 10 (`aspnet:10.0`) | A API. Não publica porta — só o nginx alcança |
| `web` | nginx sem privilégios | Serve o SPA e repassa `/api` para a `api` |

O Postgres **não** está no compose de propósito: o banco `contabil` já existe no seu
servidor, com os dados do ETL. Subir um Postgres vazio ao lado só criaria confusão.

## Subir

```bash
cp .env.example .env      # preencha a connection string
docker compose up -d --build
```

Abra `http://localhost:8080`.

```bash
docker compose logs -f api     # acompanhar
docker compose ps              # estado e healthcheck
docker compose down            # derrubar
```

## Por que o nginx repassa a API

Front e API dividem o mesmo domínio. Para o navegador é tudo mesma origem, então **a
questão de CORS não existe** — nada de `Cors:AllowedOrigins` para manter em dia a cada
ambiente novo. A API também fica inalcançável de fora: só o nginx fala com ela.

Se precisar bater direto na API para depurar, publique a porta temporariamente:

```bash
docker compose run --rm --publish 5017:8080 api
```

## Configuração

Nenhum segredo entra na imagem. Tudo vem de variável de ambiente, lida do `.env` da raiz
(que está no `.gitignore`).

O ASP.NET Core traduz dois sublinhados em `:`, então `PainelDb__ConnectionString` é o mesmo
que `PainelDb:ConnectionString` do `appsettings.json`. O `keys.json` continua valendo para
quem roda fora de container.

### O problema do front, e como está resolvido

O Vite embute as variáveis `VITE_*` no bundle **durante o build**. Se dependêssemos disso, a
imagem ficaria amarrada a um tenant e a uma URL — e não daria para construir nada antes de a
TI devolver os dados do Entra ID.

Em vez disso, o container gera um `/config.js` no start, a partir das variáveis
`PAINEL_ENTRA_*` (veja `frontend/docker-entrypoint.sh`). A mesma imagem serve para homologação
e produção; trocar de ambiente é reiniciar o container com outro `.env`, não rebuildar.

Fora do container nada muda: `npm run dev` continua lendo o `.env.local`.

## Banco na própria máquina hospedeira

Se o Postgres roda no mesmo servidor que o Docker, o container não enxerga `localhost` — ele
tem o próprio. Use `host.docker.internal`, que o compose já mapeia:

```
PAINEL_DB_CONNECTION_STRING=Host=host.docker.internal;Port=5432;Database=contabil;...
```

Para um Postgres em outro servidor, use o host normalmente.

## Produção

Três pontos além do `.env`:

1. **TLS.** Os containers falam só HTTP. Coloque um proxy na frente (nginx, Traefik,
   Caddy, o balanceador da empresa) terminando HTTPS na porta publicada. O Entra ID só
   aceita redirect URI `http://` para `localhost` — em produção, sem certificado não entra.
   A API já está preparada para proxy: `Hosting__UseForwardedHeaders` vem `true` do compose.

2. **`PAINEL_ENTRA_REDIRECT_URI` idêntica à cadastrada no Entra ID.** A Microsoft compara
   caractere por caractere: `https://painel.swisspark.com.br` e
   `https://painel.swisspark.com.br/` são URIs diferentes para ela.

3. **Saída HTTPS para `login.microsoftonline.com`.** O container `api` baixa as chaves
   públicas da Microsoft para validar a assinatura dos tokens. Em rede fechada isso falha,
   e o sintoma (401 em tudo) não aponta para a causa. Vale confirmar com a TI.

## Imagens

- `api`: build multi-estágio, `dotnet publish` no SDK e só o resultado na imagem de runtime.
  Roda como usuário sem privilégios (`$APP_UID`).
- `web`: build com `node:22-alpine`, resultado servido pelo `nginx-unprivileged` (uid 101,
  escutando na 8080). O `npm ci` fica em camada própria — mudança de componente não refaz a
  instalação de dependências.

Nenhum dos dois roda como root.

## Antes do primeiro `up`

O container não cria tabela. Rode o script uma vez no Postgres:

```bash
psql -h SERVIDOR -U USUARIO -d contabil -f docs/sql/001_bi_users.sql
```

Sem o administrador de bootstrap (o `INSERT` no fim do script, **com o e-mail trocado**)
ninguém consegue abrir a tela de usuários.
