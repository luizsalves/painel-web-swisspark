CREATE OR REPLACE FUNCTION public.fc_nf_emitidas()
 RETURNS void
 LANGUAGE plpgsql
AS $function$

BEGIN

DROP TABLE IF EXISTS public."FT_PARCELAS_NF";
CREATE TABLE public."FT_PARCELAS_NF" AS 
SELECT  row_number() OVER() as "NUMERO_LINHA",
        'PARCELA' as "TIPO_ITEM",
        cast('EM ABERTO' as VARCHAR(20)) as "STATUS_PAGAMENTO",
        UPPER(E."NOME") as "EMPRESA",
        --cast(null  as text),
        CAST(null as VARCHAR(40)) as "CODCONTA",
        UPPER(D."NOME")  as "NOME_FORNECEDOR",
        --cast(null as varchar(250))  as "NOME_FORNECEDOR",
        M."CODCFO",
        M."IDBANCODADOS",
        M."CODCOLIGADA",
        M."CODFILIAL",
        M."NUMEROMOV" as "NUMERO_NF",
        M."DATAEMISSAO" as "DATAEMISSAO_NF",
        M."DATASAIDA" as "DATASAIDA_NF",
        M."CODCCUSTO",
        TRIM(UPPER(T."DESCRICAO")) as "CODTDO_DESCRICAO",
       -- cast(null as text) as "CODTDO_DESCRICAO",
        ltrim(split_part(M."NUMEROMOV", '/', 1), '0') as "NUMERO_NF_TRATADO",
        F."NUMERODOCUMENTO" as "NUMERO_PARC",
        F."IDLAN",
        F."SEGUNDONUMERO",
        F."IDMOV",
        CAST(0 as NUMERIC(15,4)) as "VALORBRUTO_NF",
        CAST(0 as NUMERIC(15,4)) as "VALORLIQUIDO_NF",
        F."VALORORIGINAL" as "VALOR_PARC",
         CAST(0 as NUMERIC(15,4)) as "VALOR_PAGO",
         CAST(0 as NUMERIC(15,4)) as "VALOR_APAGAR",
        F."VALORORIGINAL" as "VALOR",
        F."VALORDESCONTO" as "VALORDESCONTO_PARC",       
        F."DATAVENCIMENTO" as "DATAVENCIMENTO_PARC",
        F."DATABAIXA" as "DATABAIXA_PARC",
        F."DATACANCELAMENTO" as "DATACANCELAMENTO_PARC",
        F."DATAPAG" as "DATAPAG_PARC",
        F."STATUSLAN",
        F."HISTORICO"
FROM public."TMOV" M
LEFT JOIN "FLAN" F ON M."IDMOV" = F."IDMOV" AND M."IDBANCODADOS" = F."IDBANCODADOS" AND M."CODCOLIGADA" = F."CODCOLIGADA" AND M."CODFILIAL" = F."CODFILIAL"
LEFT JOIN public."FTDO" T ON T."IDBANCODADOS" = F."IDBANCODADOS" AND T."CODCOLIGADA" = F."CODCOLIGADA" AND T."CODTDO" = F."CODTDO"
LEFT JOIN public."FCFO" D ON D."CODCFO" = M."CODCFO" AND D."IDBANCODADOS" = M."IDBANCODADOS" and d."CODCOLIGADA" = F."CODCOLIGADA"
LEFT JOIN public."GFILIAL" E ON E."IDBANCODADOS" = M."IDBANCODADOS" AND E."CODCOLIGADA" = M."CODCOLIGADA" AND E."CODFILIAL" = M."CODFILIAL"
WHERE F."DATACANCELAMENTO" IS null;


-- As parcelas nascem no DÉBITO
UPDATE public."FT_PARCELAS_NF"
  SET 	"CODCONTA" 	= subquery."DEBITO"
