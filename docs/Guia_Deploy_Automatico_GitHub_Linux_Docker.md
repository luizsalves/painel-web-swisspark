# Guia Completo — Deploy Automático com GitHub + Linux + Docker Compose

Este documento descreve o processo completo para configurar um deploy automático em um servidor Linux usando GitHub, usuário `deploy`, SSH/Deploy Key, Docker Compose, script de deploy, `cron` e testes finais.

## Fluxo final

```text
Máquina de desenvolvimento
        ↓
git add / git commit / git push
        ↓
GitHub - branch main
        ↓
Servidor Linux verifica o GitHub
        ↓
git fetch
        ↓
Detecta novo commit
        ↓
git pull
        ↓
docker compose up -d --build
        ↓
Nova versão publicada
```

## 1. Verificar pré-requisitos

No servidor:

```bash
git --version
docker --version
docker compose version
```

O repositório deve existir no GitHub e, neste exemplo, usar a branch `main`.

## 2. Criar o usuário `deploy`

Primeiro verifique se ele já existe:

```bash
id deploy
```

Se existir, aparecerá algo parecido com:

```text
uid=1001(deploy) gid=1001(deploy) groups=1001(deploy)
```

Se não existir:

```bash
useradd -m -s /bin/bash deploy
```

Se desejar definir senha:

```bash
passwd deploy
```

## 3. Dar acesso ao Docker para o usuário `deploy`

Verifique:

```bash
groups deploy
```

Se `docker` não estiver listado:

```bash
usermod -aG docker deploy
```

Teste:

```bash
sudo -u deploy docker ps
```

## 4. Criar a pasta do projeto

Exemplo:

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

## 5. Criar a pasta SSH do usuário `deploy`

```bash
mkdir -p /home/deploy/.ssh
chown -R deploy:deploy /home/deploy/.ssh
chmod 700 /home/deploy/.ssh
```

## 6. Criar uma chave SSH para o projeto

É recomendável usar uma chave separada para cada projeto.

Exemplo:

```bash
sudo -u deploy ssh-keygen -t ed25519 -C "painel-web-swisspark"
```

Quando pedir o caminho:

```text
/home/deploy/.ssh/id_ed25519_swisspark
```

Quando pedir passphrase, pressione Enter duas vezes.

Serão criados:

```text
/home/deploy/.ssh/id_ed25519_swisspark
/home/deploy/.ssh/id_ed25519_swisspark.pub
```

A primeira é privada. Nunca compartilhe.

## 7. Mostrar a chave pública

```bash
sudo -u deploy cat /home/deploy/.ssh/id_ed25519_swisspark.pub
```

Copie a linha inteira.

## 8. Cadastrar a Deploy Key no GitHub

No repositório:

```text
Settings
→ Deploy keys
→ Add deploy key
```

Exemplo:

```text
Title:
Servidor Swisspark
```

Cole a chave pública.

Para o servidor apenas baixar código, não é necessário marcar `Allow write access`.

O repositório pode continuar privado.

## 9. Configurar o SSH do usuário `deploy`

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

Se já existir outro projeto, não apague. Exemplo:

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

## 10. Testar o acesso ao GitHub

```bash
sudo -u deploy ssh -T git@github-swisspark
```

Na primeira conexão, se aparecer:

```text
Are you sure you want to continue connecting (yes/no/[fingerprint])?
```

digite:

```text
yes
```

Resultado esperado:

```text
Hi usuario/repositorio! You've successfully authenticated, but GitHub does not provide shell access.
```

## 11. Clonar o repositório

Saia da pasta de destino antes de clonar:

```bash
cd /opt/projetos
```

Garanta que a pasta pertence ao `deploy`:

```bash
mkdir -p /opt/projetos/painel-web-swisspark
chown deploy:deploy /opt/projetos/painel-web-swisspark
```

Clone:

```bash
sudo -u deploy git clone git@github-swisspark:luizsalves/painel-web-swisspark.git /opt/projetos/painel-web-swisspark
```

