-- Simulação de Preparar -> Conferir -> Realizar (Fechado) em bi_fechamento_validacao.
-- 60 combinações coligada/filial/conta sorteadas do TOTVS (reais), período 2026-08:
--   etapa 1 = só Preparado | etapa 2 = Preparado + Conferido | etapa 3 = Preparado + Conferido + Fechado
-- Preparar: rodízio entre Karime, Juliana, Franciele e Adeline.
-- Conferir: rodízio entre Kely e Mônica. Realizar (Fechado): Luiz.
-- Combinações que já tenham histórico em 2026-08 são ignoradas.
-- Para desfazer: veja o DELETE comentado no final.

BEGIN;

WITH combos (cod_coligada, cod_filial, cod_conta, etapa, saldo_anterior, saldo_atual) AS (
  VALUES
    (8, 1, '7.1.1', 1, 71688.29, 50916.94),
    (18, 1, '3.1.2.01.01.0004', 2, 709567.33, 643835.38),
    (18, 1, '5.2.1.01.01.0022', 3, 388637.29, 476243.93),
    (22, 2, '4.1.1', 1, 718452.83, 768322.32),
    (24, 2, '1.2.2.02.01.0002', 2, -64523.67, -40777.51),
    (28, 2, '5.2.1.01.01.0030', 3, 15247.82, -23842.95),
    (12, 1, '2.1.1.02.01', 1, 645257.15, 549262.27),
    (10, 2, '1.2.1.04', 2, 2860.74, 64724.73),
    (33, 1, '2.3.1.02.02.0001', 3, -107758.15, -30603.02),
    (17, 1, '1.1', 1, 476457.12, 417874.78),
    (4, 1, '2.3.1.01.01.0002', 2, 259296.79, 313234.68),
    (8, 1, '1.2.2.03.02', 3, 131509.42, 114173.88),
    (11, 1, '2.1.1.06.01.0001', 1, 76572.86, 127350.41),
    (18, 2, '3.1.2.01', 2, 656107.87, 577432.56),
    (31, 1, '5.1', 3, 203278.65, 168495.55),
    (14, 1, '2.3.2.02.01.0005', 1, 328344.94, 335630.03),
    (12, 1, '1.1.2.01.01.0003', 2, 574983.16, 644147.07),
    (22, 2, '6.2.1.01.01.0003', 3, 247462.46, 254581.79),
    (12, 2, '6.2.1.01.01.0002', 1, 431612.03, 349842.45),
    (16, 1, '5.1.1.01', 2, -111523.86, -102504.03),
    (28, 2, '3.1.2.01.02.0001', 3, 26577.63, 101081.84),
    (13, 2, '3.1.1.01', 1, -122227.98, -205779.71),
    (8, 2, '1.2.3.05.01.0001', 2, -96730.10, -112091.02),
    (21, 1, '1.1.2.02.01.0003', 3, -87334.99, -66742.55),
    (10, 2, '2.2.1.01.01.0004', 1, 190398.27, 150219.24),
    (22, 2, '7.1.1.01', 2, 390920.79, 406009.74),
    (13, 2, '4.2.1.01.01.0016', 3, -104671.16, -136141.50),
    (9, 2, '1.1.2.01.03.0001', 1, -125032.19, -64029.13),
    (5, 1, '7.3.2.01.01.0002', 2, 26339.99, 40762.42),
    (18, 2, '5.2.1.01.01.0005', 3, 577591.32, 660747.76),
    (18, 2, '7.1.2.01', 1, -81327.35, -3844.90),
    (24, 2, '2.1.1.04.01', 2, 371774.95, 301529.59),
    (5, 1, '4.1.1.01.02.0001', 3, 374786.35, 452437.38),
    (29, 2, '1.1.1.02.01.0001', 1, 267208.36, 258713.19),
    (5, 1, '2.1.1.04.01.0002', 2, 258759.62, 311681.19),
    (30, 1, '2.3.1.01.02.0004', 3, 214436.64, 176452.78),
    (25, 1, '1.1.2.07', 1, 438837.91, 468048.38),
    (4, 1, '3.1.1.01.01.0001', 2, 595593.01, 680900.45),
    (22, 1, '1.2.1.02.02', 3, 660589.09, 572244.81),
    (17, 1, '2.2.1.03.01.0002', 1, 655574.66, 664670.10),
    (20, 1, '5.2.1.01.01.0032', 2, 15231.01, -48794.87),
    (12, 1, '1.2.3.01.06', 3, 5999.66, -29176.23),
    (22, 2, '3.1.1.09', 1, 465914.69, 441243.07),
    (26, 2, '3.1.2.01.01', 2, -123608.32, -158990.25),
    (4, 1, '5.1.1.01.01.0004', 3, 246638.42, 213391.29),
    (24, 1, '1.2.1.04.10.0001', 1, 689013.31, 722851.45),
    (23, 1, '5.1.1.01.01', 2, 647778.74, 551522.13),
    (14, 1, '5.3', 3, 228960.24, 174927.53),
    (15, 1, '1.2.1.07.01.0003', 1, 403349.60, 354469.78),
    (14, 2, '4.1.1', 2, 89163.19, 68732.18),
    (17, 1, '5.4.5.01.01.0010', 3, 225284.81, 316583.57),
    (11, 1, '2.3.2.03.01.0005', 1, 226643.57, 189186.88),
    (25, 2, '4.1.1.01.03', 2, 519752.19, 552022.98),
    (21, 1, '2.1.1.08.01', 3, 192531.75, 250186.47),
    (15, 1, '4.2.1.03.01.0018', 1, -98341.92, -84667.60),
    (18, 2, '2.3', 2, 36494.01, 4115.96),
    (13, 2, '4.2.1.03.01.0011', 3, 584236.66, 569725.07),
    (12, 1, '2.2.1.04.02', 1, 334856.07, 390698.50),
    (33, 1, '3.1.1.03.06.0001', 2, 637112.48, 652290.14),
    (17, 1, '5.4.3.01.01.0004', 3, 466873.71, 534453.14)
),
novos AS (
  SELECT c.*, row_number() OVER () AS seq
  FROM combos c
  WHERE NOT EXISTS (
    SELECT 1 FROM public.bi_fechamento_validacao f
    WHERE f.cod_coligada = c.cod_coligada AND f.cod_filial = c.cod_filial
      AND f.cod_conta = c.cod_conta AND f.ano_mes = '2026-08'
  )
),
-- Rodízio por nome entre quem estiver cadastrado em bi_users (realizado_por tem FK).
-- Quem ainda não tiver cadastro fica fora do rodízio até ser cadastrado.
preparadores AS (
  SELECT id, row_number() OVER (ORDER BY name) - 1 AS pos, count(*) OVER () AS total
  FROM public.bi_users
  WHERE name ILIKE ANY (ARRAY['Karime%', 'Juliana%', 'Franciele%', 'Adeline%'])
),
conferentes AS (
  SELECT id, row_number() OVER (ORDER BY name) - 1 AS pos, count(*) OVER () AS total
  FROM public.bi_users
  WHERE name ILIKE ANY (ARRAY['Kely%', 'Kelly%', 'M_nica%'])
),
fechador AS (
  SELECT id FROM public.bi_users WHERE email = 'luiz@swisspark.com.br'
),
pendencias (pos, texto) AS (
  VALUES (0, 'Divergência entre saldo do razão e composição — aguardando extrato bancário.'),
         (1, 'Adiantamento a fornecedor sem NF vinculada no período.'),
         (2, 'Lançamento de ajuste sem histórico detalhado — confirmar com o contábil.'),
         (3, 'Título baixado no financeiro sem contrapartida contábil.'),
         (4, 'Conciliação ok, falta anexar relatório de composição.')
),
-- Mesma prioridade (1 a 4), pendência e responsável em todas as etapas da
-- combinação: é assim que o sistema faz, cada etapa clona a linha anterior.
detalhes AS (
  SELECT n.*,
         (n.seq % 4) + 1 AS prioridade,
         pe.texto AS pendencia_texto,
         co.id AS pendencia_responsavel_id,
         pr.id AS preparador_id,
         co.id AS conferente_id,
         now() - interval '3 days' + (n.seq * interval '7 minutes') AS inicio
  FROM novos n
  JOIN preparadores pr ON pr.pos = n.seq % pr.total
  JOIN conferentes co ON co.pos = n.seq % co.total
  JOIN pendencias pe ON pe.pos = n.seq % 5
),
passos AS (
  SELECT d.*, 1 AS passo, 'Preparado' AS status, d.preparador_id AS usuario
  FROM detalhes d
  UNION ALL
  SELECT d.*, 2, 'Conferido', d.conferente_id
  FROM detalhes d
  WHERE d.etapa >= 2
  UNION ALL
  SELECT d.*, 3, 'Fechado', fe.id
  FROM detalhes d CROSS JOIN fechador fe
  WHERE d.etapa = 3
)
INSERT INTO public.bi_fechamento_validacao
  (cod_coligada, cod_filial, cod_conta, ano_mes, status, prioridade,
   saldo_anterior, saldo_atual, data_fechamento,
   pendencia_texto, pendencia_responsavel_id, pendencia_registrada_em,
   realizado_por, realizado_em)
