CREATE OR REPLACE FUNCTION public.fc_fech_conciliacao()
 RETURNS void
 LANGUAGE plpgsql
AS $function$


DECLARE
    mes_array TEXT[] := ARRAY[]::TEXT[];  -- Variável para armazenar os meses
    mes_atual TEXT;
BEGIN
	
	    -- Removendo índices, se existirem
    DROP INDEX IF EXISTS idx_id_banco1;
    DROP INDEX IF EXISTS idx_id_colig1;
    DROP INDEX IF EXISTS idx_id_codconta1;
    DROP INDEX IF EXISTS idx_id_banco2;
    DROP INDEX IF EXISTS idx_id_colig2;
    DROP INDEX IF EXISTS idx_id_codconta2;
    DROP INDEX IF EXISTS idx_id_EMPRESA;
    DROP INDEX IF EXISTS idx_id_EMPRESA2;
    DROP INDEX IF EXISTS idx_id_EMPRESA3;
    DROP INDEX IF EXISTS idx_id_EMPRESA4;
    DROP INDEX IF EXISTS idx_id_EMPRESA5;
    DROP INDEX IF EXISTS idx_id_CODCONTA4;
    DROP INDEX IF EXISTS idx_id_CODCONTA5;
    DROP INDEX IF EXISTS idx_id_EMPRESA8;
    DROP INDEX IF EXISTS idx_id_CONTA8;
    DROP INDEX IF EXISTS idx_id_LCTO;
    DROP INDEX IF EXISTS idx_id_VALOR_CREDITO;
    DROP INDEX IF EXISTS idx_id_VALOR_DEBITO;
    DROP INDEX IF EXISTS IDX_DATA_FECH;
    DROP INDEX IF exists idx_id_RAZAO_EMPRESA;
    DROP INDEX IF exists idx_id_RAZAO_CONTA;
    DROP INDEX IF exists idx_ft_razao_ano_mes;
   	DROP INDEX IF exists idx_ft_razao_documento;
   
   
   
    -- Remover tabelas existentes
    DROP TABLE IF EXISTS public."FT_CREDITO";
    DROP TABLE IF EXISTS public."FT_DEBITO";
    DROP TABLE IF EXISTS public."FT_DEBITO_CREDITO";
    DROP TABLE IF EXISTS public."CCONTA_GFILIAL";
    DROP TABLE IF EXISTS public."FT_RAZAO";
   
   
 DROP TABLE IF EXISTS  public."FECHAMENTO_00";
 CREATE TABLE public."FECHAMENTO_00" AS
 SELECT 
    'FECHAMENTO' AS "TIPO_ITEM",
    TO_CHAR(CAST(A.data AS DATE), 'YYYY-MM') AS "ANO_MES", 
    date_trunc('month', CAST(A.data AS DATE)) + INTERVAL '1 month' - INTERVAL '1 second' AS "DATA",
    A.empresa AS "EMPRESA", 
    A.codconta AS "CODCONTA",
    A.codfilial AS "CODFILIAL",
    A.reduzido AS "REDUZIDO", 
    A.descricao AS "DESCRICAO", 
    A.anterior AS "ANTERIOR", 
    A.debitos AS "DEBITOS", 
    A.creditos AS "CREDITOS", 
    A.saldo_atual AS "SALDO_ATUAL",
    A.idbancodados  as "IDBANCODADOS",
    A.codcoligada  as "CODCOLIGADA"
