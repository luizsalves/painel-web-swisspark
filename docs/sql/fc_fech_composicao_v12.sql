CREATE OR REPLACE FUNCTION public.fc_fech_composicao_v12()
 RETURNS void
 LANGUAGE plpgsql
AS $function$


/* v12 Ajustado o Fornecedor deSErviços foi comentado essa parte 
 * pois todos os valores devem entrar inlcusive os Zerados
 * 
 * 1) Fornecedores de Serviços - comentado o valor > 0 
FROM public."FT_NF_EMITIDAS"  B
where  
--B."VALOR_NF_EM_ABERTO" > 0
 "DESCRICAO" in ('FORNECEDORES DE MATERIAIS','FORNECEDORES DE SERVIÇOS') ;
 
 2) Adiantamento de Fornecedores - comentado o valor > 0 
 FROM public.FT_LANCAMENTOS  B
where  
--B."VALOR_NF_EM_ABERTO" > 0 
TRIM(UPPER("DESCRICAO")) in 
('ADIANTAMENTO A FORNECEDORES','ADIANTAMENTO DE FORNECEDORES','ADIANTAMENTO DIVERSOS','ADIANTAMENTOS DIVERSOS') ;
*/

DECLARE
    data_mes_anterior DATE;
    ano_mes_anterior TEXT;
    primeiro_dia_mes_anterior DATE; 
    data_execucao  TEXT;
BEGIN
    -- Atribui o ultimo dia do mês anterior
	data_mes_anterior := DATE_TRUNC('month', CURRENT_DATE) - INTERVAL '1 day';

	-- Calcula o primeiro dia do mês anterior
    primeiro_dia_mes_anterior := DATE_TRUNC('month', CURRENT_DATE - INTERVAL '1 month');
    
    -- Atribui o ano e o mês anterior no formato 'YYYY-MM'
    ano_mes_anterior := TO_CHAR(CURRENT_DATE - INTERVAL '1 month', 'YYYY-MM');
   
    SELECT 
        "DATAEXECUCAO" || ' ' || "HORAEXECUCAO"
    INTO 
        data_execucao
    FROM 
        public."FT_RAZAO_DASH_01" A
    WHERE 
        A."DATAEXECUCAO" IS NOT NULL
    LIMIT 1;  
   
   
drop index if exists idx_id_LANCA_CODCFO;
drop index if exists idx_id_LANCA_NUMERODOCUMENTO;   
drop index if exists idx_id_LANCA_DATABAIXA;
drop index if exists idx_id_LANCA_DATACANCELAMENTO;
drop index if exists idx_id_FT_COMPO_HISTORICO;

CREATE INDEX idx_id_LANCA_CODCFO ON public."FLAN"  ("CODCFO");		
CREATE INDEX idx_id_LANCA_NUMERODOCUMENTO ON public."FLAN"  ("NUMERODOCUMENTO");
CREATE INDEX idx_id_LANCA_DATABAIXA ON public."FLAN"  ("DATABAIXA");
CREATE INDEX idx_id_LANCA_DATACANCELAMENTO ON public."FLAN"  ("DATACANCELAMENTO");
CREATE INDEX idx_id_FT_COMPO_HISTORICO ON public."FT_COMPOSICAO"  ("HISTORICO");

------------------------------------------------------- REGRA 1 ADIANTAMENTO DE FORNECEDORES ------------------------------------------------------------------------ 
-- OS Adiantamentos aos Fornecedores nascem no Lançamento de DÉBITO com valor Positivo
-- E quando as NFs são emitidas as baixas nos lançamentos vem pelo CRÉDITO com valor Negativo
  
DROP TABLE IF EXISTS public.FT_LANCAMENTOS;
CREATE TABLE public.FT_LANCAMENTOS AS   
SELECT distinct
	row_number() OVER() as "NUMERO_LINHA",
    UPPER(TRIM(E."NOME")) AS "EMPRESA",
    TO_CHAR(CURRENT_DATE, 'YYYY-MM') AS "ANOMES_CONTABILIZBX",
	TO_DATE('2000-01-01', 'YYYY-MM-DD') AS "DATACONTABILIZBX",	
    D."NOME",
    F."IDBANCODADOS", 
    F."CODCOLIGADA",
    cast(null as VARCHAR(40))  as "CODCONTA",
    cast(null as VARCHAR(250))  as "DESCRICAO",
    F."IDLAN", 
    F."CODFILIAL",
    F."CODCFO",
    F."CODTDO",
    T."DESCRICAO" AS "CODTDO_DESC",
    F."NUMERODOCUMENTO", 
    F."SEGUNDONUMERO",
    C."IDLANREL", 
    C."TIPOREL",
    C."IDBAIXA",
    C."IDBAIXAREL",
    C."VALORVINCULO",
    F."VALORORIGINAL",
    F."VALORBAIXADO",
    F."VALORLIQUIDO",
    cast(0 as numeric(15,4)) as "VALORVINCULO_ACUM",
    cast(0 as numeric(15,4)) as "VALOR_NF_EM_ABERTO",
    cast(0 as numeric(15,4)) as "VALOR_NF_EMITIDAS",
    F."STATUSLAN",
    F."CODCCUSTO",    
    F."DATAVENCIMENTO",
    F."DATAEMISSAO",
    F."DATABAIXA",
    F."DATAPAG",
    F."DATACANCELAMENTO"
FROM public."FLAN" F
LEFT JOIN public."FRELLAN" C 
    ON F."IDBANCODADOS" = C."IDBANCODADOS" 
    AND F."CODCOLIGADA" = C."CODCOLIGADA"
    AND F."IDLAN" = C."IDLAN"
LEFT JOIN public."FTDO" T
    ON T."IDBANCODADOS" = F."IDBANCODADOS"
    AND T."CODCOLIGADA" = F."CODCOLIGADA"
    AND T."CODTDO" = F."CODTDO"
LEFT JOIN public."FCFO" D
    ON D."CODCFO" = F."CODCFO"
    AND D."IDBANCODADOS" = F."IDBANCODADOS"
    AND D."CODCOLIGADA" = F."CODCOLIGADA"
LEFT JOIN public."GFILIAL" E
    ON E."IDBANCODADOS" = F."IDBANCODADOS"
    AND E."CODCOLIGADA" = F."CODCOLIGADA"
    AND E."CODFILIAL" = F."CODFILIAL"
