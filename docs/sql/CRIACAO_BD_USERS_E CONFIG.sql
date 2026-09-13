$ cat docs/sql/001_bi_users.sql

-- ============================================================================
-- 001_bi_users.sql — usuários e autorização do Painel de Conciliação Contábil
--
-- Roda no Postgres `contabil`. Script idempotente: pode rodar de novo sem
-- estragar nada, e reconcilia uma bi_users que já exista — inclusive a versão
-- anterior, que era baseada em Entra ID.
--
-- Schema: está escrito para `public`. Para usar um schema separado (recomendado
-- quando o banco do painel existir), troque `public.` por `painel.` aqui e
-- ajuste PainelDb:Schema no appsettings.json — o código não precisa mudar.
--
-- A senha nunca é guardada em texto: a coluna password_hash recebe o hash
-- PBKDF2 produzido pelo PasswordHasher do ASP.NET Core.
--
-- O primeiro administrador NÃO é criado aqui. Ele vem das variáveis
-- PAINEL_ADMIN_EMAIL / PAINEL_ADMIN_PASSWORD na primeira subida da aplicação —
-- assim nenhuma senha, nem hash de senha conhecida, fica versionada.
-- ============================================================================

-- create schema if not exists painel;

-- ----------------------------------------------------------------------------
-- gen_random_uuid()
--
-- Nativo no Postgres 13+. Antes disso vem da extensão pgcrypto, que costuma
-- exigir superusuário para instalar. Se não der, tudo bem: quem gera o id é
-- sempre a aplicação, e o default só serve para insert manual. O bloco abaixo
-- tenta, e segue em frente se não conseguir.
-- ----------------------------------------------------------------------------
do $$
begin
  create extension if not exists pgcrypto;
exception
  when others then
    raise notice 'pgcrypto não instalada (%). Seguindo sem default no id — a aplicação gera.', sqlerrm;
end $$;

-- ----------------------------------------------------------------------------
-- Quem pode entrar
-- ----------------------------------------------------------------------------
create table if not exists public.bi_users (
  id                    uuid primary key,
  email                 text        not null,
  name                  text        not null,
  password_hash         text        not null,
  must_change_password  boolean     not null default true,
  role                  text        not null,
  is_active             boolean     not null default true,
  last_login_at         timestamptz,
  created_at            timestamptz not null default now(),
  created_by            uuid,
  updated_at            timestamptz not null default now(),
  updated_by            uuid
);

-- Reconcilia uma tabela criada antes deste script.
alter table public.bi_users add column if not exists password_hash        text;
alter table public.bi_users add column if not exists must_change_password boolean     not null default true;
alter table public.bi_users add column if not exists last_login_at        timestamptz;
alter table public.bi_users add column if not exists is_active            boolean     not null default true;
alter table public.bi_users add column if not exists created_at           timestamptz not null default now();
alter table public.bi_users add column if not exists created_by           uuid;
alter table public.bi_users add column if not exists updated_at           timestamptz not null default now();
alter table public.bi_users add column if not exists updated_by           uuid;

-- Vindo da versão com Entra ID: o oid não tem mais uso. Linhas antigas ficam sem
-- senha, então entram como inativas — um administrador redefine a senha delas
-- pela tela, e aí voltam.
do $$
begin
  if exists (
    select 1 from information_schema.columns
    where table_schema = 'public' and table_name = 'bi_users' and column_name = 'entra_object_id'
  ) then
    update public.bi_users set is_active = false where password_hash is null;
    alter table public.bi_users drop column entra_object_id;
  end if;
end $$;

-- Só depois de preencher as linhas antigas é que a coluna pode virar obrigatória.
update public.bi_users set password_hash = '' where password_hash is null;
alter table public.bi_users alter column password_hash set not null;

-- O default do id só é aplicado se a função existir neste banco.
do $$
begin
  if exists (
    select 1 from pg_proc p
    join pg_namespace n on n.oid = p.pronamespace
    where p.proname = 'gen_random_uuid'
  ) then
    alter table public.bi_users alter column id set default gen_random_uuid();
  end if;
end $$;

-- Postgres diferencia maiúsculas: sem isso, "Luiz@x.com" e "luiz@x.com" entrariam
-- como dois usuários e o login casaria com um ou outro conforme a digitação.
create unique index if not exists ux_bi_users_email_lower
  on public.bi_users (lower(email));

-- Sem o check, um 'Adminstrador' com erro de digitação entra no banco e a pessoa
-- perde acesso sem nenhum erro visível.
do $$
begin
  if not exists (select 1 from pg_constraint where conname = 'ck_bi_users_role') then
    alter table public.bi_users
      add constraint ck_bi_users_role
      check (role in ('Administrador', 'Gerente', 'Analista'));
  end if;
end $$;

-- created_by/updated_by apontam para a própria tabela e são nulos no registro
-- inicial, que não foi criado por ninguém.
do $$
begin
  if not exists (select 1 from pg_constraint where conname = 'fk_bi_users_created_by') then
    alter table public.bi_users
      add constraint fk_bi_users_created_by
      foreign key (created_by) references public.bi_users(id);
  end if;

  if not exists (select 1 from pg_constraint where conname = 'fk_bi_users_updated_by') then
    alter table public.bi_users
      add constraint fk_bi_users_updated_by
      foreign key (updated_by) references public.bi_users(id);
  end if;
end $$;

-- ----------------------------------------------------------------------------
-- Trilha de auditoria da área administrativa
-- ----------------------------------------------------------------------------
create table if not exists public.bi_users_audit (
  id             bigserial   primary key,
  target_user_id uuid        not null references public.bi_users(id),
  action         text        not null,
  details        text,
  performed_by   uuid        references public.bi_users(id),
  performed_at   timestamptz not null default now()
);

create index if not exists ix_bi_users_audit_target
  on public.bi_users_audit (target_user_id);

create index if not exists ix_bi_users_audit_performed_at
  on public.bi_users_audit (performed_at desc);

-- ----------------------------------------------------------------------------
-- Conferência
-- ----------------------------------------------------------------------------
-- select column_name, data_type, is_nullable, column_default
-- from information_schema.columns
-- where table_name = 'bi_users'
-- order by ordinal_position;

 
 
 
 
 
 
 -- os módulos
create table if not exists public.bi_modules (
  key        text    primary key,
  name       text    not null,
  is_active  boolean not null default true,
  sort_order int     not null default 0
);

insert into public.bi_modules (key, name, sort_order) values
  ('conciliacao', 'Conciliação Contábil', 1),
  ('mkt',         'Marketing',            2)
on conflict (key) do nothing;

-- o perfil de cada pessoa em cada módulo
create table if not exists public.bi_user_modules (
  user_id    uuid        not null references public.bi_users(id) on delete cascade,
  module_key text        not null references public.bi_modules(key),
  role       text        not null,
  created_at timestamptz not null default now(),
  created_by uuid        references public.bi_users(id),
  primary key (user_id, module_key)
);

alter table public.bi_users add column if not exists is_superadmin boolean not null default false;
alter table public.bi_users_audit add column if not exists module_key text;
 
 
 

INSERT INTO bi_users (
    id,
    entra_object_id,
    email,
    name,
    role,
    is_active,
    created_at,
    created_by,
    updated_at,
    updated_by
)
VALUES (
    gen_random_uuid(),
    NULL,
    'luiz@swisspark.com.br',
    'luiz',
    'Administrador',
    TRUE,
    NOW(),
    NULL,
    NOW(),
    NULL
);