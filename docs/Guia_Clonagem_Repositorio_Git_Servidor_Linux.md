# Guia — Clonagem de Repositório Git no Servidor Linux

Este documento mostra como clonar corretamente um repositório GitHub em um servidor Linux, usando um usuário dedicado `deploy`, com suporte a repositórios públicos e privados.

## 1. Verificar se o Git está instalado

```bash
git --version
```

Se não estiver instalado:

### Ubuntu / Debian

```bash
apt update
apt install git -y
```

---

## 2. Verificar se o usuário `deploy` existe

```bash
id deploy
```

Se aparecer algo parecido com:

```text
uid=1001(deploy) gid=1001(deploy) groups=1001(deploy)
```

ele já existe.

Se não existir:

```bash
useradd -m -s /bin/bash deploy
```

---

## 3. Criar a pasta onde os projetos ficarão

Exemplo:

```bash
mkdir -p /opt/projetos
```

Para um projeto chamado:

```text
painel-web-swisspark
```

crie:

```bash
mkdir -p /opt/projetos/painel-web-swisspark
```

Dê a pasta ao usuário `deploy`:

```bash
chown -R deploy:deploy /opt/projetos/painel-web-swisspark
```

Confira:

```bash
ls -ld /opt/projetos/painel-web-swisspark
```

O proprietário deve aparecer como:

```text
deploy deploy
```

---

# 4. Clonar um repositório público

Se o repositório for público, você pode usar HTTPS.

Exemplo:

```bash
sudo -u deploy git clone https://github.com/USUARIO/REPOSITORIO.git /opt/projetos/NOME-PROJETO
```

Exemplo real:

```bash
sudo -u deploy git clone https://github.com/luizsalves/painel-web-swisspark.git /opt/projetos/painel-web-swisspark
```

---

# 5. Clonar um repositório privado

Para repositório privado, o recomendado no servidor é usar SSH com Deploy Key.

## 5.1 Criar a pasta SSH do usuário `deploy`

```bash
mkdir -p /home/deploy/.ssh
chown -R deploy:deploy /home/deploy/.ssh
chmod 700 /home/deploy/.ssh
```

---

## 5.2 Criar uma chave SSH específica para o projeto

Exemplo:

```bash
sudo -u deploy ssh-keygen -t ed25519 -C "painel-web-swisspark"
```

Quando pedir o caminho, informe:

```text
/home/deploy/.ssh/id_ed25519_swisspark
```

Quando pedir senha da chave, pressione Enter duas vezes.

Serão criados:

```text
/home/deploy/.ssh/id_ed25519_swisspark
/home/deploy/.ssh/id_ed25519_swisspark.pub
```

A chave sem `.pub` é privada.

Nunca envie essa chave para o GitHub ou para outra pessoa.

---

## 5.3 Mostrar a chave pública

```bash
sudo -u deploy cat /home/deploy/.ssh/id_ed25519_swisspark.pub
```

Copie a linha inteira.

---

## 5.4 Adicionar a Deploy Key no GitHub

No GitHub:

```text
Repositório
→ Settings
→ Deploy keys
→ Add deploy key
```

Exemplo:

```text
Title:
Servidor Swisspark
```

Cole a chave pública.

Para somente clonar e fazer `git pull`, não precisa marcar:

```text
Allow write access
```

O repositório continua privado.

---

# 6. Configurar o arquivo SSH

Abra:

```bash
sudo -u deploy nano /home/deploy/.ssh/config
```

Adicione:

```text
Host github-swisspark
    HostName github.com
    User git
    IdentityFile ~/.ssh/id_ed25519_swisspark
    IdentitiesOnly yes
```

Se já existir outro projeto, não apague.

Exemplo:

```text
Host github-react-restaurante
    HostName github.com
    User git
    IdentityFile ~/.ssh/react_restaurante_deploy
    IdentitiesOnly yes

Host github-swisspark
    HostName github.com
    User git
    IdentityFile ~/.ssh/id_ed25519_swisspark
    IdentitiesOnly yes
```

Ajuste as permissões:

```bash
chown deploy:deploy /home/deploy/.ssh/config
chmod 600 /home/deploy/.ssh/config
```

---

# 7. Testar a autenticação SSH

```bash
sudo -u deploy ssh -T git@github-swisspark
```

Na primeira vez pode aparecer:

```text
Are you sure you want to continue connecting (yes/no/[fingerprint])?
```

Digite:

```text
yes
```

Resultado esperado:

```text
Hi USUARIO/REPOSITORIO! You've successfully authenticated, but GitHub does not provide shell access.
```

---

# 8. Clonar o repositório privado

Antes de clonar, vá para:

```bash
cd /opt/projetos
```

Garanta que a pasta destino pertence ao `deploy`:

```bash
mkdir -p /opt/projetos/painel-web-swisspark
chown deploy:deploy /opt/projetos/painel-web-swisspark
```

Agora clone:

```bash
sudo -u deploy git clone git@github-swisspark:luizsalves/painel-web-swisspark.git /opt/projetos/painel-web-swisspark
```

---

# 9. Validar a clonagem

Confira o status:

```bash
sudo -u deploy git -C /opt/projetos/painel-web-swisspark status
```

Resultado esperado:

```text
On branch main
Your branch is up to date with 'origin/main'.

nothing to commit, working tree clean
```

Confira o remote:

```bash
sudo -u deploy git -C /opt/projetos/painel-web-swisspark remote -v
```

Resultado esperado:

```text
origin  git@github-swisspark:luizsalves/painel-web-swisspark.git (fetch)
origin  git@github-swisspark:luizsalves/painel-web-swisspark.git (push)
```

Confira a branch:

```bash
sudo -u deploy git -C /opt/projetos/painel-web-swisspark branch
```

Resultado esperado:

```text
* main
```

---

# 10. Atualizar um repositório já clonado

Você não precisa clonar novamente toda vez.

Use:

```bash
sudo -u deploy git -C /opt/projetos/painel-web-swisspark pull origin main
```

Ou primeiro:

```bash
sudo -u deploy git -C /opt/projetos/painel-web-swisspark fetch origin main
```

E depois:

```bash
sudo -u deploy git -C /opt/projetos/painel-web-swisspark pull --ff-only origin main
```

---

# 11. Ver o último commit

```bash
sudo -u deploy git -C /opt/projetos/painel-web-swisspark log -1 --oneline
```

Exemplo:

```text
5b83ece Alteracao da pagina home
```

---

# 12. Se precisar apagar e clonar novamente

Primeiro saia da pasta:

```bash
cd /opt/projetos
```

Nunca apague a pasta enquanto o terminal estiver dentro dela.

Remova:

```bash
rm -rf /opt/projetos/painel-web-swisspark
```

Crie novamente:

```bash
mkdir -p /opt/projetos/painel-web-swisspark
chown deploy:deploy /opt/projetos/painel-web-swisspark
```

Depois clone:

```bash
sudo -u deploy git clone git@github-swisspark:luizsalves/painel-web-swisspark.git /opt/projetos/painel-web-swisspark
```

---

# 13. Erro: destination path already exists

Erro:

```text
fatal: destination path '...' already exists and is not an empty directory
```

Isso significa que a pasta já existe e tem arquivos.

Se o repositório já está clonado, use:

```bash
sudo -u deploy git -C /opt/projetos/painel-web-swisspark pull origin main
```

Se realmente quiser clonar do zero:

```bash
cd /opt/projetos
rm -rf /opt/projetos/painel-web-swisspark
mkdir -p /opt/projetos/painel-web-swisspark
chown deploy:deploy /opt/projetos/painel-web-swisspark
```

E clone novamente.

---

# 14. Erro: Permission denied

Exemplo:

```text
.git: Permission denied
```

Normalmente a pasta pertence ao `root`.

Corrija:

```bash
chown -R deploy:deploy /opt/projetos/painel-web-swisspark
```

Depois tente novamente.

---

# 15. Erro: dubious ownership

Erro:

```text
fatal: detected dubious ownership in repository
```

Isso normalmente acontece porque o repositório pertence ao `deploy`, mas o Git foi executado como `root`.