WHERE F."CODTDO" in ('ADF', 'ADC')
	--and F."DATABAIXA" <= DATE '2024-12-31' 
	and F."DATABAIXA"  <= data_mes_anterior
	and F."DATACANCELAMENTO" is NULL;


drop index if exists idx_id_FRELLAN_DT_CONTABILIZBX;
drop index if exists idx_id_FRELLAN_CODCFO;
drop index if exists idx_id_FRELLAN_NOME;
drop index if exists idx_id_FRELLAN_NUMERO_LINHA;
drop index if exists idx_id_FRELLAN_IDLANREL;
drop index if exists idx_id_FT_LAN_NUMERODOCUMENTO;
drop index if exists idx_id_FRELLAN_SEGUNDONUMERO;
drop index if exists idx_id_FT_LAN_EMPRESA;

CREATE INDEX idx_id_FRELLAN_DT_CONTABILIZBX ON public.FT_LANCAMENTOS ("ANOMES_CONTABILIZBX");
CREATE INDEX idx_id_FRELLAN_CODCFO ON public.FT_LANCAMENTOS ("CODCFO");
CREATE INDEX idx_id_FRELLAN_NOME ON public.FT_LANCAMENTOS ("NOME");
CREATE INDEX idx_id_FT_LAN_EMPRESA ON public.FT_LANCAMENTOS ("EMPRESA");
CREATE INDEX idx_id_FT_LAN_NUMERODOCUMENTO ON public.FT_LANCAMENTOS ("NUMERODOCUMENTO");
CREATE INDEX idx_id_FRELLAN_NUMERO_LINHA ON public.FT_LANCAMENTOS ("NUMERO_LINHA"); 
CREATE INDEX idx_id_FRELLAN_IDLANREL ON public.FT_LANCAMENTOS ("IDLANREL"); 
CREATE INDEX idx_id_FRELLAN_SEGUNDONUMERO ON public.FT_LANCAMENTOS ("SEGUNDONUMERO"); 


UPDATE public.FT_LANCAMENTOS
  SET "NOME" = subquery."NOME"
   FROM ( 
		SELECT *  	FROM public."FCFO" 
        ) 
       AS subquery
WHERE public.FT_LANCAMENTOS."CODCFO" = subquery."CODCFO" AND
	  public.FT_LANCAMENTOS."IDBANCODADOS" =  subquery."IDBANCODADOS" AND
	  public.FT_LANCAMENTOS."NOME" is null;
	
	 
-- CARIMBA CONTA	 
UPDATE public.FT_LANCAMENTOS
  SET 	"CODCONTA" 	= subquery."CODCONTA"
FROM (  
		SELECT ltrim(split_part("DOCUMENTO", '/', 1), '0') as "DOCUMENTO",
				"IDBANCODADOS" ,
				"CODCOLIGADA" ,
				"CODCONTA" 
		FROM public."FT_RAZAO_DASH"
	  ) 
	    AS subquery
WHERE 	public.FT_LANCAMENTOS."IDBANCODADOS" =  subquery."IDBANCODADOS" 	AND
		public.FT_LANCAMENTOS."CODCOLIGADA" =  subquery."CODCOLIGADA"		and
		ltrim(public.FT_LANCAMENTOS."NUMERODOCUMENTO",'0') =  subquery."DOCUMENTO";
		

-- CARIMBA DESCRICAO		
UPDATE public.FT_LANCAMENTOS
  SET 	"DESCRICAO" 	= subquery."DESCRICAO"
FROM (  
		select "IDBANCODADOS", "CODCOLIGADA", "CODCONTA", "REDUZIDO", TRIM(UPPER("DESCRICAO")) as "DESCRICAO"
		FROM public."CCONTA"
	  ) 
	    AS subquery
WHERE 	public.FT_LANCAMENTOS."IDBANCODADOS" =  subquery."IDBANCODADOS" 	AND
		public.FT_LANCAMENTOS."CODCOLIGADA" =  subquery."CODCOLIGADA"		and
		public.FT_LANCAMENTOS."CODCONTA" =  subquery."CODCONTA";
		

		
		
	
	
-- Nessa tabela eu capturo os vinculos que são feitos conforme as NFs que vão sendo emitidas
-- FRELLAN = parcelas dos lançamentos acima 
-- FLANBAIXA = Baixa das parcelas

DROP TABLE IF EXISTS public.FT_LANCAMENTOS_PARCELAS;
CREATE TABLE public.FT_LANCAMENTOS_PARCELAS AS 
SELECT distinct	
			B."IDBANCODADOS" ,
			B."CODCOLIGADA" ,
			B."CODFILIAL",
		    F."CODCFO",
		    F."SEGUNDONUMERO",
		    F."NUMERODOCUMENTO" ,
		    C."IDLAN" ,
		    C."IDLANREL" ,
		    C."TIPOREL" ,
		    C."IDBAIXA" ,
		    C."IDBAIXAREL" ,
		    C."VALORVINCULO" ,
		    B."VALORBAIXA" ,
		    B."VALORORIGINAL" ,
		    L."DATAEMISSAO",
		    B."DATABAIXA" ,
		    B."DATACONTABILIZBX" ,
		    B."DATACANCELBAIXA" ,
		   	B."NUMCONTABILBX" ,
		   	B."TIPOBAIXA" ,
		   	B."STATUS" 
FROM public.FT_LANCAMENTOS F
LEFT JOIN public."FRELLAN" C 
    ON F."IDBANCODADOS" = C."IDBANCODADOS"
    AND F."CODCOLIGADA" = C."CODCOLIGADA"
    AND F."IDLANREL" = C."IDLANREL"
LEFT JOIN public."FLANBAIXA" B
    ON B."IDBANCODADOS" = C."IDBANCODADOS"
    AND B."CODCOLIGADA" = C."CODCOLIGADA"
    AND B."IDBAIXA" = C."IDBAIXA"
LEFT JOIN public."FLAN" L
    ON B."IDBANCODADOS" = L."IDBANCODADOS"
    AND B."CODCOLIGADA" = L."CODCOLIGADA"
    AND B."IDLAN" = L."IDLAN"  
where 	 C."TIPOREL"  = 28
	 and B."DATABAIXA" <= data_mes_anterior   
	 and L."DATAEMISSAO" <= data_mes_anterior;
	--AND B."DATABAIXA"   <= DATE '2024-12-31'
	--and L."DATAEMISSAO" <= DATE '2024-12-31';
	



