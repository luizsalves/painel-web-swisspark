-- ============================================================================
-- 002_modulos.sql — dois sistemas dentro do mesmo painel
--
-- Rode depois do 001. Idempotente: pode rodar de novo sem estragar nada.
--
-- O que muda e por quê:
--
-- O perfil deixa de ser uma propriedade da pessoa e passa a ser da relação
-- entre a pessoa e o módulo. A mesma pessoa pode ser Administrador na
-- conciliação e Analista no mkt — ou simplesmente não existir no mkt.
-- Por isso `role` sai da bi_users e vai para bi_user_modules.
--
-- Quem pertence a um módulo não enxerga o outro: nem as telas, nem os usuários.
-- A exceção é o superadministrador, que existe justamente porque o admin da
-- conciliação não teria como criar o admin do mkt — um não enxerga o outro.
-- ============================================================================

-- ----------------------------------------------------------------------------
-- Os módulos
--
-- Em tabela, e não como constante no código: acrescentar um terceiro módulo
-- um dia é um INSERT, não um deploy.
-- ----------------------------------------------------------------------------
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

-- ----------------------------------------------------------------------------
-- O perfil de cada pessoa em cada módulo
-- ----------------------------------------------------------------------------
create table if not exists public.bi_user_modules (
  user_id    uuid        not null references public.bi_users(id) on delete cascade,
  module_key text        not null references public.bi_modules(key),
  role       text        not null,
  created_at timestamptz not null default now(),
  created_by uuid        references public.bi_users(id),
  primary key (user_id, module_key)
);

create index if not exists ix_bi_user_modules_module
  on public.bi_user_modules (module_key);

do $$
begin
  if not exists (select 1 from pg_constraint where conname = 'ck_bi_user_modules_role') then
    alter table public.bi_user_modules
      add constraint ck_bi_user_modules_role
      check (role in ('Administrador', 'Gerente', 'Analista'));
  end if;
end $$;

-- ----------------------------------------------------------------------------
-- Superadministrador
--
-- Vê e administra os dois módulos. É criado uma única vez, pelas variáveis
-- PAINEL_ADMIN_* na primeira subida — não há tela para promover ninguém a
-- superadministrador, de propósito.
-- ----------------------------------------------------------------------------
alter table public.bi_users add column if not exists is_superadmin boolean not null default false;

-- ----------------------------------------------------------------------------
-- Migração do perfil que estava na bi_users
--
-- Quem já existia pertencia ao único módulo que havia: a conciliação.
-- ----------------------------------------------------------------------------
do $$
begin
  if exists (
    select 1 from information_schema.columns
    where table_schema = 'public' and table_name = 'bi_users' and column_name = 'role'
  ) then
    insert into public.bi_user_modules (user_id, module_key, role)
    select id, 'conciliacao', role
    from public.bi_users
    on conflict (user_id, module_key) do nothing;

    alter table public.bi_users drop column role;
  end if;
end $$;

-- ----------------------------------------------------------------------------
-- Auditoria passa a registrar em qual módulo a alteração aconteceu
-- ----------------------------------------------------------------------------
alter table public.bi_users_audit add column if not exists module_key text;

-- ----------------------------------------------------------------------------
-- Conferência
-- ----------------------------------------------------------------------------
-- select u.email, u.is_superadmin, m.module_key, m.role
-- from public.bi_users u
-- left join public.bi_user_modules m on m.user_id = u.id
-- order by u.email, m.module_key;
