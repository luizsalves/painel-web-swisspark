using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace PainelWebSwissPark.Api.Data.Repositories;

public class IndicadoresMovimentoMensalLinha
{
    public string CodConta { get; set; } = string.Empty;
    /// <summary>AAAA-MM do movimento; nulo na linha que junta tudo o que veio antes do primeiro mês.</summary>
    public string? AnoMes { get; set; }
    public decimal Debitos { get; set; }
    public decimal Creditos { get; set; }
    /// <summary>Débitos menos créditos, sem as partidas de apuração do resultado.</summary>
    public decimal MovimentoSemApuracao { get; set; }
}

/// <summary>
/// Os movimentos da CPARTIDA por conta e por mês, para montar a base dos indicadores de
/// cada mês com uma leitura só. Consulta nova, separada da de
/// <see cref="IndicadoresTotvsRepository"/>, que continua sem mudança: os mesmos filtros
/// e a mesma regra de apuração (TIPOGERACAO 'A'), só que agrupados por mês.
/// </summary>
public class IndicadoresMensaisTotvsRepository(AppDbContext context)
{
    private static string SqlMovimentosPorMes(BaseTotvs baseDados)
    {
        var banco = BasesTotvs.NomeDoBanco(baseDados);
        return $"""
            WITH MOVIMENTOS AS (
                SELECT LTRIM(RTRIM(DEBITO)) AS CODCONTA, DATA, TIPOGERACAO,
                    CAST(VALOR AS decimal(28,4)) AS DEBITO, CAST(0 AS decimal(28,4)) AS CREDITO
                FROM {banco}.dbo.CPARTIDA WITH (NOLOCK)
                WHERE CODCOLIGADA = @coligada AND DATA < @fim AND DEBITO IS NOT NULL AND LTRIM(RTRIM(DEBITO)) <> ''
                  AND (@filial IS NULL OR CODFILIAL = @filial)
                UNION ALL
                SELECT LTRIM(RTRIM(CREDITO)) AS CODCONTA, DATA, TIPOGERACAO,
                    CAST(0 AS decimal(28,4)), CAST(VALOR AS decimal(28,4))
                FROM {banco}.dbo.CPARTIDA WITH (NOLOCK)
                WHERE CODCOLIGADA = @coligada AND DATA < @fim AND CREDITO IS NOT NULL AND LTRIM(RTRIM(CREDITO)) <> ''
                  AND (@filial IS NULL OR CODFILIAL = @filial)
            ),
            POR_MES AS (
                SELECT CODCONTA, TIPOGERACAO, DEBITO, CREDITO,
                    CASE WHEN DATA < @inicio THEN NULL ELSE CONVERT(char(7), DATA, 126) END AS ANOMES
                FROM MOVIMENTOS
            )
            SELECT CODCONTA, ANOMES,
                CAST(SUM(DEBITO) AS decimal(28,4)) AS DEBITOS,
                CAST(SUM(CREDITO) AS decimal(28,4)) AS CREDITOS,
                CAST(SUM(CASE WHEN ISNULL(TIPOGERACAO, '') <> 'A' THEN DEBITO - CREDITO ELSE 0 END) AS decimal(28,4)) AS MOVIMENTOSEMAPURACAO
            FROM POR_MES
            GROUP BY CODCONTA, ANOMES
            """;
    }

    public Task<List<IndicadoresMovimentoMensalLinha>> ListarAsync(
        BaseTotvs baseDados, DateTime inicio, DateTime fim, short codColigada, short? codFilial,
        CancellationToken cancellationToken)
    {
        context.Database.SetCommandTimeout(180);
        var parametros = new object[]
        {
            new SqlParameter("@inicio", SqlDbType.DateTime2) { Value = inicio },
            new SqlParameter("@fim", SqlDbType.DateTime2) { Value = fim },
            new SqlParameter("@coligada", SqlDbType.SmallInt) { Value = codColigada },
            new SqlParameter("@filial", SqlDbType.SmallInt) { Value = codFilial.HasValue ? codFilial.Value : DBNull.Value },
        };
        return context.Database.SqlQueryRaw<IndicadoresMovimentoMensalLinha>(SqlMovimentosPorMes(baseDados), parametros)
            .ToListAsync(cancellationToken);
    }
}