Use:

```bash
sudo -u deploy git -C /opt/projetos/painel-web-swisspark status
```

Não é necessário adicionar `safe.directory` se você operar o Git com o usuário correto.

---

# 16. Erro: senha do GitHub não funciona

Erro:

```text
Password authentication is not supported for Git operations.
```

O GitHub não aceita senha comum da conta para operações Git por HTTPS.

No servidor, prefira:

```text
SSH + Deploy Key
```

---

# 17. Erro: Invalid username or token

Exemplo:

```text
remote: Invalid username or token.
```

Isso pode acontecer se você estiver tentando usar HTTPS com usuário ou senha incorretos.

Para servidor com repositório privado, use SSH.

---

# 18. Erro: Unable to read current working directory

Erro:

```text
fatal: Unable to read current working directory: No such file or directory
```

Isso acontece quando a pasta atual foi apagada enquanto o terminal ainda estava dentro dela.

Resolva:

```bash
cd /opt/projetos
```

Depois execute novamente o comando.

---

# 19. Verificar permissões do projeto

```bash
ls -ld /opt/projetos/painel-web-swisspark
```

Resultado desejado:

```text
deploy deploy
```

Se necessário:

```bash
chown -R deploy:deploy /opt/projetos/painel-web-swisspark
```

---

# 20. Verificar arquivos SSH

```bash
ls -la /home/deploy/.ssh
```

Exemplo:

```text
config
id_ed25519_swisspark
id_ed25519_swisspark.pub
known_hosts
```

---

# 21. Testes finais após a clonagem

## Testar GitHub

```bash
sudo -u deploy ssh -T git@github-swisspark
```

## Testar status

```bash
sudo -u deploy git -C /opt/projetos/painel-web-swisspark status
```

## Testar remote

```bash
sudo -u deploy git -C /opt/projetos/painel-web-swisspark remote -v
```

## Testar branch

```bash
sudo -u deploy git -C /opt/projetos/painel-web-swisspark branch
```

## Testar fetch

```bash
sudo -u deploy git -C /opt/projetos/painel-web-swisspark fetch origin main
```

## Testar pull

```bash
sudo -u deploy git -C /opt/projetos/painel-web-swisspark pull --ff-only origin main
```

## Ver último commit

```bash
sudo -u deploy git -C /opt/projetos/painel-web-swisspark log -1 --oneline
```

---

# 22. Checklist rápido

```text
[ ] Git instalado
[ ] usuário deploy existe
[ ] pasta do projeto criada
[ ] pasta pertence ao deploy
[ ] chave SSH criada
[ ] Deploy Key cadastrada no GitHub
[ ] ~/.ssh/config configurado
[ ] ssh -T funciona
[ ] clone realizado
[ ] branch main correta
[ ] remote origin correto
[ ] git status sem erro
[ ] git fetch funciona
[ ] git pull funciona
```

---

# 23. Exemplo completo usado

Projeto:

```text
painel-web-swisspark
```

Usuário Linux:

```text
deploy
```

Pasta:

```text
/opt/projetos/painel-web-swisspark
```

Alias SSH:

```text
github-swisspark
```

Chave:

```text
/home/deploy/.ssh/id_ed25519_swisspark
```

Repositório:

```text
git@github-swisspark:luizsalves/painel-web-swisspark.git
```

Clone:

```bash
sudo -u deploy git clone git@github-swisspark:luizsalves/painel-web-swisspark.git /opt/projetos/painel-web-swisspark
```

Status:

```bash
sudo -u deploy git -C /opt/projetos/painel-web-swisspark status
```

Atualização:

```bash
sudo -u deploy git -C /opt/projetos/painel-web-swisspark pull --ff-only origin main
```

---

# 24. Regra recomendada

Para repositórios privados em servidor:

```text
1. usar usuário deploy
2. criar chave SSH separada por projeto
3. cadastrar como Deploy Key
4. criar alias no ~/.ssh/config
5. clonar como deploy
6. executar Git sempre como deploy
```

Isso evita conflitos de permissão e facilita a automação de deploy depois.