--------------------------------------------- 401---------------------------------------------------		 
-- Atualiza o valor das NFs entregues
--  Valor dos Vinculos FREELAN lançados pelo contas a pagar
WITH cte_acumulado AS (
    SELECT 
        "IDBANCODADOS", 
        "CODCOLIGADA",
        "CODFILIAL",
        "CODCFO",
        "SEGUNDONUMERO",
        "IDLANREL",
        MAX("DATACONTABILIZBX") as "DATACONTABILIZBX",
        SUM("VALORVINCULO") AS "VALORVINCULO_ACUM"
    FROM public.FT_LANCAMENTOS_PARCELAS 
    WHERE "IDBAIXAREL" IS NOT NULL AND "DATACANCELBAIXA" IS NULL
    GROUP BY 
        "IDBANCODADOS", 
        "CODCOLIGADA", 
        "CODFILIAL",
        "CODCFO",
        "SEGUNDONUMERO",
        "IDLANREL"
)
UPDATE public.FT_LANCAMENTOS t
SET "TIPOREL" = 401,
	"VALORVINCULO_ACUM" = cte."VALORVINCULO_ACUM",
	"DATACONTABILIZBX" = cte."DATACONTABILIZBX",
	"ANOMES_CONTABILIZBX" = TO_CHAR(cte."DATACONTABILIZBX", 'YYYY-MM')
FROM cte_acumulado cte
WHERE t."IDBANCODADOS" = cte."IDBANCODADOS"
	and t."CODCOLIGADA" = cte."CODCOLIGADA"
	and t."CODFILIAL" = cte."CODFILIAL"
    AND t."CODCFO" = cte."CODCFO"
    AND t."SEGUNDONUMERO" = cte."SEGUNDONUMERO"
    AND t."IDLANREL" = cte."IDLANREL";
   

   
--------------------------------------------- 501---------------------------------------------------
--------------------------------------- Ajuste para os Lançamentos pagos sem IDBAIXAREL ---------------------------------------------------------   
WITH cte_acumulado AS (
    SELECT 
        "IDBANCODADOS", 
        "CODCOLIGADA",
        "CODFILIAL",
        "CODCFO",
        "NUMERODOCUMENTO",
        "IDLANREL",
        SUM("VALORVINCULO"),
        MAX("DATACONTABILIZBX") as "DATACONTABILIZBX",
        SUM("VALORVINCULO") AS "VALORVINCULO_ACUM"
    FROM public.FT_LANCAMENTOS_PARCELAS 
    WHERE "IDBAIXAREL" IS NULL AND "DATACANCELBAIXA" IS null and "DATABAIXA" is not null 
    GROUP BY 
        "IDBANCODADOS", 
        "CODCOLIGADA",
        "CODFILIAL", 
        "CODCFO",
        "NUMERODOCUMENTO",
        "IDLANREL"
)
UPDATE public.FT_LANCAMENTOS t
SET "TIPOREL" = 501,
	"VALORVINCULO_ACUM" = cte."VALORVINCULO_ACUM",
	"DATACONTABILIZBX" = cte."DATACONTABILIZBX",
	"ANOMES_CONTABILIZBX" = TO_CHAR(cte."DATACONTABILIZBX", 'YYYY-MM')
FROM cte_acumulado cte
WHERE t."TIPOREL" <> 401
	and t."IDBANCODADOS" = cte."IDBANCODADOS"
	and t."CODCOLIGADA" = cte."CODCOLIGADA"
	and t."CODFILIAL" = cte."CODFILIAL"
    AND t."CODCFO" = cte."CODCFO"
    AND t."NUMERODOCUMENTO" = cte."NUMERODOCUMENTO"
    AND t."IDLANREL" = cte."IDLANREL"
    and ( t."VALORVINCULO_ACUM" is null or  t."VALORVINCULO_ACUM" = 0);
   
  
   
 
   
--------------------------------------------- 601---------------------------------------------------   
--------------------------------------- Ajuste S/NFs ---------------------------------------------------------
   
WITH cte_acumulado AS (
    SELECT 
        "IDBANCODADOS", 
        "CODCOLIGADA",
        "CODFILIAL",
        "CODCFO",
        "NUMERODOCUMENTO",
        "IDLANREL",
        SUM("VALORVINCULO"),
        MAX("DATACONTABILIZBX") as "DATACONTABILIZBX",
        SUM("VALORVINCULO") AS "VALORVINCULO_ACUM"
    FROM public.FT_LANCAMENTOS_PARCELAS 
    where "IDBAIXAREL" is NOT NULL AND "DATACANCELBAIXA" IS null and "DATABAIXA" is not null 
    GROUP BY 
        "IDBANCODADOS", 
        "CODCOLIGADA",
        "CODFILIAL", 
        "CODCFO",
        "NUMERODOCUMENTO",
        "IDLANREL"
)
UPDATE public.FT_LANCAMENTOS t
SET "TIPOREL" = 601,
	"VALORVINCULO_ACUM" = cte."VALORVINCULO_ACUM",
	"DATACONTABILIZBX" = cte."DATACONTABILIZBX",
	"ANOMES_CONTABILIZBX" = TO_CHAR(cte."DATACONTABILIZBX", 'YYYY-MM')
FROM cte_acumulado cte
WHERE t."TIPOREL" not in (401, 501)
	and t."IDBANCODADOS" = cte."IDBANCODADOS"
	and t."CODCOLIGADA" = cte."CODCOLIGADA"
	and t."CODFILIAL" = cte."CODFILIAL"
    AND t."CODCFO" = cte."CODCFO"
    AND t."NUMERODOCUMENTO" = cte."NUMERODOCUMENTO"
    AND t."IDLANREL" = cte."IDLANREL"
    and ( t."VALORVINCULO_ACUM" is null or  t."VALORVINCULO_ACUM" = 0);
   
--------------------------------------------------------------------------------------------------------


   
   
   
   
--------------------------------------------- 701---------------------------------------------------
------------------------------------------ CAIXINHA ----------------------------------------------------
WITH cte_acumulado AS (
    SELECT 
        "IDBANCODADOS", 
        "CODCOLIGADA",
        "CODFILIAL",
        "CREDITO"  as "CODCONTA",
        "DOCUMENTO",
        MAX("DATA") as "DATACONTABILIZBX",
        SUM("VALOR") AS "VALORVINCULO_ACUM"
    FROM public."CLANCA" C 
     GROUP BY 
         "IDBANCODADOS", 
        "CODCOLIGADA",
        "CODFILIAL",
        "CREDITO" ,
        "DOCUMENTO"
)
UPDATE public.FT_LANCAMENTOS t
SET "TIPOREL" = 701,
	"VALORVINCULO_ACUM" = cte."VALORVINCULO_ACUM",
	"DATACONTABILIZBX" = cte."DATACONTABILIZBX",
	"ANOMES_CONTABILIZBX" = TO_CHAR(cte."DATACONTABILIZBX", 'YYYY-MM')
