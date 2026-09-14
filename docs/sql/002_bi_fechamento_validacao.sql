-- ============================================================================
-- 002_bi_fechamento_validacao.sql — trilha de preparação/conferência/fechamento
--
-- Roda no mesmo Postgres da bi_users. Script idempotente.
--
-- Cada ação (Preparar, Conferir, Realizar Fechamento, Reverter) grava uma linha
-- NOVA — a tabela nunca é atualizada, só recebe inserts. O status "atual" de uma
-- conta/mês é o status da linha mais recente para aquela chave
-- (cod_coligada, cod_filial, cod_conta, ano_mes). Isso dá a trilha completa de
-- quem preparou e quando, quem conferiu e quando, sem precisar de colunas
-- separadas pra cada evento.
-- ============================================================================

create table if not exists public.bi_fechamento_validacao (
  id                        bigserial   primary key,
  cod_coligada              smallint    not null,
  cod_filial                smallint    not null,
  cod_conta                 text        not null,
  ano_mes                   char(7)     not null,
  status                    text        not null,
  prioridade                smallint,
  saldo_anterior            numeric(18,4),
  saldo_atual               numeric(18,4),
  data_fechamento           date,
  pendencia_texto           text,
  pendencia_responsavel_id  uuid        references public.bi_users(id),
  pendencia_registrada_em   timestamptz,
  realizado_por             uuid        not null references public.bi_users(id),
  realizado_em              timestamptz not null default now()
);

-- Busca o histórico (e a linha mais recente) de uma conta/mês.
create index if not exists ix_bi_fechamento_validacao_chave
  on public.bi_fechamento_validacao (cod_coligada, cod_filial, cod_conta, ano_mes, realizado_em desc);

do $$
begin
  if not exists (select 1 from pg_constraint where conname = 'ck_bi_fechamento_validacao_status') then
    alter table public.bi_fechamento_validacao
      add constraint ck_bi_fechamento_validacao_status
      check (status in ('Pendente', 'Preparado', 'Conferido', 'Fechado'));
  end if;

  if not exists (select 1 from pg_constraint where conname = 'ck_bi_fechamento_validacao_prioridade') then
    alter table public.bi_fechamento_validacao
      add constraint ck_bi_fechamento_validacao_prioridade
      check (prioridade is null or prioridade between 1 and 4);
  end if;
end $$;