FROM public."FT_FECH_CONTAS_2023" A;


    -- Esse ajuste foi necessario pois o campo vinha com espaço no final
   UPDATE public."FECHAMENTO_00"
    SET "EMPRESA"  = TRIM(UPPER("EMPRESA"))
    WHERE "EMPRESA" like 'SWISS PARK NEGÓCIOS IMOBILIÁRIOS LTDA S%';
   
   -- Ajusta 'AEPL AGROPECUARIA LTDA I'   
  --  UPDATE public."FECHAMENTO_00"
  --  SET "EMPRESA"  = 'AEPL AGROPECUARIA LTDA I'
  --  WHERE "EMPRESA" = 'AEPL AGROPECUARIA LTDA';

    -- Criar FT_CREDITO
    CREATE TABLE public."FT_CREDITO" AS
    SELECT
        TO_CHAR(A."DATA", 'YYYY-MM') AS "ANO_MES",
        CAST('DESCONHECIDO' AS VARCHAR(100)) AS "EMPRESA",
        CAST(0 AS INT) AS "FILIAL_TOTVS_OSTENSIVA",
        CAST(0 AS INT) AS "FILIAL_TOTVS_SCP",
        CAST(0 AS INT) AS "FILIAL_TOTVS_SCP2",
        A."CREDITO" AS "CODCONTA",
        CAST(0.0000 AS numeric(15, 4)) AS "VALOR_DEBITO",
        --CAST(A."VALOR" AS numeric(15, 4)) AS "VALOR_CREDITO",
        CASE WHEN SUBSTRING(A."CREDITO",1,1) = '2' THEN CAST(A."VALOR" AS numeric(15, 4)) 
			ELSE CAST(A."VALOR" AS numeric(15, 4)) * -1 
		END AS "VALOR_CREDITO",
        A.*,
        CAST('DESCONHECIDO' AS VARCHAR(20)) AS "REDUZIDO",
        CAST('DESCONHECIDO' AS VARCHAR(250)) AS "DESCRICAO"
    FROM public."CLANCA" A
    WHERE A."CREDITO" IS NOT NULL;


    -- Criar FT_DEBITO
    CREATE TABLE public."FT_DEBITO" AS
    SELECT
        TO_CHAR(A."DATA", 'YYYY-MM') AS "ANO_MES",
        CAST('DESCONHECIDO' AS VARCHAR(100)) AS "EMPRESA",
        CAST(0 AS INT) AS "FILIAL_TOTVS_OSTENSIVA",
        CAST(0 AS INT) AS "FILIAL_TOTVS_SCP",
        CAST(0 AS INT) AS "FILIAL_TOTVS_SCP2",
        A."DEBITO" AS "CODCONTA",
        --CAST(A."VALOR" AS numeric(15, 4)) AS "VALOR_DEBITO",
        CASE WHEN SUBSTRING(A."DEBITO",1,1) = '2' THEN CAST(A."VALOR" AS numeric(15, 4)) * -1
			ELSE CAST(A."VALOR" AS numeric(15, 4)) 
		END AS "VALOR_DEBITO",
        CAST(0.0 AS numeric(15, 4)) AS "VALOR_CREDITO",
        A.*,
        CAST('DESCONHECIDO' AS VARCHAR(20)) AS "REDUZIDO",
        CAST('DESCONHECIDO' AS VARCHAR(250)) AS "DESCRICAO"
    FROM public."CLANCA" A
    WHERE A."DEBITO" IS NOT NULL;

    -- Criar índices
    CREATE INDEX idx_id_banco1 ON public."FT_CREDITO" ("IDBANCODADOS");
    CREATE INDEX idx_id_colig1 ON public."FT_CREDITO" ("CODCOLIGADA");
    CREATE INDEX idx_id_codconta1 ON public."FT_CREDITO" ("CODCONTA");
    CREATE INDEX idx_id_banco2 ON public."FT_DEBITO" ("IDBANCODADOS");
    CREATE INDEX idx_id_colig2 ON public."FT_DEBITO" ("CODCOLIGADA");
    CREATE INDEX idx_id_codconta2 ON public."FT_DEBITO" ("CODCONTA");
    CREATE INDEX idx_id_EMPRESA ON public."FT_DEBITO" ("EMPRESA");
    CREATE INDEX idx_id_EMPRESA2 ON public."FT_CREDITO" ("EMPRESA");
    
   /*
   -- AJUSTA AEPL AGROPECUARIA
   -- ELA POSSUI 3 FILIAIS E CADA UMA DEVE TER UM NOME DIFERENTE
   UPDATE public."GFILIAL"
    SET "NOME" = 'AEPL AGROPECUARIA LTDA I'
    FROM public."GFILIAL" AS subquery
    WHERE public."GFILIAL"."IDBANCODADOS" = 2
        AND public."GFILIAL"."CODCOLIGADA" = 14
        AND public."GFILIAL"."CODFILIAL" = 1
        AND public."GFILIAL"."NOME" = 'AEPL AGROPECUARIA LTDA';
       
   UPDATE public."GFILIAL"
    SET "NOME" = 'AEPL AGROPECUARIA LTDA II'
    FROM public."GFILIAL" AS subquery
    WHERE public."GFILIAL"."IDBANCODADOS" = 2
        AND public."GFILIAL"."CODCOLIGADA" = 14
        AND public."GFILIAL"."CODFILIAL" = 2
        AND public."GFILIAL"."NOME" = 'AEPL AGROPECUARIA LTDA';
       
  UPDATE public."GFILIAL"
    SET "NOME" = 'AEPL AGROPECUARIA LTDA III'
    FROM public."GFILIAL" AS subquery
    WHERE public."GFILIAL"."IDBANCODADOS" = 2
        AND public."GFILIAL"."CODCOLIGADA" = 14
        AND public."GFILIAL"."CODFILIAL" = 3
        AND public."GFILIAL"."NOME" = 'AEPL AGROPECUARIA LTDA';
       
      */
       

    -- Atualizar EMPRESA em FT_DEBITO
    UPDATE public."FT_DEBITO"
    SET "EMPRESA" = TRIM(UPPER(subquery."NOME"))
    FROM public."GFILIAL" AS subquery
    WHERE public."FT_DEBITO"."IDBANCODADOS" = subquery."IDBANCODADOS"
        AND public."FT_DEBITO"."CODCOLIGADA" = subquery."CODCOLIGADA"
        AND public."FT_DEBITO"."CODFILIAL" = subquery."CODFILIAL";

    -- Atualizar EMPRESA em FT_CREDITO
    UPDATE public."FT_CREDITO"
    SET "EMPRESA" = TRIM(UPPER(subquery."NOME"))
    FROM public."GFILIAL" AS subquery
    WHERE public."FT_CREDITO"."IDBANCODADOS" = subquery."IDBANCODADOS"
        AND public."FT_CREDITO"."CODCOLIGADA" = subquery."CODCOLIGADA"
        AND public."FT_CREDITO"."CODFILIAL" = subquery."CODFILIAL";
       
    -- Criar tabela FT_DEBITO_CREDITO
    CREATE TABLE public."FT_DEBITO_CREDITO" AS
    SELECT
        *,
        replace(replace(replace(replace(replace(replace(replace(replace(replace("DESCRICAO", '\\', '.'), ':', '.'), '*', '.'), '>', '.'), '<', '.'), '|', '.'), '?', '.'), '/', '.'), '.', '.') AS "DESCRICAO_TRATADA"
    FROM public."FT_DEBITO"
    UNION ALL
    SELECT
        *,
        replace(replace(replace(replace(replace(replace(replace(replace(replace("DESCRICAO", '\\', '.'), ':', '.'), '*', '.'), '>', '.'), '<', '.'), '|', '.'), '?', '.'), '/', '.'), '.', '.') AS "DESCRICAO_TRATADA"
    FROM public."FT_CREDITO";

   
    DROP TABLE IF EXISTS public."FT_CREDITO";
    DROP TABLE IF EXISTS public."FT_DEBITO";
   
   
  
    -- Criar tabela CCONTA_GFILIAL
    CREATE TABLE public."CCONTA_GFILIAL" AS
    SELECT DISTINCT
			        'NAO' AS "ENCONTRADO_GFILIAL",
			        TRIM(UPPER(A.empresa)) as "EMPRESA",
			        A.idbancodados  as "IDBANCODADOS",
			        A.codcoligada   as "CODCOLIGADA",
			        A.codfilial   as "CODFILIAL",
			        A.codconta as "CODCONTA",
			        A.reduzido as "REDUZIDO",
			        A.descricao as "DESCRICAO",
			        replace(replace(replace(replace(replace(replace(replace(replace(replace(A.descricao, '\\', '.'), ':', '.'), '*', '.'), '>', '.'), '<', '.'), '|', '.'), '?', '.'), '/', '.'), '.', '.') AS "DESCRICAO_TRATADA"
    FROM public."FT_FECH_CONTAS_2023" A;

   
   
    -- Atualizar ENCONTRADO_GFILIAL
    UPDATE public."CCONTA_GFILIAL"
    SET "ENCONTRADO_GFILIAL" = 'SIM'
    FROM public."GFILIAL" AS subquery
    WHERE TRIM(UPPER(public."CCONTA_GFILIAL"."EMPRESA")) = TRIM(UPPER(subquery."NOME"));

    -- Criar índices adicionais
    CREATE INDEX idx_id_EMPRESA4 ON public."CCONTA_GFILIAL" ("EMPRESA");
    CREATE INDEX idx_id_EMPRESA5 ON public."FT_DEBITO_CREDITO" ("EMPRESA");
    CREATE INDEX idx_id_CODCONTA4 ON public."CCONTA_GFILIAL" ("CODCONTA");
    CREATE INDEX idx_id_CODCONTA5 ON public."FT_DEBITO_CREDITO" ("CODCONTA");
   
   
   	-- CARIMBA REDUZIDOS VAZIOS PROCURAR NA CCONTA
    UPDATE public."FT_DEBITO_CREDITO"
    SET "REDUZIDO" = subquery."REDUZIDO",
    	"DESCRICAO" = subquery."DESCRICAO",
    	"DESCRICAO_TRATADA" = replace(replace(replace(replace(replace(replace(replace(replace(replace(subquery."DESCRICAO", '\\', '.'), ':', '.'), '*', '.'), '>', '.'), '<', '.'), '|', '.'), '?', '.'), '/', '.'), '.', '.') 
    FROM (SELECT *
          FROM public."CCONTA" 
         ) AS subquery
    WHERE public."FT_DEBITO_CREDITO"."IDBANCODADOS" = subquery."IDBANCODADOS"
           AND public."FT_DEBITO_CREDITO"."CODCOLIGADA" = subquery."CODCOLIGADA"
          AND public."FT_DEBITO_CREDITO"."CODCONTA" = subquery."CODCONTA"; 	
         
         
    -- INCLUI REDUZIDO EM QUEM FALTOU PELO .PDF ENVIADO
    UPDATE public."FT_DEBITO_CREDITO"
    SET "REDUZIDO" = subquery.reduzido,
    	"DESCRICAO" = subquery.descricao,
    	"DESCRICAO_TRATADA" = replace(replace(replace(replace(replace(replace(replace(replace(replace(subquery.descricao, '\\', '.'), ':', '.'), '*', '.'), '>', '.'), '<', '.'), '|', '.'), '?', '.'), '/', '.'), '.', '.') 
    FROM (SELECT *
          FROM public."FT_FECH_CONTAS_2023"
         ) AS subquery
    WHERE public."FT_DEBITO_CREDITO"."REDUZIDO" = 'DESCONHECIDO'
          AND public."FT_DEBITO_CREDITO"."IDBANCODADOS" = subquery.idbancodados
          AND public."FT_DEBITO_CREDITO"."CODCOLIGADA" = subquery.codcoligada
          AND public."FT_DEBITO_CREDITO"."CODFILIAL" = subquery.codfilial
          AND public."FT_DEBITO_CREDITO"."CODCONTA" = subquery.codconta;


       
     -- Atualizar FT_DEBITO_CREDITO
    UPDATE public."FT_DEBITO_CREDITO"
    SET "REDUZIDO" = subquery."REDUZIDO",
        "DESCRICAO" = subquery."DESCRICAO",
        "DESCRICAO_TRATADA" = subquery."DESCRICAO_TRATADA"
    FROM public."CCONTA_GFILIAL" AS subquery
    WHERE public."FT_DEBITO_CREDITO"."REDUZIDO" = 'DESCONHECIDO'
    	AND  public."FT_DEBITO_CREDITO"."CODCONTA" = subquery."CODCONTA"
    	AND public."FT_DEBITO_CREDITO"."IDBANCODADOS" = subquery."IDBANCODADOS"
        AND public."FT_DEBITO_CREDITO"."CODCOLIGADA" = subquery."CODCOLIGADA"
        AND public."FT_DEBITO_CREDITO"."CODFILIAL" = subquery."CODFILIAL";
       -- AND TRIM(UPPER(public."FT_DEBITO_CREDITO"."EMPRESA")) = TRIM(UPPER(subquery."EMPRESA"));
	
	  
       
    -- INCLUI REDUZIDO EM QUEM FALTOU PELO .PDF ENVIADO
    UPDATE public."FT_DEBITO_CREDITO"
    SET "REDUZIDO" = subquery.reduzido,
    	"DESCRICAO" = subquery.descricao,
    	"DESCRICAO_TRATADA" = replace(replace(replace(replace(replace(replace(replace(replace(replace(subquery.descricao, '\\', '.'), ':', '.'), '*', '.'), '>', '.'), '<', '.'), '|', '.'), '?', '.'), '/', '.'), '.', '.') 
    FROM (SELECT *
          FROM public."_REDUZIDO"
         ) AS subquery
    WHERE public."FT_DEBITO_CREDITO"."REDUZIDO" = 'DESCONHECIDO'
          AND public."FT_DEBITO_CREDITO"."IDBANCODADOS" = subquery.idbancodados
           AND public."FT_DEBITO_CREDITO"."CODCOLIGADA" = subquery.coligada
          AND public."FT_DEBITO_CREDITO"."CODCONTA" = subquery.conta;
         
         
