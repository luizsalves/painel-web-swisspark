-- ============================================================================
-- 001_bi_users.sql — autorização do Painel de Conciliação Contábil
--
-- Roda no Postgres `contabil`. Script idempotente: pode rodar de novo sem estragar
-- nada, e reconcilia uma bi_users que já exista com colunas faltando.
--
-- Schema: está escrito para `public`. Para usar um schema separado (recomendado
-- quando o banco do painel existir), troque `public.` por `painel.` aqui e ajuste
-- PainelDb:Schema no appsettings.json — o código não precisa mudar.
--
-- Por que não tem coluna de senha: quem autentica é o Entra ID. Esta tabela só
-- responde "essa pessoa pode entrar, e com qual perfil".
-- ============================================================================

-- gen_random_uuid() é nativo no Postgres 13+. Em versões anteriores, descomente:
-- create extension if not exists pgcrypto;

-- create schema if not exists painel;

-- ----------------------------------------------------------------------------
-- Quem pode entrar
-- ----------------------------------------------------------------------------
create table if not exists public.bi_users (
  id                uuid primary key default gen_random_uuid(),
  entra_object_id   uuid,
  email             text        not null,
  name              text        not null,
  role              text        not null,
  is_active         boolean     not null default true,
  created_at        timestamptz not null default now(),
  created_by        uuid,
  updated_at        timestamptz not null default now(),
  updated_by        uuid
);

-- Reconcilia uma tabela criada antes deste script.
alter table public.bi_users add column if not exists entra_object_id uuid;
alter table public.bi_users add column if not exists is_active  boolean     not null default true;
alter table public.bi_users add column if not exists created_at timestamptz not null default now();
alter table public.bi_users add column if not exists created_by uuid;
alter table public.bi_users add column if not exists updated_at timestamptz not null default now();
alter table public.bi_users add column if not exists updated_by uuid;

-- O oid do Entra identifica a pessoa no tenant e é imutável — e-mail corporativo não é.
-- Sem este unique, duas linhas poderiam apontar para a mesma pessoa.
create unique index if not exists ux_bi_users_entra_object_id
  on public.bi_users (entra_object_id)
  where entra_object_id is not null;

-- Postgres diferencia maiúsculas: sem isso, "Luiz@x.com" e "luiz@x.com" entrariam
-- como dois usuários e o login casaria com um ou outro conforme a digitação.
create unique index if not exists ux_bi_users_email_lower
  on public.bi_users (lower(email));

-- Sem o check, um 'Adminstrador' com erro de digitação entra no banco e a pessoa
-- perde acesso sem nenhum erro visível.
do $$
begin
  if not exists (
    select 1 from pg_constraint where conname = 'ck_bi_users_role'
  ) then
    alter table public.bi_users
      add constraint ck_bi_users_role
      check (role in ('Administrador', 'Gerente', 'Analista'));
  end if;
end $$;

-- created_by/updated_by apontam para a própria tabela e são nulos no registro de
-- bootstrap, que não foi criado por ninguém.
do $$
begin
  if not exists (
    select 1 from pg_constraint where conname = 'fk_bi_users_created_by'
  ) then
    alter table public.bi_users
      add constraint fk_bi_users_created_by
      foreign key (created_by) references public.bi_users(id);
  end if;

  if not exists (
    select 1 from pg_constraint where conname = 'fk_bi_users_updated_by'
  ) then
    alter table public.bi_users
      add constraint fk_bi_users_updated_by
      foreign key (updated_by) references public.bi_users(id);
  end if;
end $$;

-- ----------------------------------------------------------------------------
-- Trilha de auditoria da área administrativa
-- ----------------------------------------------------------------------------
create table if not exists public.bi_users_audit (
  id            bigint generated always as identity primary key,
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
-- Bootstrap do primeiro administrador
--
-- A tela de administração exige perfil Administrador, e a tabela nasce vazia:
-- sem esta linha ninguém consegue entrar. Nunca resolva isso com "o primeiro que
-- logar vira admin" — qualquer pessoa do tenant viraria dona do sistema.
--
-- TROQUE O E-MAIL E O NOME ABAIXO antes de rodar. O entra_object_id fica nulo de
-- propósito: o backend grava no primeiro login.
-- ----------------------------------------------------------------------------
insert into public.bi_users (email, name, role)
values (lower('SEU_EMAIL@swisspark.com.br'), 'Seu Nome', 'Administrador')
on conflict do nothing;

-- ----------------------------------------------------------------------------
-- Conferência
-- ----------------------------------------------------------------------------
-- select table_schema, column_name, data_type, is_nullable, column_default
-- from information_schema.columns
-- where table_name = 'bi_users'
-- order by ordinal_position;