FROM cte_acumulado cte
WHERE t."TIPOREL" not in (401, 501, 601)
	and t."IDBANCODADOS" = cte."IDBANCODADOS"
	and t."CODCOLIGADA" = cte."CODCOLIGADA"
	and t."CODFILIAL" = cte."CODFILIAL"
    AND t."NUMERODOCUMENTO" = cte."DOCUMENTO"
    AND t."CODCONTA" = cte."CODCONTA"
    and ( t."VALORVINCULO_ACUM" is null or  t."VALORVINCULO_ACUM" = 0);  
-------------------------------------------------------------------------------------------------------- 
  
   
   
   
   
--------------------------------------------- 801---------------------------------------------------  
-- Ajuste para os casos sem CODCFO
WITH cte_acumulado AS (
    SELECT 
        "IDBANCODADOS", 
        "CODCOLIGADA",
        "CODFILIAL",
        --"CODCFO",
        "SEGUNDONUMERO",
        "IDLANREL",
        MAX("DATACONTABILIZBX") as "DATACONTABILIZBX",
        SUM("VALORVINCULO") AS "VALORVINCULO_ACUM"
    FROM public.FT_LANCAMENTOS_PARCELAS 
    WHERE "IDBAIXAREL" IS NOT NULL AND "DATACANCELBAIXA" IS null
    --	and "IDLANREL" = 353431
    GROUP BY 
        "IDBANCODADOS", 
        "CODCOLIGADA", 
        "CODFILIAL",
     --   "CODCFO",
        "SEGUNDONUMERO",
        "IDLANREL"
)
UPDATE public.FT_LANCAMENTOS t
SET "TIPOREL" = 801,
	"VALORVINCULO_ACUM" = cte."VALORVINCULO_ACUM",
	"DATACONTABILIZBX" = cte."DATACONTABILIZBX",
	"ANOMES_CONTABILIZBX" = TO_CHAR(cte."DATACONTABILIZBX", 'YYYY-MM')
FROM cte_acumulado cte
WHERE t."TIPOREL" not in (401, 501, 601, 701)
	and t."IDBANCODADOS" = cte."IDBANCODADOS"
	and t."CODCOLIGADA" = cte."CODCOLIGADA"
	and t."CODFILIAL" = cte."CODFILIAL"
  --  AND t."CODCFO" = cte."CODCFO"
    AND t."SEGUNDONUMERO" = cte."SEGUNDONUMERO"
    AND t."IDLANREL" = cte."IDLANREL";  
   
   
  
   
   
   
-- Calcula NF em Aberto e emitidas
UPDATE public.FT_LANCAMENTOS
SET 
    "VALOR_NF_EM_ABERTO" = subquery."VALOR_NF_EM_ABERTO",
    "VALOR_NF_EMITIDAS" = subquery."VALOR_NF_EMITIDAS"
FROM (
    SELECT 
        "NUMERO_LINHA", 
        ("VALORLIQUIDO" - "VALORVINCULO_ACUM") AS "VALOR_NF_EM_ABERTO",
        "VALORVINCULO_ACUM" AS "VALOR_NF_EMITIDAS"
    FROM public.FT_LANCAMENTOS
) AS subquery
WHERE public.FT_LANCAMENTOS."NUMERO_LINHA" = subquery."NUMERO_LINHA";



TRUNCATE TABLE public."FT_COMPOSICAO";

INSERT INTO public."FT_COMPOSICAO" (
    "POSICAO",
    "ANO_MES",
    "DATA",
    "DATACONTABILIZBX",
    "DATA_ENTREGA_NF",
    "EMPRESA",
    "REDUZIDO",
    "CODCONTA",
    "IDPARTIDA",
    "ANTERIOR",
    "DEBITOS",
    "CREDITOS",
    "SALDO_ATUAL",
    "DESCRICAO_TRATADA",
    "CODFILIAL",
    "HISTORICO",
    "VALOR",
    "CODCOLIGADA",
    "CHAVE_EMPRESA_CONTA_MES",
    "PREPARADO",
    "DATA_PREPARADO",
    "CONFERIDO",
    "DATA_CONFERIDO",
    "PENDENCIA",
    "RESP_PENDENCIA",
    "DATA_PENDENCIA",
    "prioridade",
    "QTD_PREPARADO",
    "QTD_CONFERIDO",
    "QTD_PENDENCIA",
    "QTD_FINALIZADO",
    "STATUS_PREPARADO",
    "STATUS_CONFERIDO",
    "STATUS_PENDENCIA",
    "STATUS_FINALIZADO",
    "NUMERO_LINHA",
    "NOME_FORNECEDOR",
    "IDBAIXAREL",
    "TIPOREL",
    "TIPOBAIXA",
    "NUMCONTABILBX",
    "SEGUNDONUMERO",
    "VALORLIQUIDO",
    "VALORVINCULO",
    "VALORVINCULO_ACUM",
    "VALOR_NF_EM_ABERTO",
    "VALOR_NF_EMITIDAS",
    "DATAEXECUCAO",
    "IDBANCODADOS",
    "CODCFO",
    "NUMERODOCUMENTO"
) 
SELECT
    "POSICAO",
    "ANO_MES",
    "DATA",
    CAST('2000-01-01' AS DATE) as "DATACONTABILIZBX",
    CAST('2000-01-01' AS DATE) as "DATA_ENTREGA_NF",
    "EMPRESA",
    "REDUZIDO",
    "CODCONTA",
    "IDPARTIDA",
    "ANTERIOR",
    "DEBITOS",
    "CREDITOS",
    "SALDO_ATUAL",
    "DESCRICAO_TRATADA",
    "CODFILIAL",
    "HISTORICO",
    "VALOR",
    "CODCOLIGADA",
    "CHAVE_EMPRESA_CONTA_MES",
    "PREPARADO",
    "DATA_PREPARADO",
    "CONFERIDO",
    "DATA_CONFERIDO",
    "PENDENCIA",
    "RESP_PENDENCIA",
    "DATA_PENDENCIA",
    prioridade,
    "QTD_PREPARADO",
    "QTD_CONFERIDO",
    "QTD_PENDENCIA",
    "QTD_FINALIZADO",
    "STATUS_PREPARADO",
    "STATUS_CONFERIDO",
    "STATUS_PENDENCIA",
    "STATUS_FINALIZADO",
    "NUMERO_LINHA",
    cast('-' as VARCHAR(250)) as "NOME_FORNECEDOR",
    cast(0 as INT4) as "IDBAIXAREL",
    cast(0 as INT2) as "TIPOREL",
    cast(0 as INT4) as "TIPOBAIXA",
    cast('-' as VARCHAR(20)) as "NUMCONTABILBX",
    cast('-' as VARCHAR(20)) as "SEGUNDONUMERO",
    cast(0.000 as numeric(15,4)) as "VALORLIQUIDO",
    cast(0.000 as numeric(15,4)) as "VALORVINCULO",
    cast(0.000 as numeric(15,4)) as "VALORVINCULO_ACUM",
    cast(0.000 as numeric(15,4)) as "VALOR_NF_EM_ABERTO",
    cast(0.000 as numeric(15,4)) as "VALOR_NF_EMITIDAS",
    data_execucao as "DATAEXECUCAO",
    "IDBANCODADOS",
    substring("HISTORICO" FROM '^\d+(?=\s*-)') as "CODCFO",
    ltrim(substring("HISTORICO" FROM 'NF\.\s*(\d+)'), '0') as "NUMERODOCUMENTO"
