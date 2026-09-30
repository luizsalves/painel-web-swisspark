-- public."VW_FT_COMPOSICAO" source

CREATE OR REPLACE VIEW public."VW_FT_COMPOSICAO"
AS SELECT a."POSICAO",
    COALESCE(a."ANO_MES", '-'::text) AS "ANO_MES",
    a."DATA",
    a."DATACONTABILIZBX",
    COALESCE(a."EMPRESA", '-'::character varying) AS "EMPRESA",
    COALESCE(a."REDUZIDO", '-'::character varying) AS "REDUZIDO",
    COALESCE(a."CODCONTA", '-'::character varying) AS "CODCONTA",
    COALESCE(a."IDPARTIDA", '-'::character varying) AS "IDPARTIDA",
    a."ANTERIOR",
    a."DEBITOS",
    a."CREDITOS",
        CASE
            WHEN a."SALDO_ATUAL" IS NULL THEN 0::numeric
            ELSE a."SALDO_ATUAL"
        END AS "SALDO_ATUAL",
    COALESCE(a."DESCRICAO_TRATADA", '-'::text) AS "DESCRICAO_TRATADA",
    a."CODFILIAL",
        CASE
            WHEN a."HISTORICO" IS NULL AND a."IDPARTIDA"::text ~~ '202%'::text THEN 'Fechamento'::character varying
            ELSE COALESCE(a."HISTORICO", '-'::character varying)
        END AS "HISTORICO",
    COALESCE(a."CODCOLIGADA"::character varying(10), '-'::character varying) AS "CODCOLIGADA",
    COALESCE(b.preparado, '-'::character varying) AS "PREPARADO",
    COALESCE(b.data_preparado, '-'::character varying) AS "DATA_PREPARADO",
    COALESCE(b.conferido, '-'::character varying) AS "CONFERIDO",
    COALESCE(b.data_conferido, '-'::character varying) AS "DATA_CONFERIDO",
    COALESCE(b.pendencia, '-'::character varying) AS "PENDENCIA",
    COALESCE(b.resp_pendencia, '-'::character varying) AS "RESP_PENDENCIA",
        CASE
            WHEN btrim(c.prioridade::text) = '1'::text THEN '1-Baixa'::text
            WHEN btrim(c.prioridade::text) = '2'::text THEN '2-Regular'::text
            WHEN btrim(c.prioridade::text) = '3'::text THEN '3-Média'::text
            WHEN btrim(c.prioridade::text) = '4'::text THEN '4-Alta'::text
            ELSE 'Não definido'::text
        END AS prioridade,
        CASE
            WHEN btrim(b.preparado::text) IS NULL OR btrim(b.preparado::text) = ''::text THEN 0
            ELSE 1
        END AS "QTD_PREPARADO",
        CASE
            WHEN btrim(b.conferido::text) IS NULL OR btrim(b.conferido::text) = ''::text THEN 0
            ELSE 1
        END AS "QTD_CONFERIDO",
        CASE
            WHEN btrim(b.pendencia::text) IS NULL OR btrim(b.pendencia::text) = ''::text THEN 0
            ELSE 1
        END AS "QTD_PENDENCIA",
        CASE
            WHEN (btrim(b.preparado::text) IS NOT NULL OR btrim(b.preparado::text) <> ''::text) AND (btrim(b.conferido::text) IS NOT NULL OR btrim(b.conferido::text) <> ''::text) AND (btrim(b.pendencia::text) IS NULL OR btrim(b.pendencia::text) = ''::text) THEN 1
            ELSE 0
        END AS "QTD_FINALIZADO",
        CASE
            WHEN btrim(b.preparado::text) IS NULL OR btrim(b.preparado::text) = ''::text THEN 'NÃO'::text
            ELSE 'SIM'::text
        END AS "STATUS_PREPARADO",
        CASE
            WHEN btrim(b.conferido::text) IS NULL OR btrim(b.conferido::text) = ''::text THEN 'NÃO'::text
            ELSE 'SIM'::text
        END AS "STATUS_CONFERIDO",
        CASE
            WHEN btrim(b.pendencia::text) IS NULL OR btrim(b.pendencia::text) = ''::text THEN 'NÃO'::text
            ELSE 'SIM'::text
        END AS "STATUS_PENDENCIA",
        CASE
            WHEN (btrim(b.preparado::text) IS NOT NULL OR btrim(b.preparado::text) <> ''::text) AND (btrim(b.conferido::text) IS NOT NULL OR btrim(b.conferido::text) <> ''::text) AND (btrim(b.pendencia::text) IS NULL OR btrim(b.pendencia::text) = ''::text) THEN 'SIM'::text
            ELSE 'NÃO'::text
        END AS "STATUS_FINALIZADO",
    COALESCE(a."NOME_FORNECEDOR", '-'::character varying) AS "NOME_FORNECEDOR",
    COALESCE(a."NUMCONTABILBX", '-'::character varying) AS "NUMCONTABILBX",
    COALESCE(a."SEGUNDONUMERO", '-'::character varying) AS "SEGUNDONUMERO",
    a."VALOR_NF_EM_ABERTO",
    a."DATAEXECUCAO",
    a."IDBANCODADOS"
   FROM "FT_COMPOSICAO" a
     LEFT JOIN "FVALIDACAO" b ON b.chave_validacao::text = concat(a."CODFILIAL", a."EMPRESA", a."CODCONTA", a."ANO_MES")
     LEFT JOIN "DPRIORIDADE" c ON c.chave::text = concat(a."EMPRESA", a."ANO_MES")
  ORDER BY a."DATA", a."EMPRESA", a."POSICAO";
  
 
 
 
 
 
 select * 
 from "FT_COMPOSICAO" a
 where "CODCONTA" = '1.1.2.04.01.0003'
  and "ANO_MES"  = '2026-08'
  and "CODCOLIGADA" = '26'
  and "IDPARTIDA" = '99003'
  