## 12. Validar o Git

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

## 13. Evitar erro `dubious ownership`

Se o repositório pertence ao usuário `deploy`, execute Git como `deploy`:

```bash
sudo -u deploy git -C /opt/projetos/painel-web-swisspark status
```

Evite operar esse repositório com Git diretamente como `root`.

## 14. Criar o `.env`

Se existir `.env.example`:

```bash
cd /opt/projetos/painel-web-swisspark
sudo -u deploy cp .env.example .env
```

Edite:

```bash
nano .env
```

Exemplo:

```env
PAINEL_DB_CONNECTION_STRING=Host=SERVIDOR;Port=5432;Database=contabil;Username=USUARIO;Password=SENHA;SSL Mode=Prefer;Trust Server Certificate=true

PAINEL_DB_SCHEMA=public
PAINEL_DB_CACHE_SECONDS=0

PAINEL_ENTRA_TENANT_ID=
PAINEL_ENTRA_CLIENT_ID=
PAINEL_ENTRA_API_SCOPE=

PAINEL_ENTRA_REDIRECT_URI=http://SEU_IP_OU_DOMINIO:8080

PAINEL_PORTA=8080
```

Proteja:

```bash
chown deploy:deploy .env
chmod 600 .env
```

O `.env` real deve ficar fora do GitHub.

## 15. Confirmar que `.env` está ignorado pelo Git

```bash
git check-ignore .env
```

Se retornar:

```text
.env
```

está correto.

## 16. Testar Docker antes de automatizar

```bash
cd /opt/projetos/painel-web-swisspark
docker compose config
```

Se não houver erro:

```bash
docker compose up -d --build
```

Confira:

```bash
docker compose ps
```

Logs:

```bash
docker compose logs --tail=100
```

## 17. Criar a pasta de deploy

```bash
mkdir -p /opt/projetos/painel-web-swisspark/.deploy
chown -R deploy:deploy /opt/projetos/painel-web-swisspark/.deploy
```

## 18. Criar o script `auto-deploy.sh`

```bash
nano /opt/projetos/painel-web-swisspark/.deploy/auto-deploy.sh
```

Conteúdo:

```bash
#!/bin/bash
set -euo pipefail

PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin

PROJECT_DIR="/opt/projetos/painel-web-swisspark"
LOG="$PROJECT_DIR/.deploy/deploy.log"

cd "$PROJECT_DIR"

echo "[$(date '+%F %T')] Verificando GitHub..." >> "$LOG"

git fetch origin main >> "$LOG" 2>&1

LOCAL=$(git rev-parse HEAD)
REMOTE=$(git rev-parse origin/main)

if [ "$LOCAL" = "$REMOTE" ]; then
    echo "[$(date '+%F %T')] Nenhuma atualização." >> "$LOG"
    exit 0
fi

echo "[$(date '+%F %T')] Nova versão encontrada." >> "$LOG"

git pull --ff-only origin main >> "$LOG" 2>&1

echo "[$(date '+%F %T')] Reconstruindo Docker..." >> "$LOG"

docker compose up -d --build >> "$LOG" 2>&1

echo "[$(date '+%F %T')] Deploy concluído." >> "$LOG"
```

Salve no nano com:

```text
CTRL + O
ENTER
CTRL + X
```

## 19. Dar permissão ao script

```bash
chmod +x /opt/projetos/painel-web-swisspark/.deploy/auto-deploy.sh
chown deploy:deploy /opt/projetos/painel-web-swisspark/.deploy/auto-deploy.sh
```

Confira:

```bash
ls -l /opt/projetos/painel-web-swisspark/.deploy/auto-deploy.sh
```

## 20. Testar o script manualmente

Execute como o usuário `deploy`:

```bash
sudo -u deploy /opt/projetos/painel-web-swisspark/.deploy/auto-deploy.sh
```

Confira o log:

```bash
cat /opt/projetos/painel-web-swisspark/.deploy/deploy.log
```

Se estiver sincronizado:

```text
Verificando GitHub...
Nenhuma atualização.
```

## 21. Criar o cron

Abra:

```bash
sudo -u deploy crontab -e
```

Se pedir editor, escolha:

```text
1. /bin/nano
```

Adicione:

```cron
* * * * * /usr/bin/flock -n /tmp/painel-swisspark-deploy.lock /opt/projetos/painel-web-swisspark/.deploy/auto-deploy.sh
```

Isso executa a checagem a cada minuto.

O `flock` evita dois deploys simultâneos.

## 22. Manter outros deploys

Não apague outras linhas existentes.

Exemplo:

```cron
* * * * * /usr/bin/flock -n /tmp/react-restaurante-deploy.lock "/opt/projetos/Cursos/React: integrando seu projeto React com APIs/react-integrando-api-restaurante/.deploy/auto-deploy.sh"

* * * * * /usr/bin/flock -n /tmp/painel-swisspark-deploy.lock /opt/projetos/painel-web-swisspark/.deploy/auto-deploy.sh
```

## 23. Confirmar o cron

```bash
sudo -u deploy crontab -l
```

A linha do projeto deve aparecer.

## 24. Teste final de ponta a ponta

Na máquina de desenvolvimento:

```bash
git status
git add .
git commit -m "Teste deploy automatico"
git push
```

Espere aproximadamente 1 minuto.

No servidor:

```bash
sudo -u deploy git -C /opt/projetos/painel-web-swisspark log -1 --oneline
```

O commit deve ser o mesmo enviado pelo computador.

## 25. Conferir o log do deploy

```bash
tail -100 /opt/projetos/painel-web-swisspark/.deploy/deploy.log
```

Em um deploy completo:

```text
Verificando GitHub...
Nova versão encontrada.
Reconstruindo Docker...
Deploy concluído.
```

## 26. Conferir containers

```bash
cd /opt/projetos/painel-web-swisspark
docker compose ps
```

Os serviços devem estar `Up`.

Se houver problema:

```bash
docker compose logs --tail=200
```

## 27. Confirmar que o cron executa sozinho

```bash
tail -20 /opt/projetos/painel-web-swisspark/.deploy/deploy.log
```

Mesmo sem alterações, devem aparecer registros periódicos:

```text
Verificando GitHub...
Nenhuma atualização.
```

## 28. Comandos úteis de diagnóstico

Status Git:

```bash
sudo -u deploy git -C /opt/projetos/painel-web-swisspark status
```

Último commit:

```bash
sudo -u deploy git -C /opt/projetos/painel-web-swisspark log -1 --oneline
```

Remote:

```bash
sudo -u deploy git -C /opt/projetos/painel-web-swisspark remote -v
```

Testar SSH:

```bash
sudo -u deploy ssh -T git@github-swisspark
```

Testar fetch:

```bash
sudo -u deploy git -C /opt/projetos/painel-web-swisspark fetch origin main
```

Ver cron:

```bash
sudo -u deploy crontab -l
```

Ver containers:

```bash
cd /opt/projetos/painel-web-swisspark
docker compose ps
```

Ver log:

```bash
tail -100 /opt/projetos/painel-web-swisspark/.deploy/deploy.log
```

Executar deploy manual:

```bash
sudo -u deploy /opt/projetos/painel-web-swisspark/.deploy/auto-deploy.sh
```

## 29. Erros comuns

### `Permission denied`

Corrija:

```bash
chown -R deploy:deploy /opt/projetos/painel-web-swisspark
```

### `detected dubious ownership`

Use Git como `deploy`:

```bash
sudo -u deploy git -C /opt/projetos/painel-web-swisspark status
```

### `destination path already exists`

Se o clone já existe, atualize:

```bash
sudo -u deploy git -C /opt/projetos/painel-web-swisspark pull origin main
```

Se realmente quiser clonar novamente:

```bash
cd /opt/projetos
rm -rf /opt/projetos/painel-web-swisspark
mkdir -p /opt/projetos/painel-web-swisspark
chown deploy:deploy /opt/projetos/painel-web-swisspark
```