FROM public."FT_RAZAO_DASH";




-- SOMENTE OS ADIANTAMENTOS DE FORNECEDORES SÃO INSERIDOS 
INSERT INTO public."FT_COMPOSICAO" (
    "POSICAO", 
    "ANO_MES", 
   	"DATA",
	"DATACONTABILIZBX",
	"DATA_ENTREGA_NF",
    "EMPRESA", 
    "REDUZIDO", 
    "CODCONTA", 
    "IDPARTIDA", 
    "ANTERIOR", 
    "DEBITOS", 
    "CREDITOS", 
    "SALDO_ATUAL", 
    "DESCRICAO_TRATADA", 
    "CODFILIAL", 
    "HISTORICO", 
    "VALOR", 
    "CODCOLIGADA", 
    "CHAVE_EMPRESA_CONTA_MES", 
    "PREPARADO", 
    "DATA_PREPARADO", 
    "CONFERIDO", 
    "DATA_CONFERIDO", 
    "PENDENCIA", 
    "RESP_PENDENCIA", 
    "DATA_PENDENCIA", 
    "prioridade", 
    "QTD_PREPARADO", 
    "QTD_CONFERIDO", 
    "QTD_PENDENCIA", 
    "QTD_FINALIZADO", 
    "STATUS_PREPARADO", 
    "STATUS_CONFERIDO", 
    "STATUS_PENDENCIA", 
    "STATUS_FINALIZADO", 
    "NUMERO_LINHA", 
    "NOME_FORNECEDOR", 
    "IDBAIXAREL", 
    "TIPOREL", 
    "TIPOBAIXA",
    "NUMCONTABILBX",
    "SEGUNDONUMERO",
    "VALORLIQUIDO", 
    "VALORVINCULO", 
    "VALORVINCULO_ACUM", 
    "VALOR_NF_EM_ABERTO", 
    "VALOR_NF_EMITIDAS",
	"DATAEXECUCAO",
    "IDBANCODADOS",
    "CODCFO",
    "NUMERODOCUMENTO"
)
SELECT 
    101 as "POSICAO",  -- Adiantamento de fornecedor
    ano_mes_anterior  as  "ANO_MES",
    primeiro_dia_mes_anterior as "DATA",
    B."DATABAIXA",
    B."DATACONTABILIZBX" as "DATA_ENTREGA_NF",
    B."EMPRESA", 
    '-' as "REDUZIDO", 
    B."CODCONTA", 
    '-' as "IDPARTIDA", 
    0.0000 as "ANTERIOR", 
    0.0000 as "DEBITOS", 
    0.0000 as "CREDITOS", 
    0.0000 as "SALDO_ATUAL", 
    B."DESCRICAO", 
    B."CODFILIAL", 
    '-' as "HISTORICO", 
    0 as "VALOR", 
    B."CODCOLIGADA", 
    '-' as "CHAVE_EMPRESA_CONTA_MES", 
    '-' as "PREPARADO", 
    '-' as "DATA_PREPARADO", 
    '-' as "CONFERIDO", 
    '-' as "DATA_CONFERIDO", 
    '-' as "PENDENCIA", 
    '-' as "RESP_PENDENCIA", 
    '-' as "DATA_PENDENCIA", 
    '-' as "prioridade", 
    0  as "QTD_PREPARADO", 
    0 as "QTD_CONFERIDO", 
    0 as "QTD_PENDENCIA", 
    0 as "QTD_FINALIZADO", 
    'NÃO' as "STATUS_PREPARADO", 
    'NÃO' as "STATUS_CONFERIDO", 
    'NÃO' as "STATUS_PENDENCIA", 
    'NÃO' as "STATUS_FINALIZADO", 
    99999999 as "NUMERO_LINHA", 
    B."NOME" as "NOME_FORNECEDOR", 
    B."IDBAIXAREL", 
    B."TIPOREL", 
    null as "TIPOBAIXA",
    B."NUMERODOCUMENTO" as  "NUMCONTABILBX",
    B."SEGUNDONUMERO",
    B."VALORLIQUIDO", 
    B."VALORVINCULO", 
    B."VALORVINCULO_ACUM", 
    B."VALOR_NF_EM_ABERTO", 
    B."VALOR_NF_EMITIDAS",
	data_execucao as "DATAEXECUCAO",
    B."IDBANCODADOS",
    B."CODCFO",
    B."NUMERODOCUMENTO"
FROM public.FT_LANCAMENTOS  B
where  
--B."VALOR_NF_EM_ABERTO" > 0 
TRIM(UPPER("DESCRICAO")) in 
('ADIANTAMENTO A FORNECEDORES',
	'ADIANTAMENTO DE FORNECEDORES',
	'ADIANTAMENTO DIVERSOS',
	'ADIANTAMENTOS DIVERSOS',
	'ADIANTAMENTO DE CLIENTES',
	'ADIANTAMENTO DE CLIENTES VILLA SUIÇA');
----------------------------------------------------- FIM ADIANTAMENTO DE FORNECEDORES ------------------------------
------------------------------------------------------- REGRA 1 ADIANTAMENTO DE FORNECEDORES ------------------------------------------------------------------------ 