FROM (  
		SELECT 	 regexp_replace(split_part("DOCUMENTO", '/', 1),'^0+', '') as "DOCUMENTO_TRATADO", 
				 substring("COMPLEMENTO" FROM '^\d+(?=\s*-)') as "CODCFO",
				 "IDBANCODADOS",
				 "CODCOLIGADA",
				 "CODFILIAL",
				 "DEBITO"
		FROM public."CLANCA" 
		where  "DEBITO" is not NULL
		and upper("COMPLEMENTO")  like '%PAGAM%'  
		and upper("COMPLEMENTO") not like '%S/NF%' 
		and upper("COMPLEMENTO") not like '%S/ NF%'
		and upper("COMPLEMENTO")  like '%NF%'
	  ) 
	    AS subquery
WHERE 	public."FT_PARCELAS_NF"."IDBANCODADOS" =  subquery."IDBANCODADOS" 	AND
		public."FT_PARCELAS_NF"."CODCOLIGADA" =  subquery."CODCOLIGADA"		AND	
		public."FT_PARCELAS_NF"."CODFILIAL" =  subquery."CODFILIAL"		AND	
		public."FT_PARCELAS_NF"."NUMERO_NF_TRATADO" = subquery."DOCUMENTO_TRATADO" 	AND
		public."FT_PARCELAS_NF"."CODCFO" =  subquery."CODCFO";	
		

DROP TABLE IF EXISTS public."FT_NOTAS_FISCAIS";
CREATE TABLE public."FT_NOTAS_FISCAIS" AS 
SELECT  row_number() OVER() as "NUMERO_LINHA",
        'NF' as "TIPO_ITEM",
        cast('EM ABERTO' as VARCHAR(20)) as "STATUS_PAGAMENTO",
        UPPER(E."NOME") as "EMPRESA",
        CAST(null as VARCHAR(40)) as "CODCONTA",
        D."NOME" as "NOME_FORNECEDOR",
        M."CODCFO",
        M."IDBANCODADOS",
        M."CODCOLIGADA",
        M."CODFILIAL",
        M."NUMEROMOV" as "NUMERO_NF",
        M."DATAEMISSAO" as "DATAEMISSAO_NF",
        M."DATASAIDA" as "DATASAIDA_NF",
        M."CODCCUSTO",
        CAST(null as VARCHAR(40)) as "CODTDO_DESCRICAO",
        ltrim(split_part(M."NUMEROMOV", '/', 1), '0') as "NUMERO_NF_TRATADO",
        CAST(null as VARCHAR(40)) as "NUMERO_PARC",
        CAST(0 as INT4) as "IDLAN",
        CAST(null as VARCHAR(20)) as "SEGUNDONUMERO",
        M."IDMOV",
        M."VALORBRUTO" as "VALORBRUTO_NF",
        M."VALORLIQUIDO" as "VALORLIQUIDO_NF",
        CAST(0 as NUMERIC(15,4)) as "VALOR_PARC",
        CAST(0 as NUMERIC(15,4)) as "VALOR_PAGO",
        CAST(0 as NUMERIC(15,4)) as "VALOR_APAGAR",
        M."VALORLIQUIDO" as "VALOR",
        CAST(0 as NUMERIC(15,4)) as "VALORDESCONTO_PARC",        
        CAST(null as DATE) as "DATAVENCIMENTO_PARC",
        CAST(null as DATE) as "DATABAIXA_PARC",
        CAST(null as DATE) as "DATACANCELAMENTO_PARC",
        CAST(null as DATE) as "DATAPAG_PARC",
        CAST(null as INT4) as "STATUSLAN",
        CAST(null as VARCHAR(255)) as "HISTORICO"
FROM public."TMOV" M
LEFT JOIN public."FCFO" D ON D."CODCFO" = M."CODCFO" AND D."IDBANCODADOS" = M."IDBANCODADOS" and D."CODCOLIGADA" = M."CODCOLIGADA"
LEFT JOIN public."GFILIAL" E ON E."IDBANCODADOS" = M."IDBANCODADOS" AND E."CODCOLIGADA" = M."CODCOLIGADA" AND E."CODFILIAL" = M."CODFILIAL";


-- As parcelas nascem no DÉBITO
UPDATE public."FT_NOTAS_FISCAIS"
  SET 	"CODCONTA" 	= subquery."CREDITO"
