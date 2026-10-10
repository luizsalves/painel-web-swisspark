-- ============================================================================
-- 006_ajuste_email_kelli.sql — troca o e-mail de login da kelli
--
-- Roda no mesmo Postgres da bi_users. Script idempotente: o update só pega a
-- linha enquanto o e-mail ainda for o antigo, e a auditoria só é gravada se o
-- update de fato alterou algo.
--
-- kely@swisspark.com.br -> kelli@swisspark.com.br
-- A tela de Usuários não edita e-mail (o "Editar" só mexe em nome e módulos),
-- por isso o ajuste é manual. A senha não muda.
-- ============================================================================

begin;

with alterado as (
  update public.bi_users
     set email = 'kelli@swisspark.com.br',
         updated_at = now()
   where id = '843d7c04-df12-4590-babc-02cf0ab24ef9'
     and email = 'kely@swisspark.com.br'
  returning id
)
insert into public.bi_users_audit (target_user_id, action, details, performed_at)
select id,
       'atualizado',
       'e-mail: kely@swisspark.com.br -> kelli@swisspark.com.br (ajuste manual)',
       now()
  from alterado;

commit;
