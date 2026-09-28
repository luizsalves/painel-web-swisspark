USE [corpore_neu]
GO


/*
SELECT  CONVERT(char(7), DATA, 126) AS ANO_MES, *
  FROM [dbo].[CPARTIDA]
WHERE  ltrim(rtrim(CODCOLIGADA)) = 18
 and CODFILIAL = 2
 and DOCUMENTO like '%11272%'
 and IDPARTIDA = 11272
 --and IDLANCAMENTO = 11272
 and CONVERT(char(7), DATA, 126) = '2025-07'

 */




 select * 
 from (
				 SELECT DISTINCT
					'corpore' as banco,
					CODCOLIGADA,
					CODFILIAL,
					LTRIM(RTRIM(NOMEFANTASIA)) AS NOMEFANTASIA,
					LTRIM(RTRIM(NOME)) AS NOME,
					CONCAT('Coligada ', CAST(CODFILIAL AS VARCHAR(2)), '-', LTRIM(RTRIM(NOME))) AS NOME_FILIAL,
					LTRIM(RTRIM(CGC)) AS CGC,
					LTRIM(RTRIM(CIDADE)) AS CIDADE,
					LTRIM(RTRIM(ESTADO)) AS ESTADO
				FROM corpore.dbo.gfilial
				where ativo = 1
				--where ltrim(rtrim(NOME)) = 'AGV Participações Ltda'
				UNION
				SELECT DISTINCT
				'corpore_bsb' as banco,
					CODCOLIGADA,
					CODFILIAL,
					LTRIM(RTRIM(NOMEFANTASIA)) AS NOMEFANTASIA,
					LTRIM(RTRIM(NOME)) AS NOME,
					CONCAT('Coligada ', CAST(CODFILIAL AS VARCHAR(2)), '-', LTRIM(RTRIM(NOME))) AS NOME_FILIAL,
					LTRIM(RTRIM(CGC)) AS CGC,
					LTRIM(RTRIM(CIDADE)) AS CIDADE,
					LTRIM(RTRIM(ESTADO)) AS ESTADO
				FROM corpore_bsb.dbo.gfilial
				where ativo = 1
				--where ltrim(rtrim(NOME)) = 'AGV Participações Ltda'
				UNION
				SELECT DISTINCT
				'corpore_mo' as banco,
					CODCOLIGADA,
					CODFILIAL,
					LTRIM(RTRIM(NOMEFANTASIA)) AS NOMEFANTASIA,
					LTRIM(RTRIM(NOME)) AS NOME,
					CONCAT('Filial', CAST(CODFILIAL AS VARCHAR(2)), '-', LTRIM(RTRIM(NOME))) AS NOME_FILIAL,
					LTRIM(RTRIM(CGC)) AS CGC,
					LTRIM(RTRIM(CIDADE)) AS CIDADE,
					LTRIM(RTRIM(ESTADO)) AS ESTADO
				FROM corpore_mao.dbo.gfilial
				where ativo = 1
				--where ltrim(rtrim(NOME)) = 'AGV Participações Ltda'
				UNION
				SELECT DISTINCT
				'corpore_neu' as banco,
					CODCOLIGADA,
					CODFILIAL,
					LTRIM(RTRIM(NOMEFANTASIA)) AS NOMEFANTASIA,
					LTRIM(RTRIM(NOME)) AS NOME,
					CONCAT('Coligada ', CAST(CODFILIAL AS VARCHAR(2)), '-', LTRIM(RTRIM(NOME))) AS NOME_FILIAL,
					LTRIM(RTRIM(CGC)) AS CGC,
					LTRIM(RTRIM(CIDADE)) AS CIDADE,
					LTRIM(RTRIM(ESTADO)) AS ESTADO
				FROM corpore_neu.dbo.gfilial
				where ativo = 1
		) x 
	where x.NOME =	'SWISS PARK URBANIZAÇÕES LTDA SCP';






	SELECT *
	FROM (
	        SELECT DISTINCT *
        
        FROM corpore.dbo.GFILIAL
        UNION
        SELECT   DISTINCT  *
        FROM corpore_bsb.dbo.GFILIAL
        UNION
        SELECT DISTINCT  *
        FROM corpore_mao.dbo.GFILIAL
        UNION
        SELECT DISTINCT  *
        FROM corpore_neu.dbo.GFILIAL

		) X	where x.NOME like '%URBANIZ%';