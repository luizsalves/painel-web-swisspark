using Microsoft.EntityFrameworkCore;

namespace PainelWebSwissPark.Api.Data.Repositories;

/// <summary>
/// Uma linha do razão (lançamento) ou a linha extra de SALDO_RAZAO por
/// ANO_MES+CODCONTA. Não é entidade do model — só existe para essa consulta.
/// </summary>
public class LancamentoTotvsLinha
{
    public string? AnoMes { get; set; }
    public DateTime? Data { get; set; }
    public string? Empresa { get; set; }
    public short CodColigada { get; set; }
    public string? Reduzido { get; set; }
    public string CodConta { get; set; } = string.Empty;
    public short CodFilial { get; set; }
    public string? NomeFantasia { get; set; }
    public string? Complemento { get; set; }
    public string? Documento { get; set; }
    public int? IdLancamento { get; set; }
    public int? IdPartida { get; set; }
    public string? Usuario { get; set; }
    public decimal Debitos { get; set; }
    public decimal Creditos { get; set; }
    public decimal? SaldoRazao { get; set; }
    public decimal? SaldoAnterior { get; set; }
}

public class LancamentosTotvsRepository(AppDbContext context) : ILancamentosTotvsRepository
{
    // __SCHEMA__ nunca vem de entrada do usuário — só de NomeDoBanco(), que
    // mapeia o enum pros 4 nomes fixos. String.Replace em vez de interpolação
    // porque a query já usa {0}..{3} pros parâmetros do SqlQueryRaw.
    private const string SqlTemplate = """
        WITH LANCAMENTOS AS (
            SELECT
                CONVERT(char(7), A.[DATA], 126) AS [ANOMES],
                A.[DATA],
                UPPER(LTRIM(RTRIM(E.[NOME]))) AS [EMPRESA],
                A.[CODCOLIGADA],
                COALESCE(CT.[REDUZIDO], '-') AS [REDUZIDO],
                CASE WHEN A.[DEBITO] IS NOT NULL THEN A.[DEBITO] ELSE A.[CREDITO] END AS [CODCONTA],
                A.[CODFILIAL],
                LTRIM(RTRIM(E.[NOMEFANTASIA])) AS [NOMEFANTASIA],
                A.[COMPLEMENTO],
                A.[DOCUMENTO],
                A.[IDLANCAMENTO] AS [IDLANCAMENTO],
                A.[IDPARTIDA] AS [IDPARTIDA],
                A.[USUARIO],
                CASE
                    WHEN A.[DEBITO] IS NOT NULL AND CAST(A.[DEBITO] AS VARCHAR(50)) LIKE '1.%' THEN A.[VALOR]
                    WHEN A.[DEBITO] IS NOT NULL THEN (-1) * A.[VALOR]
                    ELSE 0
                END AS [DEBITOS],
                CASE
                    WHEN A.[CREDITO] IS NOT NULL AND CAST(A.[CREDITO] AS VARCHAR(50)) LIKE '1.%' THEN (-1) * A.[VALOR]
                    WHEN A.[CREDITO] IS NOT NULL THEN A.[VALOR]
                    ELSE 0
                END AS [CREDITOS]
            FROM __SCHEMA__.dbo.[CPARTIDA] A
            LEFT JOIN __SCHEMA__.dbo.[GFILIAL] E
                ON E.[CODCOLIGADA] = A.[CODCOLIGADA] AND E.[CODFILIAL] = A.[CODFILIAL]
            LEFT JOIN __SCHEMA__.dbo.[CCONTA] CT
                ON CT.[CODCOLIGADA] = A.[CODCOLIGADA]
               AND CT.[CODCONTA] = (CASE WHEN A.[DEBITO] IS NOT NULL THEN A.[DEBITO] ELSE A.[CREDITO] END)
            WHERE A.[CODCOLIGADA] = {0}
              AND A.[CODFILIAL] = {1}
              AND CONVERT(char(7), A.[DATA], 126) = {2}
              AND (CASE WHEN A.[DEBITO] IS NOT NULL THEN A.[DEBITO] ELSE A.[CREDITO] END) = {3}
        ),
        ANTERIOR AS (
            SELECT
                CASE WHEN A.[DEBITO] IS NOT NULL THEN A.[DEBITO] ELSE A.[CREDITO] END AS [CODCONTA],
                SUM(
                    CASE
                        WHEN A.[DEBITO] IS NOT NULL AND CAST(A.[DEBITO] AS VARCHAR(50)) LIKE '1.%' THEN A.[VALOR]
                        WHEN A.[DEBITO] IS NOT NULL THEN (-1) * A.[VALOR]
                        WHEN A.[CREDITO] IS NOT NULL AND CAST(A.[CREDITO] AS VARCHAR(50)) LIKE '1.%' THEN (-1) * A.[VALOR]
                        WHEN A.[CREDITO] IS NOT NULL THEN A.[VALOR]
                        ELSE 0
                    END
                ) AS [ANTERIOR]
            FROM __SCHEMA__.dbo.[CPARTIDA] A
            WHERE A.[CODCOLIGADA] = {0}
              AND A.[CODFILIAL] = {1}
              AND (CASE WHEN A.[DEBITO] IS NOT NULL THEN A.[DEBITO] ELSE A.[CREDITO] END) = {3}
              AND A.[DATA] < CONVERT(date, {2} + '-01')
            GROUP BY CASE WHEN A.[DEBITO] IS NOT NULL THEN A.[DEBITO] ELSE A.[CREDITO] END
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

            SELECT
                L.[ANOMES], NULL, MAX(L.[EMPRESA]), MAX(L.[CODCOLIGADA]), MAX(L.[REDUZIDO]),
                L.[CODCONTA], MAX(L.[CODFILIAL]), MAX(L.[NOMEFANTASIA]), 'SALDO DO RAZAO (COMPOSICAO)', NULL, NULL, NULL,
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
        BaseTotvs baseDados, short codColigada, short codFilial, string anoMes, string codConta,
        CancellationToken cancellationToken)
    {
        var sql = SqlTemplate.Replace("__SCHEMA__", NomeDoBanco(baseDados));

        return await context.Database
            .SqlQueryRaw<LancamentoTotvsLinha>(sql, codColigada, codFilial, anoMes, codConta)
            .ToListAsync(cancellationToken);
    }

    private static string NomeDoBanco(BaseTotvs baseDados) => baseDados switch
    {
        BaseTotvs.Corpore => "corpore",
        BaseTotvs.CorporeBsb => "corpore_bsb",
        BaseTotvs.CorporeMao => "corpore_mao",
        BaseTotvs.CorporeNeu => "corpore_neu",
        _ => throw new ArgumentOutOfRangeException(nameof(baseDados)),
    };
}