Depois clone novamente.

### `Password authentication is not supported`

Não use a senha normal do GitHub.

Use SSH + Deploy Key.

### Variável obrigatória ausente

Exemplo:

```text
PAINEL_DB_CONNECTION_STRING is missing
```

Verifique:

```bash
nano .env
docker compose config
```

### Git atualizou, mas o site não mudou

Confira:

```bash
sudo -u deploy git -C /opt/projetos/painel-web-swisspark log -1 --oneline
```

Depois:

```bash
tail -100 /opt/projetos/painel-web-swisspark/.deploy/deploy.log
```

E:

```bash
docker compose ps
```

## 30. Estrutura recomendada

```text
/opt/projetos/
│
├── painel-web-swisspark/
│   ├── .git/
│   ├── .deploy/
│   │   ├── auto-deploy.sh
│   │   └── deploy.log
│   ├── .env
│   ├── .env.example
│   ├── docker-compose.yml
│   └── ...
│
└── outro-projeto/
```

SSH:

```text
/home/deploy/
└── .ssh/
    ├── config
    ├── id_ed25519_swisspark
    ├── id_ed25519_swisspark.pub
    ├── react_restaurante_deploy
    ├── react_restaurante_deploy.pub
    └── known_hosts
```

## 31. Checklist final

```text
[ ] usuário deploy existe
[ ] deploy consegue executar Docker
[ ] pasta do projeto pertence ao deploy
[ ] chave SSH foi criada
[ ] Deploy Key foi cadastrada no GitHub
[ ] /home/deploy/.ssh/config está correto
[ ] ssh -T para o GitHub funciona
[ ] repositório foi clonado como deploy
[ ] branch main está correta
[ ] remote origin está correto
[ ] .env existe no servidor
[ ] .env não está versionado
[ ] docker compose config funciona
[ ] docker compose up -d --build funciona
[ ] .deploy/auto-deploy.sh existe
[ ] script pertence ao deploy
[ ] script tem permissão de execução
[ ] execução manual do script funciona
[ ] cron do usuário deploy está configurado
[ ] flock possui nome exclusivo
[ ] git push gera atualização no servidor
[ ] último commit no servidor corresponde ao GitHub
[ ] deploy.log mostra Deploy concluído
[ ] containers estão Up
[ ] aplicação está acessível
```

## 32. Uso diário

Depois de tudo configurado, na máquina de desenvolvimento:

```bash
git status
git add .
git commit -m "Descrição da alteração"
git push
```

O servidor fará automaticamente:

```text
git fetch
→ compara commits
→ git pull
→ docker compose up -d --build
→ aplicação atualizada
```

## 33. Exemplo real deste projeto

Projeto:

```text
painel-web-swisspark
```

Pasta:

```text
/opt/projetos/painel-web-swisspark
```

Usuário:

```text
deploy
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

Script:

```text
/opt/projetos/painel-web-swisspark/.deploy/auto-deploy.sh
```

Log:

```text
/opt/projetos/painel-web-swisspark/.deploy/deploy.log
```

Cron:

```cron
* * * * * /usr/bin/flock -n /tmp/painel-swisspark-deploy.lock /opt/projetos/painel-web-swisspark/.deploy/auto-deploy.sh
```

## 34. Regra para novos projetos

Para cada projeto novo:

```text
1. criar/verificar usuário deploy
2. criar pasta do projeto
3. atribuir pasta ao deploy
4. criar chave SSH própria
5. cadastrar Deploy Key no GitHub
6. criar Host próprio no ~/.ssh/config
7. testar SSH
8. clonar como deploy
9. criar .env
10. testar Docker
11. criar .deploy
12. criar auto-deploy.sh
13. criar lock exclusivo
14. adicionar cron
15. fazer git push de teste
16. conferir commit, log e containers
```

Assim cada projeto fica isolado e um deploy não interfere no outro.
