using Microsoft.EntityFrameworkCore;

namespace PainelWebSwissPark.Api.Data.Repositories;

/// <summary>
/// Uma linha do UNION de CCONTA. Não é entidade do model (sem ToTable/chave) de
/// propósito: essa forma só existe para essa consulta específica.
/// </summary>
public class CcontaUniao
{
    public short CodColigada { get; set; }
    public string CodConta { get; set; } = string.Empty;
    public string? Reduzido { get; set; }
    public string? Descricao { get; set; }
}

public class ContaContabilTotvsRepository(AppDbContext context) : IContaContabilTotvsRepository
{
    // Mesma lógica do EmpresaRepository: cada coligada vive num banco próprio
    // no mesmo SQL Server, então dá para unir com nome de três partes. CCONTA
    // é replicada idêntica nas quatro bases — o DISTINCT do UNION já colapsa
    // as linhas repetidas sozinho. CODCONTA precisa de LTRIM/RTRIM igual aos
    // outros campos de texto: é CHAR de largura fixa, e o padding pode variar
    // entre as bases, o que faz a mesma conta virar duas linhas "diferentes"
    // (uma delas com espaço sobrando) que nem o DISTINCT nem o dedupe em C#
    // enxergam como iguais.
    private const string Sql = """
        SELECT DISTINCT CODCOLIGADA, LTRIM(RTRIM(CODCONTA)) AS CODCONTA,
            LTRIM(RTRIM(REDUZIDO)) AS REDUZIDO, LTRIM(RTRIM(DESCRICAO)) AS DESCRICAO
        FROM corpore.dbo.CCONTA
        UNION
        SELECT DISTINCT CODCOLIGADA, LTRIM(RTRIM(CODCONTA)) AS CODCONTA,
            LTRIM(RTRIM(REDUZIDO)) AS REDUZIDO, LTRIM(RTRIM(DESCRICAO)) AS DESCRICAO
        FROM corpore_bsb.dbo.CCONTA
        UNION
        SELECT DISTINCT CODCOLIGADA, LTRIM(RTRIM(CODCONTA)) AS CODCONTA,
            LTRIM(RTRIM(REDUZIDO)) AS REDUZIDO, LTRIM(RTRIM(DESCRICAO)) AS DESCRICAO
        FROM corpore_mao.dbo.CCONTA
        UNION
        SELECT DISTINCT CODCOLIGADA, LTRIM(RTRIM(CODCONTA)) AS CODCONTA,
            LTRIM(RTRIM(REDUZIDO)) AS REDUZIDO, LTRIM(RTRIM(DESCRICAO)) AS DESCRICAO
        FROM corpore_neu.dbo.CCONTA
        """;

    public async Task<List<CcontaUniao>> ListarTodasColigadasAsync(CancellationToken cancellationToken)
    {
        return await context.Database.SqlQueryRaw<CcontaUniao>(Sql)
            .OrderBy(c => c.CodConta)
            .ToListAsync(cancellationToken);
    }
}