-- Tabela com os arquivos PDF de fechamento de Dezembro/2024
DROP TABLE IF EXISTS public."DIMSALDO_INICIAL";
CREATE TABLE public."DIMSALDO_INICIAL" AS
SELECT DISTINCT   *  , CONCAT("EMPRESA", '_', "CODCONTA") as "CHAVE_2"
FROM public."FECHAMENTO_00";

DROP INDEX IF EXISTS idx_id_DIMLANCAMENTOS_EMPRESA;
DROP INDEX IF EXISTS idx_idDIMLANCAMENTOS_CONTA;




-- Essa tabela é criada para Iniciar as contas com valores zerados '0', caso elas não estejam no PDF
DROP TABLE IF EXISTS public."DIMLANCAMENTOS";
CREATE TABLE public."DIMLANCAMENTOS" AS
SELECT DISTINCT
    "EMPRESA",
    "CODCONTA",
    "IDBANCODADOS" ,
    "CODCOLIGADA" ,
    "CODFILIAL" ,
    SUM(CAST(0 AS numeric(15,4))) AS "ANTERIOR",
    SUM(CAST(0 AS numeric(15,4))) AS "DEBITOS",
    SUM(CAST(0 AS numeric(15,4))) AS "CREDITOS",
    SUM(CAST(0 AS numeric(15,4))) AS "SALDO_ATUAL"
FROM public."FT_DEBITO_CREDITO"
GROUP BY "EMPRESA","CODCONTA","IDBANCODADOS" ,"CODCOLIGADA" ,"CODFILIAL";

CREATE INDEX idx_id_DIMLANCAMENTOS_EMPRESA ON public."DIMLANCAMENTOS" ("EMPRESA");
CREATE INDEX idx_idDIMLANCAMENTOS_CONTA ON public."DIMLANCAMENTOS" ("CODCONTA");

DELETE FROM public."DIMLANCAMENTOS"
WHERE ctid NOT IN (
    SELECT MIN(ctid)
    FROM public."DIMLANCAMENTOS"
    GROUP BY "EMPRESA", "IDBANCODADOS","CODCOLIGADA", "CODFILIAL" , "CODCONTA"
);


-------------- AQUI 

INSERT INTO public."DIMSALDO_INICIAL" ("TIPO_ITEM","ANO_MES", "DATA", "EMPRESA","CODCONTA","CODFILIAL", "REDUZIDO", "DESCRICAO", "ANTERIOR", "DEBITOS", "CREDITOS", "SALDO_ATUAL", "IDBANCODADOS", "CODCOLIGADA","CHAVE_2")
SELECT DISTINCT 
	'FECHAMENTO' as "TIPO_ITEM",
    CAST('2024-01' AS text) AS "ANO_MES",
    CAST('2024-01-31 23:59:59.000 - 0300' AS DATE) AS "DATA",
    "EMPRESA",
    "CODCONTA",
    "CODFILIAL",
    CAST('DESCONHECIDO' as VARCHAR(255)) as "REDUZIDO",
    CAST('DESCONHECIDO' as VARCHAR(255)) as "DESCRICAO",
    "ANTERIOR",
    "DEBITOS",
    "CREDITOS",
    "SALDO_ATUAL",
    "IDBANCODADOS",
    "CODCOLIGADA",
    CAST(CONCAT("EMPRESA", cast("IDBANCODADOS" as varchar(10)), cast("CODCOLIGADA" as varchar(10)),cast("CODFILIAL" as varchar(10)), '_', "CODCONTA") AS VARCHAR(160)) as "CHAVE_2"
