using Microsoft.EntityFrameworkCore;

namespace PainelWebSwissPark.Api.Data.Repositories;

/// <summary>
/// Lançamentos de CPARTIDA de uma conta no mês somando todas as filiais da empresa, para
/// as empresas que fecham consolidadas. Consulta nova, separada da de
/// <see cref="LancamentosTotvsRepository"/>, que continua sem mudança: o texto é o
/// mesmo, só que o filtro de uma filial virou "as filiais da empresa na coligada".
/// </summary>
public class LancamentosConsolidadosTotvsRepository(AppDbContext context)
{
    // __SCHEMA__ nunca vem de entrada do usuário — só de BasesTotvs.NomeDoBanco().
    // A empresa é comparada sem caixa e sem acento, porque o mesmo nome aparece
    // escrito de jeitos diferentes no GFILIAL.
    private const string SqlTemplate = """
        WITH FILIAIS AS (
            SELECT F.[CODFILIAL]
            FROM __SCHEMA__.dbo.[GFILIAL] F WITH (NOLOCK)
            WHERE F.[CODCOLIGADA] = {0}
              AND LTRIM(RTRIM(F.[NOME])) COLLATE Latin1_General_CI_AI = {1} COLLATE Latin1_General_CI_AI
        ),
        LANCAMENTOS AS (
            SELECT
                CONVERT(char(7), A.[DATA], 126) AS [ANOMES],
                A.[DATA],
                UPPER(LTRIM(RTRIM(E.[NOME]))) AS [EMPRESA],
                A.[CODCOLIGADA],
                COALESCE(
                    NULLIF(LTRIM(RTRIM(CONVERT(varchar(80), CT.[REDUZIDO]))), ''),
                    NULLIF(LTRIM(RTRIM(CONVERT(varchar(80), CT0.[REDUZIDO]))), ''),
                    '-'
                ) AS [REDUZIDO],
                {3} AS [CODCONTA],
                A.[CODFILIAL],
                LTRIM(RTRIM(E.[NOMEFANTASIA])) AS [NOMEFANTASIA],
                A.[COMPLEMENTO],
                A.[DOCUMENTO],
                A.[IDLANCAMENTO] AS [IDLANCAMENTO],
                A.[IDPARTIDA] AS [IDPARTIDA],
                A.[USUARIO],
                CASE
                    WHEN A.[DEBITO] = {3} AND A.[DEBITO] LIKE '1%' THEN A.[VALOR]
                    WHEN A.[DEBITO] = {3} THEN (-1) * A.[VALOR]
                    ELSE 0
                END AS [DEBITOS],
                CASE
                    WHEN A.[CREDITO] = {3} AND A.[CREDITO] LIKE '1%' THEN (-1) * A.[VALOR]
                    WHEN A.[CREDITO] = {3} THEN A.[VALOR]
                    ELSE 0
                END AS [CREDITOS]
            FROM __SCHEMA__.dbo.[CPARTIDA] A WITH (NOLOCK)
            LEFT JOIN __SCHEMA__.dbo.[GFILIAL] E WITH (NOLOCK)
                ON E.[CODCOLIGADA] = A.[CODCOLIGADA] AND E.[CODFILIAL] = A.[CODFILIAL]
            LEFT JOIN __SCHEMA__.dbo.[CCONTA] CT WITH (NOLOCK)
                ON CT.[CODCOLIGADA] = A.[CODCOLIGADA]
               AND CT.[CODCONTA] = {3}
            LEFT JOIN __SCHEMA__.dbo.[CCONTA] CT0 WITH (NOLOCK)
                ON CT0.[CODCOLIGADA] = 0
               AND CT0.[CODCONTA] = {3}
            WHERE A.[CODCOLIGADA] <> 0 AND A.[CODCOLIGADA] = {0}
              AND A.[CODFILIAL] IN (SELECT [CODFILIAL] FROM FILIAIS)
              AND CONVERT(char(7), A.[DATA], 126) = {2}
              AND (A.[DEBITO] = {3} OR A.[CREDITO] = {3})
        ),
        ANTERIOR AS (
            SELECT
                {3} AS [CODCONTA],
                SUM(X.[VALOR]) AS [ANTERIOR]
            FROM (
                SELECT
                    CASE
                        WHEN {3} LIKE '1%' THEN SUM(A.[VALOR])
                        ELSE -SUM(A.[VALOR])
                    END AS [VALOR]
                FROM __SCHEMA__.dbo.[CPARTIDA] A WITH (NOLOCK)
                WHERE A.[CODCOLIGADA] <> 0 AND A.[CODCOLIGADA] = {0}
                  AND A.[CODFILIAL] IN (SELECT [CODFILIAL] FROM FILIAIS)
                  AND A.[DATA] < CONVERT(date, {2} + '-01')
                  AND A.[DEBITO] = {3}

                UNION ALL

                SELECT
                    CASE
                        WHEN {3} LIKE '1%' THEN -SUM(A.[VALOR])
                        ELSE SUM(A.[VALOR])
                    END
                FROM __SCHEMA__.dbo.[CPARTIDA] A WITH (NOLOCK)
                WHERE A.[CODCOLIGADA] <> 0 AND A.[CODCOLIGADA] = {0}
                  AND A.[CODFILIAL] IN (SELECT [CODFILIAL] FROM FILIAIS)
                  AND A.[DATA] < CONVERT(date, {2} + '-01')
                  AND A.[CREDITO] = {3}
            ) X
        )
        SELECT * FROM (
            SELECT
                L.[ANOMES], L.[DATA], L.[EMPRESA], L.[CODCOLIGADA], L.[REDUZIDO],
                L.[CODCONTA], L.[CODFILIAL], L.[NOMEFANTASIA], L.[COMPLEMENTO], L.[DOCUMENTO], L.[IDLANCAMENTO], L.[IDPARTIDA],
                L.[USUARIO], L.[DEBITOS], L.[CREDITOS],
                CAST(NULL AS NUMERIC(18,4)) AS [SALDORAZAO],
                CAST(NULL AS NUMERIC(18,4)) AS [SALDOANTERIOR]
            FROM LANCAMENTOS L

            UNION ALL

            -- A linha-resumo é da empresa inteira: filial 0, a mesma do fechamento consolidado.
            SELECT
                L.[ANOMES], NULL, MAX(L.[EMPRESA]), MAX(L.[CODCOLIGADA]), MAX(L.[REDUZIDO]),
                L.[CODCONTA], CAST(0 AS smallint), MAX(L.[NOMEFANTASIA]), 'SALDO DO RAZAO (COMPOSICAO)', NULL, NULL, NULL,
                NULL, SUM(L.[DEBITOS]), SUM(L.[CREDITOS]),
                COALESCE(MAX(A.[ANTERIOR]), 0) + SUM(L.[DEBITOS]) + SUM(L.[CREDITOS]) AS [SALDORAZAO],
                COALESCE(MAX(A.[ANTERIOR]), 0) AS [SALDOANTERIOR]
            FROM LANCAMENTOS L
            LEFT JOIN ANTERIOR A ON A.[CODCONTA] = L.[CODCONTA]
            GROUP BY L.[ANOMES], L.[CODCONTA]
        ) AS RESULTADO
        ORDER BY CASE WHEN [DATA] IS NULL THEN 1 ELSE 0 END, [DATA]
        """;

    public async Task<List<LancamentoTotvsLinha>> ListarAsync(
        BaseTotvs baseDados, short codColigada, string empresa, string anoMes, string codConta,
        CancellationToken cancellationToken)
    {
        var sql = SqlTemplate.Replace("__SCHEMA__", BasesTotvs.NomeDoBanco(baseDados));

        return await context.Database
            .SqlQueryRaw<LancamentoTotvsLinha>(sql, codColigada, empresa, anoMes, codConta)
            .ToListAsync(cancellationToken);
    }
}