FROM (  
		SELECT 	regexp_replace(split_part("DOCUMENTO", '/', 1),'^0+', '') as "DOCUMENTO_TRATADO", 
				 substring("COMPLEMENTO" FROM '^\d+(?=\s*-)') as "CODCFO",
				 "IDBANCODADOS",
				 "CODCOLIGADA",
				 "CODFILIAL",
				 "CREDITO"
		FROM public."CLANCA" 
		where  "CREDITO" is not NULL
		and upper("COMPLEMENTO") not like '%PAGAM%'  
		and upper("COMPLEMENTO") not like '%S/NF%' 
		and upper("COMPLEMENTO") not like '%S/ NF%'
		and upper("COMPLEMENTO")  like '%NF%'
	  ) 
	    AS subquery
WHERE 	public."FT_NOTAS_FISCAIS"."IDBANCODADOS" =  subquery."IDBANCODADOS" 	AND
		public."FT_NOTAS_FISCAIS"."CODCOLIGADA" =  subquery."CODCOLIGADA"		AND	
		public."FT_NOTAS_FISCAIS"."CODFILIAL" =  subquery."CODFILIAL"		AND	
		public."FT_NOTAS_FISCAIS"."NUMERO_NF_TRATADO" =  subquery."DOCUMENTO_TRATADO"	AND
		public."FT_NOTAS_FISCAIS"."CODCFO" =  subquery."CODCFO";	


UPDATE public."FT_NOTAS_FISCAIS" N
SET "VALOR_PAGO" = subquery."VALOR_PARC"
FROM (
    SELECT 
        SUM("VALOR_PARC") AS "VALOR_PARC",
        "CODCFO",
        "IDMOV",
        "IDBANCODADOS",
        "CODCOLIGADA",
        "CODFILIAL"
    FROM public."FT_PARCELAS_NF"
    GROUP BY 
        "CODCFO",
        "IDMOV",
        "IDBANCODADOS",
        "CODCOLIGADA",
        "CODFILIAL"
) AS subquery
WHERE 
    N."CODCFO" = subquery."CODCFO" AND
    N."IDMOV" = subquery."IDMOV" AND
    N."IDBANCODADOS" = subquery."IDBANCODADOS" AND
    N."CODCOLIGADA" = subquery."CODCOLIGADA" AND
    N."CODFILIAL" = subquery."CODFILIAL";

   
   
UPDATE public."FT_NOTAS_FISCAIS" N
SET "VALOR_APAGAR" = subquery."VALOR_APAGAR"
FROM (
    SELECT DISTINCT  "VALORLIQUIDO_NF" - "VALOR_PAGO" AS "VALOR_APAGAR",
    				"VALORBRUTO_NF",
    				"VALOR_PAGO",
        	"NUMERO_LINHA"
    FROM public."FT_NOTAS_FISCAIS"

) AS subquery
WHERE 
    N."NUMERO_LINHA" = subquery."NUMERO_LINHA";
	

DROP TABLE IF EXISTS public."FT_NF_EMITIDAS_VS_PARC";
CREATE TABLE public."FT_NF_EMITIDAS_VS_PARC" AS 	
select * from public."FT_NOTAS_FISCAIS"
union 
select * from public."FT_PARCELAS_NF";



UPDATE public."FT_NF_EMITIDAS_VS_PARC"
SET "NOME_FORNECEDOR" = "NOME"
FROM (
    SELECT DISTINCT  "IDBANCODADOS", "CODCFO", "NOME"
    FROM public."FCFO"

) AS subquery
WHERE 
    public."FT_NF_EMITIDAS_VS_PARC"."IDBANCODADOS" = subquery."IDBANCODADOS" and
    public."FT_NF_EMITIDAS_VS_PARC"."CODCFO" = subquery."CODCFO" AND
    public."FT_NF_EMITIDAS_VS_PARC"."NOME_FORNECEDOR" is NULL;
   
   
   

UPDATE public."FT_NF_EMITIDAS_VS_PARC" N
SET "STATUS_PAGAMENTO" = 'PAGO'
WHERE "VALOR_APAGAR" = 0;
	



-- As parcelas nascem no DÉBITO
UPDATE public."FT_NF_EMITIDAS_VS_PARC"
  SET 	"CODCONTA" 	= subquery."DEBITO"