FROM public."DIMLANCAMENTOS" D
WHERE CONCAT(D."EMPRESA",  '_', cast(D."CODCONTA" as varchar(10)))
	NOT IN (select  CONCAT(S."EMPRESA",  '_', cast(S."CODCONTA" as varchar(10))) FROM public."DIMSALDO_INICIAL" S);
--CAST(CONCAT("EMPRESA", cast("IDBANCODADOS" as varchar(10)), cast("CODCOLIGADA" as varchar(10)),cast("CODFILIAL" as varchar(10)), '_', "CODCONTA") AS VARCHAR(160))
    

   
DELETE FROM public."DIMSALDO_INICIAL"
WHERE ctid NOT IN (
    SELECT MIN(ctid)
    FROM public."DIMSALDO_INICIAL"
    GROUP BY "CHAVE_2"
);


-- Carimba reduzidos vazios, procurar na CCONTA
UPDATE public."DIMSALDO_INICIAL"
SET "REDUZIDO" = subquery."REDUZIDO",
    "DESCRICAO" = subquery."DESCRICAO"
FROM (
    SELECT * FROM public."CCONTA"
) AS subquery
WHERE public."DIMSALDO_INICIAL"."IDBANCODADOS" = subquery."IDBANCODADOS"
    AND public."DIMSALDO_INICIAL"."CODCOLIGADA" = subquery."CODCOLIGADA"
    AND public."DIMSALDO_INICIAL"."CODCONTA" = subquery."CODCONTA";

-- Inclui reduzido em quem faltou pelo PDF enviado
UPDATE public."DIMSALDO_INICIAL"
SET "REDUZIDO" = subquery."reduzido",
    "DESCRICAO" = subquery."descricao"
FROM (
    SELECT * FROM public."FT_FECH_CONTAS_2023"
) AS subquery
WHERE public."DIMSALDO_INICIAL"."REDUZIDO" = 'DESCONHECIDO'
    AND public."DIMSALDO_INICIAL"."IDBANCODADOS" = subquery."idbancodados"
    AND public."DIMSALDO_INICIAL"."CODCOLIGADA" = subquery."codcoligada"
    AND public."DIMSALDO_INICIAL"."CODFILIAL" = subquery."codfilial"
    AND public."DIMSALDO_INICIAL"."CODCONTA" = subquery."codconta";

-- Atualizar FT_DEBITO_CREDITO com dados do CCONTA_GFILIAL
UPDATE public."DIMSALDO_INICIAL"
SET "REDUZIDO" = subquery."REDUZIDO",
    "DESCRICAO" = subquery."DESCRICAO"
FROM public."CCONTA_GFILIAL" AS subquery
WHERE public."DIMSALDO_INICIAL"."REDUZIDO" = 'DESCONHECIDO'
    AND public."DIMSALDO_INICIAL"."CODCONTA" = subquery."CODCONTA"
    AND TRIM(UPPER(public."DIMSALDO_INICIAL"."EMPRESA")) = TRIM(UPPER(subquery."EMPRESA"));

-- Inclui reduzido em quem faltou pelo PDF enviado (dados do _REDUZIDO)
UPDATE public."DIMSALDO_INICIAL"
SET "REDUZIDO" = subquery."reduzido",
    "DESCRICAO" = subquery."descricao"
FROM (
    SELECT * FROM public."_REDUZIDO"
) AS subquery
WHERE public."DIMSALDO_INICIAL"."REDUZIDO" = 'DESCONHECIDO'
    AND public."DIMSALDO_INICIAL"."IDBANCODADOS" = subquery."idbancodados"
    AND public."DIMSALDO_INICIAL"."CODCOLIGADA" = subquery."coligada"
    AND public."DIMSALDO_INICIAL"."CODCONTA" = subquery."conta";

   
-- INICIO  
    DROP TABLE IF EXISTS public."FECHAMENTO_01";
    CREATE TABLE public."FECHAMENTO_01" AS
    SELECT CONCAT(CAST("IDBANCODADOS" AS VARCHAR(10)), CAST("CODCOLIGADA" AS VARCHAR(10)),CAST("CODFILIAL" AS VARCHAR(10)),"CODCONTA") AS "CHAVE",
           *
    FROM public."DIMSALDO_INICIAL";
   
   
