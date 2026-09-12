# Autenticação e autorização

## O desenho em uma frase

O **Entra ID autentica** (prova que a pessoa é quem diz ser) e a **tabela `bi_users`
autoriza** (diz se ela pode entrar e com qual perfil).

O painel não guarda senha, não faz hash de senha e não emite token próprio.

## Por que não emitir um token próprio

Seria natural validar o token da Microsoft uma vez e emitir um JWT da aplicação. Mas aí,
quando um administrador desativasse alguém, o acesso continuaria valendo até aquele token
expirar. Como o objetivo da área administrativa é justamente controlar acesso, isso
esvaziaria a funcionalidade.

Em vez disso: o front manda o access token do Entra a cada requisição, e o backend valida
a assinatura **e** consulta a `bi_users`. Desativou na tela, o próximo request toma 403.

O custo é uma consulta por requisição — barata e indexada. Se um dia incomodar, existe
`PainelDb:CacheSeconds` no `appsettings.json`: qualquer valor acima de 0 liga um cache de
autorização, e o atraso na revogação passa a ser esse número de segundos. O padrão é 0.

## O fluxo, passo a passo

```
1. usuário clica "Entrar com Microsoft"
2. MSAL abre login.microsoftonline.com        ← senha e MFA são digitados AQUI
3. Microsoft autentica e devolve um authorization code
4. MSAL troca o code por tokens (PKCE, sem client secret no navegador)
5. front recebe o access token da NOSSA api
6. front chama a API: Authorization: Bearer <access token>
7. backend valida a assinatura contra as chaves públicas da Microsoft,
   lê o claim "oid" e consulta a bi_users
8. não cadastrado ou inativo -> 403
```

A senha nunca passa pela aplicação. O que chega é a identidade já verificada.

### A pegadinha que derruba a primeira tentativa

O front precisa pedir um token **para a nossa API** — escopo
`api://<client-id>/access_as_user`. Se mandar o ID token ou um token do Microsoft Graph,
a validação de audience falha e a resposta é 401 sem explicação aparente. É o erro
clássico. Está resolvido em `frontend/src/services/entraId.ts`, mas vale saber ao
depurar.

## Casamento por `oid`, não por e-mail

E-mail corporativo muda (casamento, troca de domínio). O `oid` do Entra é imutável dentro
do tenant. Mas o administrador cadastra pessoas por e-mail, porque ele não conhece o
`oid` de ninguém.

A solução tem três tempos:

1. o administrador cadastra com `entra_object_id` nulo;
2. no primeiro login o backend casa por e-mail e grava o `oid`;
3. dali em diante casa sempre por `oid`.

Enquanto o `oid` estiver nulo, a tela de usuários mostra "aguardando 1º acesso".

Se o e-mail bater mas a linha já tiver outro `oid` gravado, o acesso é negado — reaproveitar
seria entregar o acesso de uma pessoa a outra que herdou o endereço.

## O primeiro administrador

A tela de administração exige perfil `Administrador`, e a tabela nasce vazia: ninguém
consegue entrar. O `INSERT` de bootstrap está em `docs/sql/001_bi_users.sql` — **troque o
e-mail antes de rodar**.

Nunca resolva isso com "o primeiro que logar vira admin": qualquer pessoa do tenant
viraria dona do sistema.

## Travas da área administrativa

Existem para impedir que um administrador se tranque para fora — situação que só se
resolve com acesso direto ao banco:

- administrador não pode desativar o próprio acesso;
- administrador não pode rebaixar o próprio perfil;
- não é possível desativar nem rebaixar o último `Administrador` ativo;
- usuário não é excluído, é desativado — a auditoria depende do registro continuar existindo;
- toda alteração grava uma linha em `bi_users_audit`.

As travas estão no backend (`Services/BusinessLogic/UsuariosService.cs`). O front também
desabilita o botão de auto-desativação, mas isso é conveniência: a regra que vale é a do
servidor.

## Onde está cada coisa

### Backend

