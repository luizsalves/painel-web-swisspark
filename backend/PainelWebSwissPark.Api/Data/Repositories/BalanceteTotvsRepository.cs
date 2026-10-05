using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace PainelWebSwissPark.Api.Data.Repositories;

public class BalanceteMovimentoLinha
{
    public short CodColigada { get; set; }
    public short CodFilial { get; set; }
    public string CodConta { get; set; } = string.Empty;
    public string Empresa { get; set; } = string.Empty;
    public decimal SaldoAnterior { get; set; }
    public decimal Debito { get; set; }
    public decimal Credito { get; set; }
}

public class BalanceteDescricaoLinha
{
    public short CodColigada { get; set; }
    public string CodConta { get; set; } = string.Empty;
    public string? Descricao { get; set; }
}

public class BalanceteTotvsRepository(AppDbContext context)
{
    private static string SqlMovimentos(BaseTotvs baseDados)
    {
        var banco = BasesTotvs.NomeDoBanco(baseDados);
        return $"""
            WITH MOVIMENTOS AS (
                SELECT CODCOLIGADA, CODFILIAL, LTRIM(RTRIM(DEBITO)) AS CODCONTA,
                    SUM(CASE WHEN DATA < @inicio THEN CAST(VALOR AS decimal(28,4)) ELSE CAST(0 AS decimal(28,4)) END) AS SALDOANTERIOR,
                    SUM(CASE WHEN DATA >= @inicio THEN CAST(VALOR AS decimal(28,4)) ELSE CAST(0 AS decimal(28,4)) END) AS DEBITO,
                    CAST(0 AS decimal(28,4)) AS CREDITO
                FROM {banco}.dbo.CPARTIDA WITH (NOLOCK)
                WHERE CODCOLIGADA <> 0 AND DATA < @fim AND DEBITO IS NOT NULL AND LTRIM(RTRIM(DEBITO)) <> ''
                  AND (@coligada IS NULL OR CODCOLIGADA = @coligada)
                  AND (@filial IS NULL OR CODFILIAL = @filial)
                  AND (@conta IS NULL OR LTRIM(RTRIM(DEBITO)) = @conta OR LTRIM(RTRIM(DEBITO)) LIKE @prefixo)
                GROUP BY CODCOLIGADA, CODFILIAL, LTRIM(RTRIM(DEBITO))
                UNION ALL
                SELECT CODCOLIGADA, CODFILIAL, LTRIM(RTRIM(CREDITO)) AS CODCONTA,
                    -SUM(CASE WHEN DATA < @inicio THEN CAST(VALOR AS decimal(28,4)) ELSE CAST(0 AS decimal(28,4)) END),
                    CAST(0 AS decimal(28,4)),
                    SUM(CASE WHEN DATA >= @inicio THEN CAST(VALOR AS decimal(28,4)) ELSE CAST(0 AS decimal(28,4)) END)
                FROM {banco}.dbo.CPARTIDA WITH (NOLOCK)
                WHERE CODCOLIGADA <> 0 AND DATA < @fim AND CREDITO IS NOT NULL AND LTRIM(RTRIM(CREDITO)) <> ''
                  AND (@coligada IS NULL OR CODCOLIGADA = @coligada)
                  AND (@filial IS NULL OR CODFILIAL = @filial)
                  AND (@conta IS NULL OR LTRIM(RTRIM(CREDITO)) = @conta OR LTRIM(RTRIM(CREDITO)) LIKE @prefixo)
                GROUP BY CODCOLIGADA, CODFILIAL, LTRIM(RTRIM(CREDITO))
            ), SALDOS AS (
                SELECT CODCOLIGADA, CODFILIAL, CODCONTA,
                    CAST(SUM(SALDOANTERIOR) AS decimal(28,4)) AS SALDOANTERIOR,
                    CAST(SUM(DEBITO) AS decimal(28,4)) AS DEBITO,
                    CAST(SUM(CREDITO) AS decimal(28,4)) AS CREDITO
                FROM MOVIMENTOS
                GROUP BY CODCOLIGADA, CODFILIAL, CODCONTA
            )
            SELECT S.CODCOLIGADA, S.CODFILIAL, S.CODCONTA,
                COALESCE(NULLIF(LTRIM(RTRIM(F.NOME)), ''), CONCAT('Coligada ', S.CODCOLIGADA)) AS EMPRESA,
                S.SALDOANTERIOR, S.DEBITO, S.CREDITO
            FROM SALDOS S
            LEFT JOIN {banco}.dbo.GFILIAL F WITH (NOLOCK)
              ON F.CODCOLIGADA = S.CODCOLIGADA AND F.CODFILIAL = S.CODFILIAL
            """;
    }

    private static string SqlDescricoes(BaseTotvs baseDados) => $"""
        SELECT C.CODCOLIGADA, LTRIM(RTRIM(C.CODCONTA)) AS CODCONTA,
            MAX(NULLIF(LTRIM(RTRIM(C.DESCRICAO)), '')) AS DESCRICAO
        FROM {BasesTotvs.NomeDoBanco(baseDados)}.dbo.CCONTA C WITH (NOLOCK)
        INNER JOIN OPENJSON(@codigos) J ON LTRIM(RTRIM(C.CODCONTA)) = J.[value]
        WHERE @coligada IS NULL OR C.CODCOLIGADA IN (0, @coligada)
        GROUP BY C.CODCOLIGADA, LTRIM(RTRIM(C.CODCONTA))
        """;

    public Task<List<BalanceteMovimentoLinha>> ListarMovimentosAsync(
        BaseTotvs baseDados, DateTime inicio, short? codColigada, short? codFilial,
        string? codConta, CancellationToken cancellationToken)
    {
        context.Database.SetCommandTimeout(180);
        var parametros = new object[]
        {
            new SqlParameter("@inicio", SqlDbType.DateTime2) { Value = inicio },
            new SqlParameter("@fim", SqlDbType.DateTime2) { Value = inicio.AddMonths(1) },
            new SqlParameter("@coligada", SqlDbType.SmallInt) { Value = codColigada.HasValue ? codColigada.Value : DBNull.Value },
            new SqlParameter("@filial", SqlDbType.SmallInt) { Value = codFilial.HasValue ? codFilial.Value : DBNull.Value },
            new SqlParameter("@conta", SqlDbType.VarChar, 60) { Value = codConta is null ? DBNull.Value : codConta },
            new SqlParameter("@prefixo", SqlDbType.VarChar, 62) { Value = codConta is null ? DBNull.Value : codConta + ".%" },
        };
        return context.Database.SqlQueryRaw<BalanceteMovimentoLinha>(SqlMovimentos(baseDados), parametros)
            .ToListAsync(cancellationToken);
    }

    public Task<List<BalanceteDescricaoLinha>> ListarDescricoesAsync(
        BaseTotvs baseDados, IReadOnlyCollection<string> codigos, short? codColigada,
        CancellationToken cancellationToken)
    {
        if (codigos.Count == 0) return Task.FromResult(new List<BalanceteDescricaoLinha>());

        var parametros = new object[]
        {
            new SqlParameter("@codigos", SqlDbType.NVarChar, -1) { Value = JsonSerializer.Serialize(codigos) },
            new SqlParameter("@coligada", SqlDbType.SmallInt) { Value = codColigada.HasValue ? codColigada.Value : DBNull.Value },
        };
        return context.Database.SqlQueryRaw<BalanceteDescricaoLinha>(SqlDescricoes(baseDados), parametros)
            .ToListAsync(cancellationToken);
    }
}