FROM (  
		SELECT 	 regexp_replace(split_part("DOCUMENTO", '/', 1),'^0+', '') as "DOCUMENTO_TRATADO", 
				-- substring("COMPLEMENTO" FROM '^\d+(?=\s*-)') as "CODCFO",
				 TRIM(substring("COMPLEMENTO" FROM '^[^-]+')) as "NOME_FORNECEDOR",
				 "IDBANCODADOS",
				 "CODCOLIGADA",
				 "CODFILIAL",
				 "DEBITO"
		FROM public."CLANCA" 
		where  "DEBITO" is not NULL
		and upper("COMPLEMENTO")  like '%PAGAM%'  
		and upper("COMPLEMENTO") not like '%S/NF%' 
		and upper("COMPLEMENTO") not like '%S/ NF%'
		and upper("COMPLEMENTO")  like '%NF%'
	  ) 
	    AS subquery
WHERE 	public."FT_NF_EMITIDAS_VS_PARC"."IDBANCODADOS" =  subquery."IDBANCODADOS" 	AND
		public."FT_NF_EMITIDAS_VS_PARC"."CODCOLIGADA" =  subquery."CODCOLIGADA"		AND	
		public."FT_NF_EMITIDAS_VS_PARC"."CODFILIAL" =  subquery."CODFILIAL"		AND	
		public."FT_NF_EMITIDAS_VS_PARC"."NUMERO_NF_TRATADO" = subquery."DOCUMENTO_TRATADO" 	AND
		public."FT_NF_EMITIDAS_VS_PARC"."NOME_FORNECEDOR" =  subquery."NOME_FORNECEDOR" AND
		public."FT_NF_EMITIDAS_VS_PARC"."CODCONTA" is null;

	
	
UPDATE public."FT_NF_EMITIDAS_VS_PARC"
  SET 	"CODCONTA" 	= subquery."CREDITO"
FROM (  
		SELECT 	 regexp_replace(split_part("DOCUMENTO", '/', 1),'^0+', '') as "DOCUMENTO_TRATADO", 
				-- substring("COMPLEMENTO" FROM '^\d+(?=\s*-)') as "CODCFO",
				 TRIM(substring("COMPLEMENTO" FROM '^[^-]+')) as "NOME_FORNECEDOR",
				 "IDBANCODADOS",
				 "CODCOLIGADA",
				 "CODFILIAL",
				 "CREDITO"
		FROM public."CLANCA" 
		where  "CREDITO" is not NULL
		and upper("COMPLEMENTO") not like '%PAGAM%'  
		and upper("COMPLEMENTO") not like '%S/NF%' 
		and upper("COMPLEMENTO") not like '%S/ NF%'
		and upper("COMPLEMENTO")  like '%NF%'
	  ) 
	    AS subquery
WHERE 	public."FT_NF_EMITIDAS_VS_PARC"."IDBANCODADOS" =  subquery."IDBANCODADOS" 	AND
		public."FT_NF_EMITIDAS_VS_PARC"."CODCOLIGADA" =  subquery."CODCOLIGADA"		AND	
		public."FT_NF_EMITIDAS_VS_PARC"."CODFILIAL" =  subquery."CODFILIAL"		AND	
		public."FT_NF_EMITIDAS_VS_PARC"."NUMERO_NF_TRATADO" = subquery."DOCUMENTO_TRATADO" 	AND
		public."FT_NF_EMITIDAS_VS_PARC"."NOME_FORNECEDOR" =  subquery."NOME_FORNECEDOR" AND
		public."FT_NF_EMITIDAS_VS_PARC"."CODCONTA" is null;
	


UPDATE public."FT_NOTAS_FISCAIS"
  SET 	"CODCONTA" 	= subquery."CREDITO"
FROM (  
		SELECT 	--regexp_replace(split_part("DOCUMENTO", '/', 1),'^0+', '') as "DOCUMENTO_TRATADO", 
				 substring("COMPLEMENTO" FROM '^\d+(?=\s*-)') as "CODCFO",
				 "IDBANCODADOS",
				 "CODCOLIGADA",
				 "CODFILIAL",
				 "CREDITO"
		FROM public."CLANCA" 
		where  "CREDITO" is not NULL
		and upper("COMPLEMENTO") not like '%PAGAM%'  
		and upper("COMPLEMENTO") not like '%S/NF%' 
		and upper("COMPLEMENTO") not like '%S/ NF%'
		and upper("COMPLEMENTO")  like '%NF%'
	  ) 
	    AS subquery