------------------------------------ LOOP -------------------------------------------------------------------------

    -- Obter os valores únicos de ANO_MES (exceto '2024-01')
    SELECT ARRAY_AGG("ANO_MES" ORDER BY "ANO_MES") 
    INTO mes_array
    FROM (
        SELECT X."ANO_MES"
        FROM (
            SELECT DISTINCT "ANO_MES", CAST(REPLACE("ANO_MES", '-', '') AS INTEGER) AS "ORDEM"
            FROM public."FT_DEBITO_CREDITO"
            WHERE "ANO_MES" <> '2024-01'
        ) X
        ORDER BY X."ORDEM" ASC
    ) AS subquery;

    -- Iterar sobre o array de meses
    FOREACH mes_atual IN ARRAY mes_array LOOP
        -- Inserir dados no fechamento
        INSERT INTO public."FECHAMENTO_01" (
            "CHAVE",
            "TIPO_ITEM",
            "ANO_MES",
            "DATA",
            "EMPRESA",
            "CODCONTA",
            "CODFILIAL",
            "REDUZIDO",
            "DESCRICAO",
            "ANTERIOR",
            "DEBITOS",
            "CREDITOS",
            "SALDO_ATUAL",
            "IDBANCODADOS",
            "CODCOLIGADA",
            "CHAVE_2"
        )
        SELECT 
            CONCAT(CAST("IDBANCODADOS" AS VARCHAR(10)), CAST("CODCOLIGADA" AS VARCHAR(10)),CAST("CODFILIAL" AS VARCHAR(10)),"CODCONTA") AS "CHAVE",
            'FECHAMENTO' AS "TIPO_ITEM",
            TO_CHAR(A."DATA", 'YYYY-MM') AS "ANO_MES",
            (DATE_TRUNC('month', A."DATA") + interval '1 month' - interval '1 day')::DATE AS "DATA",
            "EMPRESA",
            "CODCONTA",
            "CODFILIAL",
            "REDUZIDO",
            "DESCRICAO",
            CAST(0 AS NUMERIC(15, 2)) AS "ANTERIOR",
            SUM("VALOR_DEBITO") AS "DEBITOS",
            SUM("VALOR_CREDITO") AS "CREDITOS",
            CAST(0 AS NUMERIC(15, 2)) AS "SALDO_ATUAL",
            "IDBANCODADOS",
            "CODCOLIGADA",
            CONCAT("EMPRESA", '_', "CODCONTA") AS "CHAVE_2"
        FROM public."FT_DEBITO_CREDITO" A
        WHERE mes_atual = TO_CHAR(A."DATA", 'YYYY-MM')
        GROUP BY 
            TO_CHAR(A."DATA", 'YYYY-MM'),
            DATE_TRUNC('month', A."DATA") + interval '1 month' - interval '1 day',
            "EMPRESA", "CODCONTA", "CODFILIAL", "REDUZIDO", "DESCRICAO", "IDBANCODADOS", "CODCOLIGADA";

        -- Inserir fechamento sem lançamentos
        INSERT INTO public."FECHAMENTO_01" (
            "CHAVE",
            "TIPO_ITEM",
            "ANO_MES",
            "DATA",
            "EMPRESA",
            "CODCONTA",
            "CODFILIAL",
            "REDUZIDO",
            "DESCRICAO",
            "ANTERIOR",
            "DEBITOS",
            "CREDITOS",
            "SALDO_ATUAL",
            "IDBANCODADOS",
            "CODCOLIGADA",
            "CHAVE_2"
        )
        SELECT 
            "CHAVE",
            "TIPO_ITEM",
            TO_CHAR("DATA" + interval '1 month', 'YYYY-MM') AS "ANO_MES",
            ("DATA" + interval '1 month') AS "DATA",
            "EMPRESA", 
            "CODCONTA",
            "CODFILIAL",
            "REDUZIDO", 
            "DESCRICAO", 
            "ANTERIOR", 
            0,  
            0, 
            "SALDO_ATUAL",
            "IDBANCODADOS",
            "CODCOLIGADA",
            "CHAVE_2"
        FROM public."FECHAMENTO_01" A	
        WHERE "ANO_MES" = TO_CHAR(TO_DATE(mes_atual, 'YYYY-MM') - interval '1 month', 'YYYY-MM')
          AND A."CHAVE" NOT IN (
              SELECT "CHAVE" 
              FROM public."FECHAMENTO_01" B 
              WHERE TO_CHAR(B."DATA", 'YYYY-MM') = mes_atual
          );

        -- Atualizar valores ANTERIOR e SALDO_ATUAL
        UPDATE public."FECHAMENTO_01"
        SET "ANTERIOR" = subquery."SALDO_ATUAL"
        FROM (
            SELECT *
            FROM public."FECHAMENTO_01"
            WHERE "ANO_MES" = TO_CHAR(TO_DATE(mes_atual, 'YYYY-MM') - interval '1 month', 'YYYY-MM')
        ) AS subquery
        WHERE public."FECHAMENTO_01"."ANO_MES" = mes_atual
          AND public."FECHAMENTO_01"."EMPRESA" = subquery."EMPRESA"
         -- AND public."FECHAMENTO_01"."IDBANCODADOS" = subquery."IDBANCODADOS"
         -- AND public."FECHAMENTO_01"."CODCOLIGADA" = subquery."CODCOLIGADA"
         -- AND public."FECHAMENTO_01"."CODFILIAL" = subquery."CODFILIAL"
          AND public."FECHAMENTO_01"."CODCONTA" = subquery."CODCONTA";

        UPDATE public."FECHAMENTO_01"
        SET "SALDO_ATUAL" = subquery."SALDO"
        FROM (
            SELECT *, ("ANTERIOR" + "DEBITOS" + "CREDITOS") AS "SALDO"
            FROM public."FECHAMENTO_01"
            WHERE "ANO_MES" = mes_atual
        ) AS subquery
        WHERE public."FECHAMENTO_01"."ANO_MES" = mes_atual
          AND public."FECHAMENTO_01"."EMPRESA" = subquery."EMPRESA"
         -- AND public."FECHAMENTO_01"."IDBANCODADOS" = subquery."IDBANCODADOS"
         -- AND public."FECHAMENTO_01"."CODCOLIGADA" = subquery."CODCOLIGADA"
         -- AND public."FECHAMENTO_01"."CODFILIAL" = subquery."CODFILIAL"
          AND public."FECHAMENTO_01"."CODCONTA" = subquery."CODCONTA";
    END LOOP; 	
           
drop table if exists public."FECHAMENTO_02";

create table public."FECHAMENTO_02" as
  select	* ,
	replace(    replace(
			            replace(
			                replace(
			                    replace(
			                        replace(
			                            replace(
			                                replace(
			                                    replace(A."DESCRICAO", '\\', '.'), -- Escapando a barra invertida
			                                    ':', '.'
			                                ),
			                                '*', '.'
			                            ),
			                            '>', '.'
			                        ),
			                        '<', '.'
			                    ),
			                    '|', '.'
			                ),
			                '?', '.'
			            ), 
			            '/', '.'
			        ),
			        '.', '.' -- Isso foi adicionado para evitar erro de função com um argumento
			    ) as "DESCRICAO_TRATADA"
	from public."FECHAMENTO_01" A;        
 
    -- Dropar e criar a tabela FT_RAZAO
    DROP TABLE IF EXISTS public."FT_RAZAO";
    CREATE TABLE public."FT_RAZAO" AS  
    SELECT
        CAST(1 AS INT) AS "POSICAO",
        "ANO_MES",
        "DATA", 
        "EMPRESA",
        "REDUZIDO",
        "CODCONTA",
        CAST("IDPARTIDA" AS VARCHAR(50)), 
        "DESCRICAO",
        "COMPLEMENTO",
        CAST(0 AS numeric(15, 4)) AS "ANTERIOR",
        "VALOR_DEBITO" AS "DEBITOS", 
        "VALOR_CREDITO"  AS "CREDITOS",
        CAST(0 AS numeric(15, 4)) AS "SALDO_ATUAL",
        "IDBANCODADOS",
        "CODCOLIGADA",
        "CODFILIAL",
        	replace(    replace(
			            replace(
			                replace(
			                    replace(
			                        replace(
			                            replace(
			                                replace(
			                                    replace(A."DESCRICAO", '\\', '.'), -- Escapando a barra invertida
			                                    ':', '.'
			                                ),
			                                '*', '.'
			                            ),
			                            '>', '.'
			                        ),
			                        '<', '.'
			                    ),
			                    '|', '.'
			                ),
			                '?', '.'
			            ), 
			            '/', '.'
			        ),
			        '.', '.' -- Isso foi adicionado para evitar erro de função com um argumento
			    ) as "DESCRICAO_TRATADA",
			    "DOCUMENTO"
    FROM public."FT_DEBITO_CREDITO" A;

   
    -- Inserir dados na tabela FT_RAZAO a partir de FT_FECHAMENTO_02
    INSERT INTO public."FT_RAZAO" (
        "POSICAO",
        "ANO_MES",
        "DATA", 
        "EMPRESA",
        "REDUZIDO",
        "CODCONTA",
        "IDPARTIDA", 
        "DESCRICAO",
        "COMPLEMENTO",
        "ANTERIOR",
        "DEBITOS", 
        "CREDITOS",
        "SALDO_ATUAL",
        "DESCRICAO_TRATADA",
        "IDBANCODADOS",
        "CODCOLIGADA",
        "CODFILIAL",
        "DOCUMENTO"
    )
    SELECT
        CAST(9999 AS INT) AS "POSICAO", 
        "ANO_MES",
        "DATA", 
        "EMPRESA",
        "REDUZIDO",
        "CODCONTA",
        "ANO_MES" AS "IDPARTIDA", 
        "DESCRICAO",
        NULL,
        "ANTERIOR",
        "DEBITOS", 
        "CREDITOS",
        "SALDO_ATUAL",
        "DESCRICAO_TRATADA",
        "IDBANCODADOS",
        "CODCOLIGADA",
        "CODFILIAL",
        NULL
    FROM public."FECHAMENTO_02" A;
   
   CREATE INDEX idx_id_RAZAO_EMPRESA ON public."FT_RAZAO" ("EMPRESA");
   CREATE INDEX idx_id_RAZAO_CONTA ON public."FT_RAZAO" ("CODCONTA");
   CREATE INDEX idx_ft_razao_ano_mes ON public."FT_RAZAO"("ANO_MES");
   CREATE INDEX idx_ft_razao_documento ON public."FT_RAZAO"("DOCUMENTO");


       
