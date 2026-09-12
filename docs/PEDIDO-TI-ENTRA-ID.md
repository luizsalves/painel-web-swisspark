# Pedido para a TI — registro de aplicativo no Entra ID

Texto pronto para encaminhar. É a única dependência externa da autenticação do painel:
sem esses dados não é possível nem testar em ambiente local.

---

**Assunto:** Registro de aplicativo no Entra ID — Painel de Conciliação Contábil

Olá,

Estamos implementando o login do Painel de Conciliação Contábil usando as contas
corporativas Microsoft (Entra ID), para que ninguém precise de senha própria no sistema.
Para isso, preciso que seja registrado um aplicativo no nosso tenant.

**O que precisa ser criado**

Um registro de aplicativo com estas características:

1. **Plataforma: SPA (Single-page application)** — não "Web". A diferença importa: SPA
   habilita o fluxo com PKCE, que dispensa client secret no navegador. Um registro do tipo
   "Web" não funciona para o nosso caso.

2. **Redirect URIs cadastradas:**
   - `http://localhost:5173` (desenvolvimento)
   - `<URL de produção do painel>` (quando estiver definida)

3. **Uma scope exposta na API**, chamada `access_as_user`, com consentimento de
   administrador ou de usuário. Ela aparece como `api://<client-id>/access_as_user`.

4. **Permissão para que o próprio aplicativo consuma essa scope** (adicionar
   `api://<client-id>/access_as_user` às permissões de API do mesmo registro).

**O que preciso receber de volta**

- `TenantId` (ID do diretório)
- `ClientId` (ID do aplicativo)
- a URI completa da scope exposta (`api://<client-id>/access_as_user`)
- confirmação das redirect URIs cadastradas

**O que NÃO é necessário**

- client secret — o fluxo é PKCE, sem segredo no navegador
- permissões do Microsoft Graph — o painel não lê dados do Graph, só precisa saber quem é a
  pessoa autenticada

**Sobre acesso**

Estar no tenant não dá acesso ao painel. Quem pode entrar é controlado dentro da própria
aplicação, por uma lista mantida por administradores do painel. O Entra ID só confirma a
identidade.

Obrigado!

---

## Depois que a TI responder

| Onde | Chave | Valor |
|---|---|---|
| `backend/PainelWebSwissPark.Api/appsettings.json` | `AzureAd:TenantId` | TenantId |
| `backend/PainelWebSwissPark.Api/appsettings.json` | `AzureAd:ClientId` | ClientId |
| `frontend/.env.local` | `VITE_ENTRA_TENANT_ID` | TenantId |
| `frontend/.env.local` | `VITE_ENTRA_CLIENT_ID` | ClientId |
| `frontend/.env.local` | `VITE_ENTRA_API_SCOPE` | `api://<client-id>/access_as_user` |
| `frontend/.env.local` | `VITE_ENTRA_REDIRECT_URI` | `http://localhost:5173` |

Nenhum desses valores é segredo — `TenantId` e `ClientId` aparecem no tráfego do navegador
por natureza do protocolo. O que não pode vazar é a connection string do banco, que fica no
`keys.json`, fora do repositório.