-------------------------------X------------X-------------------X----------------X-------------X-XXX---------------------------------------------------------------





------------------------------------------------------- REGRA 2 EMISSAO DE NF À FORNECEDORES E PAGAMENTOS ------------------------------------------------------------------------ 
-- A NF NASCE EM UM LANÇAMENTO DE CREDITO
-- E a partir da FLAN REGRAS: 
--					1) que tenha "DATAEMISSAO" <= MÊS ANTERIOR 
--					2) DATABAIXA NULA
--					3) DATACANCELAMENTO NULA

-- Exemplo
-- 11393 - NF.0001551 HIDROTAM COMERCIO DE TUBOS E CONEXÕES LTDA
-- Estou tentando pegar as NF abertas pela CLANCA
-- SWISS PARK NEGÓCIOS IMOBILIÁRIOS LTDA
-- 2.1.1.01.01.0001
-- 4
-- 12
DROP TABLE IF EXISTS public."FT_NF_EMITIDAS";
CREATE TABLE public."FT_NF_EMITIDAS" AS 
		select   row_number() OVER() as "NUMERO_LINHA",
				 UPPER(TRIM(E."NOME")) AS "EMPRESA",
				 D."NOME"  as "NOME_FORNECEDOR",
				 F."DOCUMENTO",
				 regexp_replace(split_part("DOCUMENTO", '/', 1),'^0+', '') as "DOCUMENTO_TRATADO",
				 substring("COMPLEMENTO" FROM '^\d+(?=\s*-)') as "CODCFO",
				 F."DATA" as "DATAEMISSAO_NF",
	     		 F."IDBANCODADOS",
				 F."CODCOLIGADA",
				 F."CODFILIAL",
				 F."CREDITO" as "CODCONTA",
				 CAST(null as VARCHAR(250)) as "DESCRICAO",
				 F."IDPARTIDA",
				 F."LCTREF" ,
				 F."VALOR" as "VALOR_NF_EMITIDAS",
				 "VALOR" as "VALOR_NF_EM_ABERTO",
				 cast(0 as numeric(15,4)) as "VALORVINCULO_ACUM",
				 F."COMPLEMENTO" ,
				 F."CODCCUSTO" ,
				 F."CODLOTE" ,
				 F."IDLANCAMENTO" 
		from "CLANCA" F
		LEFT JOIN public."GFILIAL" E
		    ON E."IDBANCODADOS" = F."IDBANCODADOS"
		    AND E."CODCOLIGADA" = F."CODCOLIGADA"
		    AND E."CODFILIAL" = F."CODFILIAL"
		LEFT JOIN public."FCFO" D
		    ON D."CODCFO" = substring(F."COMPLEMENTO" FROM '^\d+(?=\s*-)')
		    AND D."IDBANCODADOS" = F."IDBANCODADOS"
		    AND D."CODCOLIGADA" = F."CODCOLIGADA"
		where  F."DATA" <= data_mes_anterior
			-- F."DATA" <= 	DATE '2024-12-31'
			and "CREDITO" is not null
			and upper("COMPLEMENTO") not like '%PAGAM%'  
			and upper("COMPLEMENTO") not like '%S/NF%' 
			and upper("COMPLEMENTO") not like '%S/ NF%'
			and upper("COMPLEMENTO")  like '%NF%';
	
	
/*
CREATE TABLE public."FT_NF_EMITIDAS" AS 
		select   row_number() OVER() as "NUMERO_LINHA",
				 UPPER(TRIM(E."NOME")) AS "EMPRESA",
				 D."NOME"  as "NOME_FORNECEDOR",
				 F."NUMEROMOV",
				 regexp_replace(split_part("NUMEROMOV", '/', 1),'^0+', '') as "DOCUMENTO_TRATADO",
				 F."CODCFO",
				 F."DATAEMISSAO" as "DATAEMISSAO_NF",
	     		 F."IDBANCODADOS",
				 F."CODCOLIGADA",
				 F."CODFILIAL",
				 CAST(null as VARCHAR(40)) as "CODCONTA",
				 CAST(null as VARCHAR(250)) as "DESCRICAO",
				 cast(null as INT4) as "IDPARTIDA",
				 cast(null as INT4) as  "LCTREF" ,
				 F."VALORLIQUIDO" as "VALOR_NF_EMITIDAS",
				 F."VALORLIQUIDO" as "VALOR_NF_EM_ABERTO",
				 cast(0 as numeric(15,4)) as "VALORVINCULO_ACUM",
				 cast(null as INT4) as "COMPLEMENTO" ,
				 F."CODCCUSTO" ,
				 cast(null as INT4) as "CODLOTE" ,
				 cast(null as INT4) as "IDLANCAMENTO" 
		from "TMOV" F
		LEFT JOIN public."GFILIAL" E
		    ON E."IDBANCODADOS" = F."IDBANCODADOS"
		    AND E."CODCOLIGADA" = F."CODCOLIGADA"
		    AND E."CODFILIAL" = F."CODFILIAL"
	    LEFT JOIN public."FCFO" D
		    ON D."CODCFO" = F."CODCFO"
		    AND D."IDBANCODADOS" = F."IDBANCODADOS"
		    AND D."CODCOLIGADA" = F."CODCOLIGADA"
		where  	--F."DATA" <= data_mes_anterior;
		       F."DATA" <= 	DATE '2024-12-31';
	
*/	
	
	
	

DROP TABLE IF EXISTS public."FT_NF_PAGAMENTOS";
CREATE TABLE public."FT_NF_PAGAMENTOS" AS
select 	 	F."IDBANCODADOS",
		 	F."CODCOLIGADA",
			F."CODFILIAL",
			F."CODCFO" ,
			F."CODTDO" ,
			TRIM(UPPER(T."DESCRICAO")) as "CODTDO_DESCRICAO",
			F."IDLAN" ,
			-- F."IDBAIXA" ,
			-- F."DATACANCELBAIXA",
			F."NUMERODOCUMENTO" ,
			ltrim(split_part(M."NUMEROMOV", '/', 1), '0') as "DOCUMENTO_TRATADO",
			F."SEGUNDONUMERO" ,
			F."IDMOV",
			F."VALORORIGINAL" ,
			F."VALORBAIXADO" ,
			F."VALORLIQUIDO" ,
			F."VALORDESCONTO" ,
			F."DATAVENCIMENTO" ,
			F."DATAEMISSAO" ,
			F."DATABAIXA" ,
			F."DATACANCELAMENTO" ,
			F."DATAPAG" ,
			F."STATUSLAN" ,
			F."HISTORICO" ,
			F."CODCCUSTO" 			