-- CRIA A TABELA RAZAO PARA USAR NO EXCEL  
DROP TABLE IF EXISTS public."FT_RAZAO_DASH_0";
CREATE TABLE public."FT_RAZAO_DASH_0" AS 
SELECT     A."POSICAO",
		    A."ANO_MES",
		    A."DATA",
		    A."EMPRESA",
		    A."REDUZIDO",
		    A."CODCONTA",
		    A."IDPARTIDA",
		    A."ANTERIOR",
		    A."DEBITOS",
		    A."CREDITOS",
		    A."SALDO_ATUAL",
		    A."DESCRICAO_TRATADA",
		    A."CODFILIAL",
		    CASE 
		        WHEN A."COMPLEMENTO" IS NULL AND A."IDPARTIDA" LIKE '202%' THEN 'Fechamento'
		        ELSE A."COMPLEMENTO" 
		    END AS "HISTORICO",
		    A."DEBITOS" AS "VALOR",
		    A."CODCOLIGADA",
		    CONCAT(A."CODFILIAL", A."EMPRESA", '-', A."CODCONTA", '-', A."ANO_MES") AS "CHAVE_EMPRESA_CONTA_MES",
		    B.preparado  AS "PREPARADO",
		    B.data_preparado  AS "DATA_PREPARADO",
		    B.conferido  AS "CONFERIDO",
		    B.data_conferido  AS "DATA_CONFERIDO",
		    B.pendencia  AS "PENDENCIA",
		    B.resp_pendencia  AS "RESP_PENDENCIA",
		    B.data_pendencia  AS "DATA_PENDENCIA",
			CASE 
		        WHEN TRIM(C.prioridade)  = '1' THEN '1-Baixa'
		        WHEN TRIM(C.prioridade)  = '2' THEN '2-Regular'
		        WHEN TRIM(C.prioridade)  = '3' THEN '3-Média'
		        WHEN TRIM(C.prioridade)  = '4' THEN '4-Alta'
		        ELSE 'Não definido'
		    END AS prioridade,
		    CASE 
		        WHEN TRIM(B.preparado) IS NULL OR TRIM(B.preparado) = '' THEN 0
		        ELSE 1 
		    END AS "QTD_PREPARADO",
		    CASE 
		        WHEN TRIM(B.conferido) IS NULL OR TRIM(B.conferido) = '' THEN 0
		        ELSE 1 
		    END AS "QTD_CONFERIDO",
		    CASE 
		        WHEN TRIM(B.pendencia) IS NULL OR TRIM(B.pendencia) = '' THEN 0
		        ELSE 1 
		    END AS "QTD_PENDENCIA",
		    CASE 
		        WHEN 
		            (TRIM(B.preparado) IS NOT NULL OR TRIM(B.preparado) <> '') AND 
		            (TRIM(B.conferido) IS NOT NULL OR TRIM(B.conferido) <> '') AND 
		            (TRIM(B.pendencia) IS NULL OR TRIM(B.pendencia) = '') 
		        THEN 1
		        ELSE 0 
		    END AS "QTD_FINALIZADO",
		    CASE 
		        WHEN TRIM(B.preparado) IS NULL OR TRIM(B.preparado) = '' THEN 'NÃO'
		        ELSE 'SIM' 
		    END AS "STATUS_PREPARADO",
		    CASE 
		        WHEN TRIM(B.conferido) IS NULL OR TRIM(B.conferido) = '' THEN 'NÃO'
		        ELSE 'SIM'
		    END AS "STATUS_CONFERIDO",
		    CASE 
		        WHEN TRIM(B.pendencia) IS NULL OR TRIM(B.pendencia) = '' THEN 'NÃO'
		        ELSE 'SIM'
		    END AS "STATUS_PENDENCIA",
		    CASE 
		        WHEN 
		            (TRIM(B.preparado) IS NOT NULL OR TRIM(B.preparado) <> '') AND 
		            (TRIM(B.conferido) IS NOT NULL OR TRIM(B.conferido) <> '') AND 
		            (TRIM(B.pendencia) IS NULL OR TRIM(B.pendencia) = '') 
		        THEN 'SIM'
		        ELSE 'NÃO' 
		    END AS "STATUS_FINALIZADO" ,
		  ROW_NUMBER() OVER () AS "NUMERO_LINHA", -- Número da linha
		  A."DOCUMENTO",
		  A."IDBANCODADOS" 
FROM 
    public."FT_RAZAO" A
LEFT JOIN 
    public."FVALIDACAO" B 
    ON B.chave_validacao  = CONCAT(A."CODFILIAL", A."EMPRESA", A."CODCONTA", A."ANO_MES")
LEFT JOIN 
    public."DPRIORIDADE" C 
    ON C.chave  = CONCAT(A."EMPRESA", A."ANO_MES")
ORDER BY 
    A."DATA",  
    A."EMPRESA",  
    A."POSICAO" ASC;
    
DROP INDEX IF EXISTS IDX__RAZAO_DASH_ANO_MES;
DROP INDEX IF EXISTS IDX__RAZAO_DASH_EMPRESA;
DROP INDEX IF EXISTS IDX__RAZAO_DASH_CODCONTA;
DROP INDEX IF EXISTS IDX__RAZAO_DASH_DEBITOS;
DROP INDEX IF EXISTS IDX__RAZAO_DASH_CREDITOS;
DROP INDEX IF EXISTS IDX__RAZAO_DASH_NUMERO_LINHA;
DROP INDEX IF EXISTS IDX__RAZAO_DASH_HISTORICO;
DROP INDEX IF EXISTS IDX__RAZAO_DASH_DATA;

