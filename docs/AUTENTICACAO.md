# Autenticação e autorização

## O desenho em uma frase

A **`bi_users`** guarda o hash da senha; a **`bi_user_modules`** guarda em quais módulos a
pessoa entra e com qual perfil em cada um. O login confere a senha e devolve um token; o
token diz apenas quem a pessoa é, e **módulos, perfis e situação são lidos do banco a cada
requisição**.

## Dois sistemas em um painel

O painel hospeda módulos independentes — hoje `conciliacao` e `mkt`. Quem pertence a um não
enxerga o outro: nem as telas, nem os usuários.

```
bi_users           quem é a pessoa, senha, situação
bi_user_modules    pessoa + módulo + perfil NAQUELE módulo
bi_modules         os módulos existentes
```

O perfil não fica na `bi_users` porque não é uma propriedade da pessoa: é da relação entre
ela e o módulo. A mesma pessoa pode ser Administrador na conciliação e Analista no mkt — com
**uma conta só**, uma senha só.

### Quem administra o quê

| | |
|---|---|
| **Administrador de módulo** | Lista, cria, edita e desativa só quem tem vínculo com os módulos que ele administra |
| **Superadministrador** | Enxerga e administra todos os módulos |

O superadministrador existe por um motivo estrutural, não por conveniência: o administrador
da conciliação não enxerga o mkt e vice-versa, então nenhum dos dois conseguiria criar o
outro. Alguém precisa estar acima dos módulos.

Ele é criado uma única vez, pelas variáveis `PAINEL_ADMIN_*`. **Não há tela para promover
ninguém a superadministrador**, de propósito.

### Duas travas que não são óbvias

Um administrador de módulo **não enxerga o superadministrador** na lista — senão poderia
desativá-lo e deixar o painel sem ninguém acima dos módulos.

E ele **não vê os vínculos da pessoa em módulos alheios**: saber que o fulano da conciliação
também é do mkt não é da conta dele. Ao editar, os vínculos dos outros módulos ficam
intocados.

### Onde a separação realmente acontece

No `[ExigeModulo]` de cada controller. Esconder o item do menu é cosmético — sem a checagem
no backend, bastaria digitar a URL.

```csharp
[ExigeModulo(Modulos.Conciliacao)]
public class FechamentosController : ControllerBase
```

## Por que o token carrega tão pouco

Seria natural colocar o perfil dentro do token — economiza uma consulta. Mas aí, quando um
administrador desativasse alguém ou rebaixasse um perfil, o acesso antigo continuaria
valendo até aquele token expirar. Como a área administrativa existe justamente para
controlar acesso, isso a esvaziaria.

Então o token carrega só `sub` (o id na `bi_users`), `email` e `name`. Perfil e situação
vêm do banco em toda requisição. Desativou na tela, o próximo request toma 403.

O custo é uma consulta por requisição — barata e indexada. Se um dia incomodar, existe
`PainelDb:CacheSeconds`: qualquer valor acima de 0 liga um cache, e o atraso na revogação
passa a ser esse número de segundos. O padrão é 0.

## O fluxo

```
1. pessoa digita e-mail e senha
2. POST /api/auth/login
3. backend busca na bi_users por lower(email)
4. confere a senha contra o hash PBKDF2
5. devolve { accessToken, expiraEm, usuario }
6. front guarda o token e o manda em todo request:
      Authorization: Bearer <token>
7. backend valida a assinatura, lê o "sub" e consulta a bi_users
8. inativo, inexistente ou com senha provisória -> 403
```

A senha nunca é guardada em texto. A coluna `password_hash` recebe o hash produzido pelo
`PasswordHasher` do ASP.NET Core (PBKDF2 com HMAC-SHA512), o mesmo componente usado pelo
ASP.NET Identity — não é implementação caseira.

## Senha provisória

Quem define senha para outra pessoa é sempre o administrador, e essa senha nasce
provisória: `must_change_password = true`. Até trocar, a pessoa entra mas o painel fica
fechado — só `/api/auth/me` e `/api/auth/trocar-senha` respondem.