| Arquivo | Papel |
|---|---|
| `Program.cs` | Liga o Entra ID, define a política padrão e a de administrador |
| `Entities/BiUser.cs` | A tabela de autorização |
| `Data/PainelDbContext.cs` | Mapeamento; o schema vem de `PainelDb:Schema` |
| `Services/Auth/UsuarioAtualService.cs` | Traduz token → registro na `bi_users`, vincula o `oid` no 1º login |
| `Authorization/UsuarioAtivoHandler.cs` | A checagem por requisição que torna a revogação imediata |
| `Authorization/BiUsersClaimsTransformation.cs` | Traz o perfil da `bi_users` para dentro do principal |
| `Controllers/AuthController.cs` | `GET /api/auth/me` |
| `Controllers/UsuariosController.cs` | CRUD da área administrativa |
| `Services/BusinessLogic/UsuariosService.cs` | As travas |

Todo endpoint já nasce protegido: a política padrão (`FallbackPolicy`) exige autenticação
**e** usuário ativo. Para abrir um endpoint é preciso dizer `[AllowAnonymous]`
explicitamente — só o `/api/health` faz isso.

### Frontend

| Arquivo | Papel |
|---|---|
| `src/services/entraId.ts` | Configuração do MSAL, login, obtenção e renovação do token |
| `src/services/api.ts` | Injeta o Bearer; traduz 401/403 em eventos |
| `src/contexts/AuthContext.tsx` | Estado da sessão; reage à revogação |
| `src/rotas/ProtectedRoute.tsx` | Rota protegida, com variante por perfil |
| `src/paginas/Usuarios/` | A área administrativa |

## Perfis

`Administrador`, `Gerente`, `Analista` — o `CHECK` da coluna `role` no banco e a constante
`PerfisUsuario` no backend precisam continuar iguais. Hoje só `Administrador` tem efeito
(libera a área de usuários); `Gerente` e `Analista` existem para as próximas etapas.

## Como rodar

### 1. Banco

```bash
psql -h SERVIDOR -U USUARIO -d contabil -f docs/sql/001_bi_users.sql
```

O script é idempotente e reconcilia uma `bi_users` que já exista. Troque o e-mail do
administrador de bootstrap antes de rodar.

Para rodar tudo em container, veja [`DOCKER.md`](DOCKER.md) — lá a configuração vem por
variável de ambiente e o nginx resolve o CORS servindo front e API na mesma origem.

### 2. Backend

```bash
cp keys.example.json keys.json      # na RAIZ do projeto; preencha ConnectionStrings:Contabil
cd backend/PainelWebSwissPark.Api
dotnet restore
dotnet run
```

O `keys.json` fica na raiz, ao lado de `backend/` e `frontend/`, e está no `.gitignore`. A
aplicação sobe de diretório a partir da pasta do projeto até encontrá-lo. Para guardá-lo em
outro lugar, aponte `PAINEL_KEYS_FILE` para o caminho.

A chave que a autenticação lê é `ConnectionStrings:Contabil` — o Postgres onde vive a
`bi_users`. A grafia antiga `PainelDb:ConnectionString` continua aceita.

Preencha `AzureAd:TenantId` e `AzureAd:ClientId` no `appsettings.json` com o que a TI
devolver. A aplicação recusa subir sem a connection string do Postgres — de propósito, e a mensagem
diz qual `keys.json` ela leu, para você não ficar procurando por que o valor "está lá" mas
não é enxergado.

### 3. Frontend

```bash
cd frontend
cp .env.example .env.local          # preencha tenant, client id e escopo
npm install
npm run dev
```

Sem as variáveis do Entra a aplicação sobe do mesmo jeito e a tela de login explica o que
está faltando, em vez de quebrar.

### 4. Conferência

```
GET  /api/health        -> 200 sem token
GET  /api/auth/me       -> 401 sem token
                        -> 403 com token de quem não está na bi_users
                        -> 200 com o perfil de quem está
GET  /api/usuarios      -> 403 para Gerente/Analista, 200 para Administrador
```

Teste a revogação: entre com uma conta, desative-a pela tela de outro navegador e recarregue
a primeira. O acesso cai na primeira requisição — não no próximo login.

## O que ainda depende da TI

O registro do aplicativo no Entra ID. Sem ele não dá nem para testar local.
O pedido pronto para enviar está em [`PEDIDO-TI-ENTRA-ID.md`](PEDIDO-TI-ENTRA-ID.md).