CREATE INDEX IDX__RAZAO_DASH_DATA ON public."FT_RAZAO_DASH_0" ("DATA");
CREATE INDEX IDX__RAZAO_DASH_HISTORICO ON public."FT_RAZAO_DASH_0" ("HISTORICO");
CREATE INDEX IDX__RAZAO_DASH_ANO_MES ON public."FT_RAZAO_DASH_0" ("ANO_MES");
CREATE INDEX IDX__RAZAO_DASH_EMPRESA ON public."FT_RAZAO_DASH_0" ("EMPRESA");
CREATE INDEX IDX__RAZAO_DASH_CODCONTA ON public."FT_RAZAO_DASH_0" ("CODCONTA");
CREATE INDEX IDX__RAZAO_DASH_DEBITOS ON public."FT_RAZAO_DASH_0" ("DEBITOS");
CREATE INDEX IDX__RAZAO_DASH_CREDITOS ON public."FT_RAZAO_DASH_0" ("CREDITOS");   
CREATE INDEX IDX__RAZAO_DASH_NUMERO_LINHA ON public."FT_RAZAO_DASH_0" ("NUMERO_LINHA");   
   
/*
WITH registros_duplicados AS (
    SELECT "ANO_MES", "EMPRESA", "CODCONTA", COUNT(DISTINCT "CODFILIAL") AS qtd_filiais
    FROM public."FT_RAZAO_DASH_0"
    WHERE "EMPRESA" IN ('AEPL AGROPECUARIA LTDA', 'AGV PARTICIPAÇÕES LTDA')  -- Empresas desejadas
      AND "HISTORICO" = 'Fechamento'
    GROUP BY "ANO_MES", "EMPRESA", "CODCONTA"
    HAVING COUNT(DISTINCT "CODFILIAL") = 2  -- Quando existem dois registros de filiais diferentes
)
DELETE FROM public."FT_RAZAO_DASH_0" A
WHERE A."CODFILIAL" in (2,3)  and A."EMPRESA" IN ('AEPL AGROPECUARIA LTDA', 'AGV PARTICIPAÇÕES LTDA') -- Eliminar somente os registros com CODFILIAL = 2
  AND EXISTS (
    SELECT 1
    FROM registros_duplicados R
    WHERE R."ANO_MES" = A."ANO_MES"
      AND R."EMPRESA" = A."EMPRESA"
      AND R."CODCONTA" = A."CODCONTA"
  );
*/

DROP TABLE IF EXISTS public."FT_RAZAO_DASH";
CREATE TABLE public."FT_RAZAO_DASH" AS
SELECT DISTINCT A.*
FROM public."FT_RAZAO_DASH_0" A
ORDER BY 
    A."DATA",  
    A."EMPRESA",  
    A."POSICAO" ASC;
   


   
-- ajustes no SALDO_ATUAL E ANTERIOR
UPDATE public."FT_RAZAO_DASH"
        SET "SALDO_ATUAL" 	= null,
        	 "ANTERIOR"		= NULL
	WHERE "HISTORICO" = 'Fechamento' 
			AND "ANO_MES" <> '2024-01'
            AND "IDBANCODADOS" not in (2,1)
      		AND "CODCOLIGADA" not in (26,3);
	

DROP INDEX IF EXISTS IDX_FT_RAZAO_DASH_CODCONTA; 
DROP INDEX IF EXISTS IDX_FT_RAZAO_DASH_HISTORICO; 
DROP INDEX IF EXISTS IDX_RAZAO_DASH2_NUMERO_LINHA;

CREATE INDEX IDX_FT_RAZAO_DASH_CODCONTA ON public."FT_RAZAO_DASH" ("CODCONTA");
CREATE INDEX IDX_FT_RAZAO_DASH_HISTORICO ON public."FT_RAZAO_DASH" ("HISTORICO");
CREATE INDEX IDX_RAZAO_DASH2_NUMERO_LINHA ON public."FT_RAZAO_DASH" ("NUMERO_LINHA");  

    -- Obter os meses ordenados
    SELECT ARRAY_AGG("ANO_MES" ORDER BY "ANO_MES") 
        INTO mes_array
        FROM (
            SELECT DISTINCT "ANO_MES" 
            FROM public."FT_DEBITO_CREDITO"
            where "IDBANCODADOS" not in (2,1)
      			 AND "CODCOLIGADA" not in (26,3)
				 AND "ANO_MES" <> '2024-01' 
        ) AS subquery;

    -- Iterar sobre o array de meses
    FOREACH mes_atual IN ARRAY mes_array LOOP      
	    
	 UPDATE public."FT_RAZAO_DASH"
        SET "ANTERIOR" = null
	  WHERE "HISTORICO" = 'Fechamento' and "ANO_MES" = mes_atual
	  AND "IDBANCODADOS" not in (2,1)
      AND "CODCOLIGADA" not in (26,3);
	 
	 -- SALDO ANTERIOR
	 UPDATE public."FT_RAZAO_DASH"
        SET "ANTERIOR" = subquery."SALDO_ATUAL"
        FROM (
            SELECT distinct *
            FROM public."FT_RAZAO_DASH"
            WHERE "ANO_MES" = TO_CHAR(TO_DATE(mes_atual, 'YYYY-MM') - interval '1 month', 'YYYY-MM')
              AND "HISTORICO" = 'Fechamento'
              AND "IDBANCODADOS" not in (2,1)
      		  AND "CODCOLIGADA" not in (26,3)
        ) AS subquery
        WHERE public."FT_RAZAO_DASH"."HISTORICO" = 'Fechamento'
          	AND public."FT_RAZAO_DASH"."ANO_MES"  = mes_atual
          --AND public."FT_RAZAO_DASH"."IDBANCODADOS" = subquery."IDBANCODADOS"
          --AND public."FT_RAZAO_DASH"."CODCOLIGADA" = subquery."CODCOLIGADA"
          --AND public."FT_RAZAO_DASH"."CODFILIAL" = subquery."CODFILIAL"
          	AND public."FT_RAZAO_DASH"."EMPRESA" = subquery."EMPRESA"
          	AND public."FT_RAZAO_DASH"."CODCONTA" = subquery."CODCONTA"
          	AND public."FT_RAZAO_DASH"."IDBANCODADOS" not in (2,1)
      		AND public."FT_RAZAO_DASH"."CODCOLIGADA" not in (26,3);
	 
        -- Atualizar valores ANTERIOR e SALDO_ATUAL
     UPDATE public."FT_RAZAO_DASH"
        SET "SALDO_ATUAL" = subquery."SALDO"
        FROM (
            SELECT "NUMERO_LINHA","ANO_MES","ANTERIOR" + "DEBITOS" + "CREDITOS" AS "SALDO"
            FROM public."FT_RAZAO_DASH"
            WHERE "HISTORICO" = 'Fechamento' 
             AND "ANO_MES" = mes_atual
             AND "IDBANCODADOS" not in (2,1)
      		 AND "CODCOLIGADA" not in (26,3)
 
        ) AS subquery
        WHERE public."FT_RAZAO_DASH"."HISTORICO" = 'Fechamento'
          AND public."FT_RAZAO_DASH"."NUMERO_LINHA" = subquery."NUMERO_LINHA"
          AND public."FT_RAZAO_DASH"."ANO_MES"  = subquery."ANO_MES"
		  AND public."FT_RAZAO_DASH"."ANO_MES"  = mes_atual
		  AND public."FT_RAZAO_DASH"."IDBANCODADOS" not in (2,1)
          AND public."FT_RAZAO_DASH"."CODCOLIGADA" not in (26,3);
		 
    END LOOP; 	
