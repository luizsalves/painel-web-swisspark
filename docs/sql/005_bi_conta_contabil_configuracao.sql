-- ============================================================================
-- 005_bi_conta_contabil_configuracao.sql — ativar/desativar conta contábil
--
-- Roda no mesmo Postgres da bi_users. Script idempotente.
--
-- CCONTA (TOTVS RM — corpore/corpore_bsb/corpore_mao/corpore_neu) é somente
-- leitura para o painel, então esse flag não pode morar lá: é um override do
-- próprio painel. Ausência de linha para uma cod_coligada+cod_conta significa
-- ativa (default) — a tabela só ganha uma linha quando alguém explicitamente
-- ativa ou desativa pela tela de Contas Contábeis. Uma conta desativada some
-- do combobox "Conta Contábil" da tela de Fechamentos.
-- ============================================================================

create table if not exists public.bi_conta_contabil_configuracao (
  cod_coligada    smallint     not null,
  cod_conta       text         not null,
  ativo           boolean      not null default true,
  atualizado_por  uuid         not null references public.bi_users(id),
  atualizado_em   timestamptz  not null default now(),
  primary key (cod_coligada, cod_conta)
);
