using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace PainelWebSwissPark.Api.Data.Repositories;

public class IndicadoresSaldoLinha
{
    public string CodConta { get; set; } = string.Empty;
    public decimal SaldoInicial { get; set; }
    public decimal SaldoFinal { get; set; }
    public decimal Debitos { get; set; }
    public decimal Creditos { get; set; }
    /// <summary>Débitos menos créditos do período, sem as partidas de apuração do resultado.</summary>
    public decimal MovimentoSemApuracao { get; set; }
}

public class IndicadoresTotvsRepository(AppDbContext context)
{
    // Saldos devedores positivos. TIPOGERACAO 'A' marca as partidas de apuração
    // que zeram as contas de resultado no encerramento do exercício; sem elas
    // o movimento das contas 3 a 8 é a DRE do período.
    // OPTION (RECOMPILE): o plano guardado na primeira execução servia mal às outras
    // coligadas. Montado para uma pequena, levava a AGV de 0,2 s para até 69 s.
    private static string SqlSaldos(BaseTotvs baseDados)
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
            )
            SELECT CODCONTA,
                CAST(SUM(CASE WHEN DATA < @inicio THEN DEBITO - CREDITO ELSE 0 END) AS decimal(28,4)) AS SALDOINICIAL,
                CAST(SUM(DEBITO - CREDITO) AS decimal(28,4)) AS SALDOFINAL,
                CAST(SUM(CASE WHEN DATA >= @inicio THEN DEBITO ELSE 0 END) AS decimal(28,4)) AS DEBITOS,
                CAST(SUM(CASE WHEN DATA >= @inicio THEN CREDITO ELSE 0 END) AS decimal(28,4)) AS CREDITOS,
                CAST(SUM(CASE WHEN DATA >= @inicio AND ISNULL(TIPOGERACAO, '') <> 'A' THEN DEBITO - CREDITO ELSE 0 END) AS decimal(28,4)) AS MOVIMENTOSEMAPURACAO
            FROM MOVIMENTOS
            GROUP BY CODCONTA
            OPTION (RECOMPILE)
            """;
    }

    public Task<List<IndicadoresSaldoLinha>> ListarSaldosAsync(
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
        return context.Database.SqlQueryRaw<IndicadoresSaldoLinha>(SqlSaldos(baseDados), parametros)
            .ToListAsync(cancellationToken);
    }
}
