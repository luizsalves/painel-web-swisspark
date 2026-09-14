USE [corpore_neu]
GO

SELECT  CONVERT(char(7), DATA, 126) AS ANO_MES, *
  FROM [dbo].[CPARTIDA]
WHERE  ltrim(rtrim(CODCOLIGADA)) = 18
 and CODFILIAL = 2
 and DOCUMENTO like '%11272%'
 and IDPARTIDA = 11272
 --and IDLANCAMENTO = 11272
 and CONVERT(char(7), DATA, 126) = '2025-07'