WHERE 	public."FT_NOTAS_FISCAIS"."IDBANCODADOS" =  subquery."IDBANCODADOS" 	AND
		public."FT_NOTAS_FISCAIS"."CODCOLIGADA" =  subquery."CODCOLIGADA"		AND	
		public."FT_NOTAS_FISCAIS"."CODFILIAL" =  subquery."CODFILIAL"		AND	
		--public."FT_NOTAS_FISCAIS"."NUMERO_NF_TRATADO" =  subquery."DOCUMENTO_TRATADO"	AND
		public."FT_NOTAS_FISCAIS"."CODCFO" =  subquery."CODCFO" AND
		public."FT_NOTAS_FISCAIS"."CODCONTA" is NULL;		
	
	
	
UPDATE public."FT_NOTAS_FISCAIS"
  SET 	"CODCONTA" 	= subquery."DEBITO"
FROM (  
		SELECT 	--regexp_replace(split_part("DOCUMENTO", '/', 1),'^0+', '') as "DOCUMENTO_TRATADO", 
				 substring("COMPLEMENTO" FROM '^\d+(?=\s*-)') as "CODCFO",
				 "IDBANCODADOS",
				 "CODCOLIGADA",
				 "CODFILIAL",
				 "DEBITO"
		FROM public."CLANCA" 
		where  "DEBITO" is not NULL
		and upper("COMPLEMENTO")  like '%PAGAM%'  
		and upper("COMPLEMENTO") not like '%S/NF%' 
		and upper("COMPLEMENTO") not like '%S/ NF%'
		and upper("COMPLEMENTO")  like '%NF%'
	  ) 
	    AS subquery
WHERE 	public."FT_NOTAS_FISCAIS"."IDBANCODADOS" =  subquery."IDBANCODADOS" 	AND
		public."FT_NOTAS_FISCAIS"."CODCOLIGADA" =  subquery."CODCOLIGADA"		AND	
		public."FT_NOTAS_FISCAIS"."CODFILIAL" =  subquery."CODFILIAL"		AND	
		--public."FT_NOTAS_FISCAIS"."NUMERO_NF_TRATADO" =  subquery."DOCUMENTO_TRATADO"	AND
		public."FT_NOTAS_FISCAIS"."CODCFO" =  subquery."CODCFO" AND
		public."FT_NOTAS_FISCAIS"."CODCONTA" is NULL;	
	

	
	
	
UPDATE public."FT_NOTAS_FISCAIS"
  SET 	"CODCONTA" 	= subquery."CREDITO"
FROM (  
		SELECT --	regexp_replace(split_part("DOCUMENTO", '/', 1),'^0+', '') as "DOCUMENTO_TRATADO", 
				 substring("COMPLEMENTO" FROM '^\d+(?=\s*-)') as "CODCFO",
				 "IDBANCODADOS",
				 "CODCOLIGADA",
				 "CODFILIAL",
				 "CREDITO"
		FROM public."CLANCA" 
		where  "CREDITO" is not NULL
			AND trim(substring("COMPLEMENTO" FROM '-(.{4})')) = 'NF.'
	  ) 
	    AS subquery
WHERE 	public."FT_NOTAS_FISCAIS"."IDBANCODADOS" =  subquery."IDBANCODADOS" 	AND
		public."FT_NOTAS_FISCAIS"."CODCOLIGADA" =  subquery."CODCOLIGADA"		AND	
		public."FT_NOTAS_FISCAIS"."CODFILIAL" =  subquery."CODFILIAL"		AND	
	--	public."FT_NOTAS_FISCAIS"."NUMERO_NF_TRATADO" =  subquery."DOCUMENTO_TRATADO"	AND
		public."FT_NOTAS_FISCAIS"."CODCFO" =  subquery."CODCFO" AND
		public."FT_NOTAS_FISCAIS"."CODCONTA" is NULL;	
	
	
	
END;
$function$
;