from "FLAN" F
LEFT JOIN public."TMOV" M
    ON  M."IDMOV" = F."IDMOV" 
    AND M."IDBANCODADOS" = F."IDBANCODADOS"
    AND M."CODCOLIGADA" = F."CODCOLIGADA"
    AND M."CODFILIAL" = F."CODFILIAL"      
LEFT JOIN public."FTDO" T
    ON T."IDBANCODADOS" = F."IDBANCODADOS"
    AND T."CODCOLIGADA" = F."CODCOLIGADA"
    AND T."CODTDO" = F."CODTDO"
LEFT JOIN public."FCFO" D
    ON D."CODCFO" 		= F."CODCFO"
    AND D."IDBANCODADOS" = F."IDBANCODADOS"
    AND D."CODCOLIGADA" = F."CODCOLIGADA"
LEFT JOIN public."GFILIAL" E
    ON E."IDBANCODADOS" = F."IDBANCODADOS"
    AND E."CODCOLIGADA" = F."CODCOLIGADA"
    AND E."CODFILIAL" = F."CODFILIAL"
where 
  F."DATACANCELAMENTO"  is null
	and F."DATABAIXA" IS NOT null
	--and F."DATABAIXA" <= DATE '2024-12-31';
	and F."DATABAIXA"  <= data_mes_anterior;





-- Ajuste para o pagamento do financiamento do veiculo fornecedor 11127
drop table 	if exists temp_pagamento_diferenca;
	
CREATE TEMP TABLE temp_pagamento_diferenca AS
SELECT * 
FROM public."FT_NF_PAGAMENTOS"
WHERE "CODCFO" = '11127'
and "NUMERODOCUMENTO" in ('02551/01/3','02551/01/4','02551/01/5')
LIMIT 3;


update temp_pagamento_diferenca
set "NUMERODOCUMENTO" = '02551/01/23'
where "NUMERODOCUMENTO" = '02551/01/3';

update temp_pagamento_diferenca
set "NUMERODOCUMENTO" = '02551/01/24'
where "NUMERODOCUMENTO" = '02551/01/4';

update temp_pagamento_diferenca
set "NUMERODOCUMENTO" = '02551/01/25'
where "NUMERODOCUMENTO" = '02551/01/5';




insert into public."FT_NF_PAGAMENTOS"
select * from  temp_pagamento_diferenca ;




------------------- Usa a Flan para carimbar os valores pagos -----------------------------		
WITH cte_acumulado AS (
    SELECT 
        "IDBANCODADOS", 
        "CODCOLIGADA",
        "CODFILIAL",
        "CODCFO",
        "DOCUMENTO_TRATADO",
        SUM("VALORORIGINAL") as "VALORVINCULO_ACUM"
    FROM public."FT_NF_PAGAMENTOS" 
    where  "DATABAIXA" is not null 
   		and "DATABAIXA"  <= data_mes_anterior
   	--	and "DATABAIXA"  <=	DATE '2024-12-31'
    GROUP BY 
        "IDBANCODADOS", 
        "CODCOLIGADA",
        "CODFILIAL",
        "CODCFO",
        "DOCUMENTO_TRATADO"
)
UPDATE public."FT_NF_EMITIDAS" t
SET 	"VALORVINCULO_ACUM" = cte."VALORVINCULO_ACUM"
FROM cte_acumulado cte
WHERE t."IDBANCODADOS" = cte."IDBANCODADOS"
	and t."CODCOLIGADA" = cte."CODCOLIGADA"
	and t."CODFILIAL" = cte."CODFILIAL"
    AND t."CODCFO" = cte."CODCFO"
    AND t."DOCUMENTO_TRATADO" = cte."DOCUMENTO_TRATADO";

-- ajustes no valor da NF
UPDATE public."FT_NF_EMITIDAS" t
SET "VALOR_NF_EM_ABERTO" = "VALOR_NF_EMITIDAS"
WHERE  "VALOR_NF_EM_ABERTO" is null or  "VALOR_NF_EM_ABERTO" = 0;


UPDATE public."FT_NF_EMITIDAS" t
SET "VALORVINCULO_ACUM" = 0
WHERE  "VALORVINCULO_ACUM" is null;
   
   
UPDATE public."FT_NF_EMITIDAS" t
SET "VALOR_NF_EM_ABERTO" = t."VALOR_NF_EMITIDAS" - t."VALORVINCULO_ACUM"
FROM (
     SELECT "NUMERO_LINHA", "VALOR_NF_EMITIDAS", "VALORVINCULO_ACUM"
    FROM public."FT_NF_EMITIDAS"
) AS cte
WHERE t."NUMERO_LINHA" = cte."NUMERO_LINHA"
	and t."VALORVINCULO_ACUM" > 0;



-- CARIMBA DESCRICAO		
UPDATE public."FT_NF_EMITIDAS"
  SET 	"DESCRICAO" 	= subquery."DESCRICAO"
FROM (  
		select "IDBANCODADOS", "CODCOLIGADA", "CODCONTA", "REDUZIDO", TRIM(UPPER("DESCRICAO")) as "DESCRICAO"
		FROM public."CCONTA"
	  ) 
	    AS subquery
WHERE 	public."FT_NF_EMITIDAS"."IDBANCODADOS" =  subquery."IDBANCODADOS" 	AND
		public."FT_NF_EMITIDAS"."CODCOLIGADA" =  subquery."CODCOLIGADA"		AND
		public."FT_NF_EMITIDAS"."CODCONTA" =  subquery."CODCONTA";
		


/*
UPDATE public."FT_NF_EMITIDAS"
  SET 	"DESCRICAO"	= subquery."DESCRICAO_TRATADA"	
FROM (  
		SELECT *
		FROM public."FT_RAZAO_DASH" 
		where  UPPER(TRIM("DESCRICAO_TRATADA")) like 'ADIANTAMENTO%' or UPPER(TRIM("DESCRICAO_TRATADA")) like 'FORNECEDO%'
	  ) 
	    AS subquery
WHERE 	public."FT_NF_EMITIDAS"."EMPRESA" =  subquery."EMPRESA" 		and
		public."FT_NF_EMITIDAS"."CODCONTA" =  subquery."CODCONTA" 		and 
		UPPER(TRIM(subquery."DESCRICAO_TRATADA")) like 'ADIANTAMENTO%';
*/