Isso vale para o cadastro de um usuário novo e para a redefinição de senha de alguém que
esqueceu. Sem essa trava, o administrador continuaria conhecendo a senha de outra pessoa
indefinidamente.

Não existe "esqueci minha senha" por e-mail: exigiria SMTP, que não está definido. O
caminho é pedir a um administrador, que redefine pela tela.

## O primeiro administrador

A tela de administração exige perfil `Administrador`, e a tabela nasce vazia: ninguém
entra. O primeiro é criado na primeira subida da aplicação, a partir de variáveis de
ambiente:

```bash
PAINEL_ADMIN_EMAIL=seu.email@swisspark.com.br
PAINEL_ADMIN_PASSWORD=uma-senha-provisoria
PAINEL_ADMIN_NAME=Seu Nome
```

Só roda se **não existir nenhum administrador ativo**. Se o e-mail já estiver cadastrado,
nada acontece e um aviso vai para o log — promover alguém por variável de ambiente seria
uma porta dos fundos.

Depois do primeiro acesso, apague as duas primeiras linhas do `.env`.

Nunca resolva o bootstrap com "o primeiro que logar vira admin": qualquer pessoa que
achasse a URL viraria dona do sistema.

## Travas da área administrativa

Existem para impedir que um administrador se tranque para fora — situação que só se
resolve com acesso direto ao banco:

- administrador não pode desativar o próprio acesso;
- administrador não pode rebaixar o próprio perfil;
- não é possível desativar nem rebaixar o último `Administrador` ativo;
- usuário não é excluído, é desativado — a auditoria depende do registro continuar existindo;
- toda alteração grava uma linha em `bi_users_audit`, incluindo troca e redefinição de senha.

As travas estão no backend (`Services/BusinessLogic/UsuariosService.cs`). O front também
desabilita o botão de auto-desativação, mas isso é conveniência: a regra que vale é a do
servidor.

## Detalhes que não são óbvios

**E-mail não diferencia maiúsculas.** O índice é `lower(email)`, e a escrita normaliza para
minúsculo. Sem isso, `Luiz@x.com` e `luiz@x.com` seriam dois usuários e o login casaria com
um ou outro conforme a digitação.

**A resposta do login não revela se o e-mail existe.** Errar o e-mail e errar a senha dão a
mesma mensagem. Quando o e-mail não existe, a verificação roda contra um hash descartável
assim mesmo — sem isso o tempo de resposta denunciaria quais e-mails estão cadastrados.

**"Acesso desativado" só aparece para quem acertou a senha.** Quem errou continua recebendo
a mensagem genérica.

**O hash se atualiza sozinho.** Quando o .NET muda os parâmetros do PBKDF2, o próximo login
bem-sucedido regrava a senha no formato novo.

## Onde está cada coisa

### Backend

| Arquivo | Papel |
|---|---|
| `Program.cs` | Valida a chave de assinatura, liga o JWT e define as políticas |
| `Entities/BiUser.cs` | A tabela: hash, perfil, situação |
| `Services/Auth/LoginService.cs` | Confere a senha, emite o token, troca de senha |
| `Services/Auth/TokenService.cs` | Monta e assina o token |
| `Services/Auth/UsuarioAtualService.cs` | Traduz o `sub` do token para o registro na `bi_users` |
| `Services/Auth/AdministradorInicial.cs` | Bootstrap do primeiro administrador |
| `Authorization/UsuarioAtivoHandler.cs` | A checagem por requisição que torna a revogação imediata |
| `Authorization/ExigeModuloAttribute.cs` | Amarra um endpoint a um módulo — é aqui que os sistemas se separam |
| `Authorization/AdministradorDeModuloHandler.cs` | Abre a área administrativa para quem administra algum módulo |
| `Controllers/AuthController.cs` | `login`, `me`, `trocar-senha` |
| `Controllers/UsuariosController.cs` | Área administrativa |
| `Services/BusinessLogic/UsuariosService.cs` | As travas |