SELECT p.cod_coligada, p.cod_filial, p.cod_conta, '2026-08', p.status, p.prioridade,
       p.saldo_anterior, p.saldo_atual, DATE '2026-08-01',
       p.pendencia_texto, p.pendencia_responsavel_id, p.inicio + interval '1 day',
       p.usuario, p.inicio + (p.passo * interval '1 day')
FROM passos p
ORDER BY p.seq, p.passo;

-- Conferência rápida do status atual por combinação:
SELECT status, count(*) FROM (
  SELECT DISTINCT ON (cod_coligada, cod_filial, cod_conta) status
  FROM public.bi_fechamento_validacao WHERE ano_mes = '2026-08'
  ORDER BY cod_coligada, cod_filial, cod_conta, realizado_em DESC
) x GROUP BY status;

COMMIT;

-- DESFAZER (apaga só as combinações deste script em 2026-08):
-- DELETE FROM public.bi_fechamento_validacao
-- WHERE ano_mes = '2026-08' AND data_fechamento = DATE '2026-08-01'
--   AND (cod_coligada, cod_filial, cod_conta) IN (
--     (8, 1, '7.1.1'),
--     (18, 1, '3.1.2.01.01.0004'),
--     (18, 1, '5.2.1.01.01.0022'),
--     (22, 2, '4.1.1'),
--     (24, 2, '1.2.2.02.01.0002'),
--     (28, 2, '5.2.1.01.01.0030'),
--     (12, 1, '2.1.1.02.01'),
--     (10, 2, '1.2.1.04'),
--     (33, 1, '2.3.1.02.02.0001'),
--     (17, 1, '1.1'),
--     (4, 1, '2.3.1.01.01.0002'),
--     (8, 1, '1.2.2.03.02'),
--     (11, 1, '2.1.1.06.01.0001'),
--     (18, 2, '3.1.2.01'),
--     (31, 1, '5.1'),
--     (14, 1, '2.3.2.02.01.0005'),
--     (12, 1, '1.1.2.01.01.0003'),
--     (22, 2, '6.2.1.01.01.0003'),
--     (12, 2, '6.2.1.01.01.0002'),
--     (16, 1, '5.1.1.01'),
--     (28, 2, '3.1.2.01.02.0001'),
--     (13, 2, '3.1.1.01'),
--     (8, 2, '1.2.3.05.01.0001'),
--     (21, 1, '1.1.2.02.01.0003'),
--     (10, 2, '2.2.1.01.01.0004'),
--     (22, 2, '7.1.1.01'),
--     (13, 2, '4.2.1.01.01.0016'),
--     (9, 2, '1.1.2.01.03.0001'),
--     (5, 1, '7.3.2.01.01.0002'),
--     (18, 2, '5.2.1.01.01.0005'),
--     (18, 2, '7.1.2.01'),
--     (24, 2, '2.1.1.04.01'),
--     (5, 1, '4.1.1.01.02.0001'),
--     (29, 2, '1.1.1.02.01.0001'),
--     (5, 1, '2.1.1.04.01.0002'),
--     (30, 1, '2.3.1.01.02.0004'),
--     (25, 1, '1.1.2.07'),
--     (4, 1, '3.1.1.01.01.0001'),
--     (22, 1, '1.2.1.02.02'),
--     (17, 1, '2.2.1.03.01.0002'),
--     (20, 1, '5.2.1.01.01.0032'),
--     (12, 1, '1.2.3.01.06'),
--     (22, 2, '3.1.1.09'),
--     (26, 2, '3.1.2.01.01'),
--     (4, 1, '5.1.1.01.01.0004'),
--     (24, 1, '1.2.1.04.10.0001'),
--     (23, 1, '5.1.1.01.01'),
--     (14, 1, '5.3'),
--     (15, 1, '1.2.1.07.01.0003'),
--     (14, 2, '4.1.1'),
--     (17, 1, '5.4.5.01.01.0010'),
--     (11, 1, '2.3.2.03.01.0005'),
--     (25, 2, '4.1.1.01.03'),
--     (21, 1, '2.1.1.08.01'),
--     (15, 1, '4.2.1.03.01.0018'),
--     (18, 2, '2.3'),
--     (13, 2, '4.2.1.03.01.0011'),
--     (12, 1, '2.2.1.04.02'),
--     (33, 1, '3.1.1.03.06.0001'),
--     (17, 1, '5.4.3.01.01.0004')
--   );