INSERT INTO public."FT_COMPOSICAO" (
    "POSICAO", 
    "ANO_MES", 
   	"DATA",
	"DATACONTABILIZBX",
	"DATA_ENTREGA_NF",
    "EMPRESA", 
    "REDUZIDO", 
    "CODCONTA", 
    "IDPARTIDA", 
    "ANTERIOR", 
    "DEBITOS", 
    "CREDITOS", 
    "SALDO_ATUAL", 
    "DESCRICAO_TRATADA", 
    "CODFILIAL", 
    "HISTORICO", 
    "VALOR", 
    "CODCOLIGADA", 
    "CHAVE_EMPRESA_CONTA_MES", 
    "PREPARADO", 
    "DATA_PREPARADO", 
    "CONFERIDO", 
    "DATA_CONFERIDO", 
    "PENDENCIA", 
    "RESP_PENDENCIA", 
    "DATA_PENDENCIA", 
    "prioridade", 
    "QTD_PREPARADO", 
    "QTD_CONFERIDO", 
    "QTD_PENDENCIA", 
    "QTD_FINALIZADO", 
    "STATUS_PREPARADO", 
    "STATUS_CONFERIDO", 
    "STATUS_PENDENCIA", 
    "STATUS_FINALIZADO", 
    "NUMERO_LINHA", 
    "NOME_FORNECEDOR", 
    "IDBAIXAREL", 
    "TIPOREL", 
    "TIPOBAIXA",
    "NUMCONTABILBX",
    "SEGUNDONUMERO",
    "VALORLIQUIDO", 
    "VALORVINCULO", 
    "VALORVINCULO_ACUM", 
    "VALOR_NF_EM_ABERTO", 
    "VALOR_NF_EMITIDAS",
	"DATAEXECUCAO",
    "IDBANCODADOS",
    "CODCFO",
    "NUMERODOCUMENTO"
)
SELECT 
    101 as "POSICAO",  -- Adiantamento de fornecedor
    ano_mes_anterior  as  "ANO_MES",    
    primeiro_dia_mes_anterior as "DATA",
   -- '2024-11'  as  "ANO_MES",
   -- DATE '2024-12-31' as "DATA",
    B."DATAEMISSAO_NF" as "DATABAIXA",
    B."DATAEMISSAO_NF" as "DATA_ENTREGA_NF",
    B."EMPRESA", 
    '-' as "REDUZIDO", 
    B."CODCONTA", 
    '-' as "IDPARTIDA", 
    0.0000 as "ANTERIOR", 
    0.0000 as "DEBITOS", 
    0.0000 as "CREDITOS", 
    0.0000 as "SALDO_ATUAL", 
    B."DESCRICAO", 
    B."CODFILIAL", 
    '-' as "HISTORICO", 
    0 as "VALOR", 
    B."CODCOLIGADA", 
    '-' as "CHAVE_EMPRESA_CONTA_MES", 
    '-' as "PREPARADO", 
    '-' as "DATA_PREPARADO", 
    '-' as "CONFERIDO", 
    '-' as "DATA_CONFERIDO", 
    '-' as "PENDENCIA", 
    '-' as "RESP_PENDENCIA", 
    '-' as "DATA_PENDENCIA", 
    '-' as "prioridade", 
    0  as "QTD_PREPARADO", 
    0 as "QTD_CONFERIDO", 
    0 as "QTD_PENDENCIA", 
    0 as "QTD_FINALIZADO", 
    'NÃO' as "STATUS_PREPARADO", 
    'NÃO' as "STATUS_CONFERIDO", 
    'NÃO' as "STATUS_PENDENCIA", 
    'NÃO' as "STATUS_FINALIZADO", 
    99999999 as "NUMERO_LINHA", 
    B."NOME_FORNECEDOR", 
    0  as "IDBAIXAREL", 
    0  as "TIPOREL", 
    null as "TIPOBAIXA",
    B."DOCUMENTO" as  "NUMCONTABILBX",
    B."DOCUMENTO_TRATADO" as "SEGUNDONUMERO",
    B."VALOR_NF_EMITIDAS" as "VALORLIQUIDO", 
    B."VALORVINCULO_ACUM" as "VALORVINCULO", 
    B."VALORVINCULO_ACUM", 
    B."VALOR_NF_EM_ABERTO", 
    B."VALOR_NF_EMITIDAS",
	data_execucao as "DATAEXECUCAO",
    B."IDBANCODADOS",
    B."CODCFO",
    B."DOCUMENTO" as "NUMERODOCUMENTO"
FROM public."FT_NF_EMITIDAS"  B
where  
--B."VALOR_NF_EM_ABERTO" > 0
 "DESCRICAO" in ('FORNECEDORES DE MATERIAIS','FORNECEDORES DE SERVIÇOS') ;

-------------------------------------------------------- Fornecedores Diversos ----------------------------------------
------------------------------------------------------- REGRA 2 EMISSAO DE NF À FORNECEDORES E PAGAMENTOS ------------------------------------------------------------------------ 


----------------------------------------------------- NF EMITIDAS A PAGAR --------------------------------------------

	 
-- Carimba Reduzido	 
UPDATE public."FT_COMPOSICAO"
SET "REDUZIDO" = subquery."REDUZIDO"
FROM (
    SELECT DISTINCT "REDUZIDO" , "CODCONTA" , "EMPRESA" , "CODFILIAL" 
    FROM PUBLIC."FT_RAZAO" 
) AS subquery
where public."FT_COMPOSICAO"."EMPRESA" = subquery."EMPRESA"
    and public."FT_COMPOSICAO"."CODCONTA" = subquery."CODCONTA"
    AND public."FT_COMPOSICAO"."CODFILIAL" = subquery."CODFILIAL"
    and ( 
   			public."FT_COMPOSICAO"."REDUZIDO" is null or  public."FT_COMPOSICAO"."REDUZIDO" = '-'
   		);

 UPDATE public."FT_COMPOSICAO"
SET "NOME_FORNECEDOR" = subquery."NOME_FORNECEDOR"
FROM (
    SELECT "CODCFO", trim(upper("NOME")) AS "NOME_FORNECEDOR"
    FROM public."FCFO"
) AS subquery
WHERE public."FT_COMPOSICAO"."CODCFO" = subquery."CODCFO"
  AND (public."FT_COMPOSICAO"."NOME_FORNECEDOR" IS NULL 
       OR public."FT_COMPOSICAO"."NOME_FORNECEDOR" = '-');

   
END;
$function$
;