----------------------------------------------------------------------------------------------   
   
    
   
   
DROP TABLE IF EXISTS public."FT_RAZAO_DASH_RESUMO";
CREATE TABLE public."FT_RAZAO_DASH_RESUMO" AS
SELECT 
	TO_DATE(X."ANO_MES", 'YYYY-MM') AS "DATA",
    X."ANO_MES",
    X."EMPRESA",
    X."CODCONTA",
    X."CHAVE_EMPRESA_CONTA_MES",
    CONCAT(X."EMPRESA",X."ANO_MES") as "CHAVE",
    1 AS "QTD_TOTAL",
    CASE WHEN X."QTD_PREPARADO" = 0 THEN 0 ELSE 1 END AS "QTD_PREPARADO",
    CASE WHEN X."QTD_CONFERIDO" = 0 THEN 0 ELSE 1 END AS "QTD_CONFERIDO",
    CASE WHEN X."QTD_PENDENCIA" = 0 THEN 0 ELSE 1 END AS "QTD_PENDENCIA",
    CASE WHEN X."QTD_FINALIZADO" = 0 THEN 0 ELSE 1 END AS "QTD_FINALIZADO"
FROM (
    SELECT 
        "ANO_MES",
        "EMPRESA",
        "CODCONTA",
        "CHAVE_EMPRESA_CONTA_MES",        
        1 AS "QTD_TOTAL",
        SUM("QTD_PREPARADO") AS "QTD_PREPARADO",
        SUM("QTD_CONFERIDO") AS "QTD_CONFERIDO",
        SUM("QTD_PENDENCIA") AS "QTD_PENDENCIA",
        SUM("QTD_FINALIZADO") AS "QTD_FINALIZADO"
    FROM public."FT_RAZAO_DASH" 
    GROUP BY 
        "ANO_MES",
        "EMPRESA",
        "CODCONTA",
        "CHAVE_EMPRESA_CONTA_MES"
) X;

-- indice para que essa tabela possa montar uma view com a fvalicao e dprioridade
-- DROP INDEX IF exists idx_CHAVE_EMPRESA_CONTA_MES;
-- CREATE INDEX idx_CHAVE_EMPRESA_CONTA_MES ON public."FT_RAZAO_DASH" ("CHAVE_EMPRESA_CONTA_MES");   


	TRUNCATE TABLE public."FT_RAZAO_DASH_01";
	
	INSERT INTO public."FT_RAZAO_DASH_01"
	SELECT A."POSICAO", 
			A."ANO_MES", 
			A."DATA",
			A."EMPRESA", 
			A."REDUZIDO", 
			A."CODCONTA", 
			A."IDPARTIDA", 
			A."ANTERIOR", 
			A."DEBITOS", 
			A."CREDITOS", 
			A."SALDO_ATUAL", 
			A."DESCRICAO_TRATADA",
			A."CODFILIAL",
			A."HISTORICO", 
			A."VALOR", 
			A."CODCOLIGADA", 
			A."CHAVE_EMPRESA_CONTA_MES", 
			A."PREPARADO",
			A."DATA_PREPARADO", 
			A."CONFERIDO", 
			A."DATA_CONFERIDO",
			A."PENDENCIA", 
			A."RESP_PENDENCIA",
			A."DATA_PENDENCIA",
			A.prioridade,
			A."QTD_PREPARADO",
			A."QTD_CONFERIDO",
			A."QTD_PENDENCIA", 
			A."QTD_FINALIZADO",
			A."STATUS_PREPARADO",
			A."STATUS_CONFERIDO",
			A."STATUS_PENDENCIA",
			A."STATUS_FINALIZADO",
			A."NUMERO_LINHA",			
			TO_CHAR(CURRENT_DATE, 'YYYY-MM-DD') AS "DATAEXECUCAO",
     		TO_CHAR(CURRENT_TIMESTAMP, 'HH24:MI') as "HORAEXECUCAO",
     		A."DOCUMENTO",
     		null as "NOME_FORNECEDOR",
			null as "CODCFO" ,
			null as "CODTDO" ,
			null as "SEGUNDONUMERO" ,
			null as "NUMERODOCUMENTO" ,
			null as "VALORVINCULO" ,
			null as "VALOR_NF_EM_ABERTO",
			null as "TIPOBAIXA"
	FROM public."FT_RAZAO_DASH" A;

    DROP INDEX IF EXISTS IDX_01DASH_EMPRESA;
   	DROP INDEX IF EXISTS IDX_01DASH_CODCONTA;
	DROP INDEX IF EXISTS IDX_01DASH_POSICAO;
   	DROP INDEX IF EXISTS IDX_01DASH_DATA;   
   
    CREATE INDEX IDX_01DASH_EMPRESA ON public."FT_RAZAO_DASH_01" ("EMPRESA");
    CREATE INDEX IDX_01DASH_CODCONTA ON public."FT_RAZAO_DASH_01" ("CODCONTA");
    CREATE INDEX IDX_01DASH_POSICAO ON public."FT_RAZAO_DASH_01" ("POSICAO");
    CREATE INDEX IDX_01DASH_DATA ON public."FT_RAZAO_DASH_01" ("DATA");
   	
   
   
   
   	--select * from public.fc_fech_composicao();
  /* 
    DROP TABLE IF EXISTS public."FT_DEBITO_CREDITO";
    DROP TABLE IF EXISTS public."FT_VALIDACAO_1";
    DROP TABLE IF EXISTS public."FT_RAZAO_0";
    DROP TABLE IF EXISTS public."FT_RAZAO_00";
    DROP TABLE IF EXISTS public."FT_RAZAO_01";
    --DROP TABLE IF EXISTS public."FT_COMPOSICAO";
    DROP TABLE IF EXISTS public."FT_VALIDACAO_1";
    DROP TABLE IF EXISTS public."FECHAMENTO_01";
    DROP TABLE IF EXISTS public."FECHAMENTO_02";
    DROP TABLE IF EXISTS public."FT_RAZAO_DASH_0";
    DROP TABLE IF EXISTS public."FVALIDACAO_1";
   */
   	PERFORM fc_fech_composicao_v12();
   
   	PERFORM public.fc_nf_emitidas();
  
   	PERFORM public.fc_validacao();
   
END;
$function$
;