Todo endpoint já nasce protegido: a política padrão (`FallbackPolicy`) exige autenticação
**e** usuário ativo sem senha provisória. Para abrir um endpoint é preciso dizer
`[AllowAnonymous]` explicitamente — só `/api/health` e `/api/auth/login` fazem isso.

### Frontend

| Arquivo | Papel |
|---|---|
| `src/services/authService.ts` | Login, `me`, troca de senha |
| `src/services/authStorage.ts` | Guarda o token em `sessionStorage` |
| `src/services/api.ts` | Injeta o Bearer; traduz 401/403 em eventos |
| `src/contexts/AuthContext.tsx` | Estado da sessão; reage à revogação |
| `src/rotas/ProtectedRoute.tsx` | Rota protegida, com variante por perfil |
| `src/paginas/TrocarSenha/` | Tela obrigatória de quem tem senha provisória |
| `src/paginas/Usuarios/` | A área administrativa |
| `src/paginas/Inicio/` | Decide a primeira tela conforme os módulos da pessoa |

O token fica em `sessionStorage`, não `localStorage`: a sessão morre ao fechar a aba. Em
máquina compartilhada isso vale mais que a conveniência de continuar logado.

## Perfis

`Administrador`, `Gerente`, `Analista` — o `CHECK` da coluna `role` em `bi_user_modules` e a
constante `PerfisUsuario` no backend precisam continuar iguais. Hoje só `Administrador` tem
efeito (libera a área de usuários daquele módulo); `Gerente` e `Analista` diferenciam apenas
o que aparece no menu do usuário.

## Acrescentar um módulo

É um `INSERT`, não um deploy:

```sql
insert into public.bi_modules (key, name, sort_order)
values ('novo_modulo', 'Nome do Módulo', 3);
```

As telas dele é que precisam ser escritas, declarando o módulo na rota do front
(`requireModulo`) e no controller do backend (`[ExigeModulo]`).

## Como rodar

### 1. Banco

```bash
psql -h SERVIDOR -U USUARIO -d contabil -f docs/sql/001_bi_users.sql
psql -h SERVIDOR -U USUARIO -d contabil -f docs/sql/002_modulos.sql
```

Os scripts são idempotentes e reconciliam uma `bi_users` que já exista — inclusive a versão
anterior, baseada em Entra ID. Linhas antigas ficam sem senha e entram como inativas; um
administrador redefine a senha delas pela tela e elas voltam.

Nenhuma senha, nem hash de senha, fica no script.

### 2. Chave de assinatura

```bash
openssl rand -base64 48
```

Guarde em `Auth:JwtSigningKey` no `keys.json`, ou na variável `Auth__JwtSigningKey`. Menos
de 32 bytes e a aplicação recusa subir — HMAC-SHA256 exige isso.

Trocar a chave invalida todas as sessões abertas.

### 3. Backend

```bash
cp keys.example.json keys.json      # na RAIZ; preencha ConnectionStrings:Contabil
cd backend/PainelWebSwissPark.Api
dotnet restore
dotnet run
```

O `keys.json` fica na raiz, ao lado de `backend/` e `frontend/`, e está no `.gitignore`. A
aplicação sobe de diretório a partir da pasta do projeto até encontrá-lo. `PAINEL_KEYS_FILE`
aponta para outro caminho, se preferir.

Para rodar tudo em container, veja [`DOCKER.md`](DOCKER.md).

### 4. Frontend

```bash
cd frontend
npm install
npm run dev
```

### 5. Conferência

```
GET  /api/health              -> 200 sem token
POST /api/auth/login          -> 200 com credenciais certas, 401 com erradas
GET  /api/auth/me             -> 401 sem token, 200 com token
GET  /api/usuarios            -> 403 para Gerente/Analista, 200 para Administrador
```

Teste a revogação: entre com uma conta, desative-a pela tela em outro navegador e recarregue
a primeira. O acesso cai na primeira requisição — não no próximo login.
