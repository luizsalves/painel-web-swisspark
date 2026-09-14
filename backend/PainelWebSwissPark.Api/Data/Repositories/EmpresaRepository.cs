using Microsoft.EntityFrameworkCore;

namespace PainelWebSwissPark.Api.Data.Repositories;

/// <summary>
/// Uma linha do UNION de GFILIAL. Não é entidade do model (não tem ToTable nem
/// chave configurada) de propósito: essa forma só existe para essa consulta
/// específica.
/// </summary>
public class GfilialUniao
{
    public short CodColigada { get; set; }
    public short CodFilial { get; set; }
    public string? NomeFantasia { get; set; }
    public string? Nome { get; set; }
    public string? Cgc { get; set; }
    public string? Cidade { get; set; }
    public string? Estado { get; set; }
}

public class EmpresaRepository(AppDbContext context) : IEmpresaRepository
{
    // Nomes de banco fixos: cada coligada do TOTVS vive num banco próprio no
    // mesmo servidor SQL Server, então dá para unir com nome de três partes em
    // vez de quatro conexões separadas. GFILIAL é replicada idêntica nas quatro
    // bases — o DISTINCT do UNION já colapsa as linhas repetidas sozinho, sem
    // precisar marcar de qual banco veio cada uma.
    private const string Sql = """
        SELECT DISTINCT CODCOLIGADA, CODFILIAL,
            LTRIM(RTRIM(NOMEFANTASIA)) AS NOMEFANTASIA, LTRIM(RTRIM(NOME)) AS NOME,
            LTRIM(RTRIM(CGC)) AS CGC, LTRIM(RTRIM(CIDADE)) AS CIDADE, LTRIM(RTRIM(ESTADO)) AS ESTADO
        FROM corpore.dbo.GFILIAL
        UNION
        SELECT DISTINCT CODCOLIGADA, CODFILIAL,
            LTRIM(RTRIM(NOMEFANTASIA)) AS NOMEFANTASIA, LTRIM(RTRIM(NOME)) AS NOME,
            LTRIM(RTRIM(CGC)) AS CGC, LTRIM(RTRIM(CIDADE)) AS CIDADE, LTRIM(RTRIM(ESTADO)) AS ESTADO
        FROM corpore_bsb.dbo.GFILIAL
        UNION
        SELECT DISTINCT CODCOLIGADA, CODFILIAL,
            LTRIM(RTRIM(NOMEFANTASIA)) AS NOMEFANTASIA, LTRIM(RTRIM(NOME)) AS NOME,
            LTRIM(RTRIM(CGC)) AS CGC, LTRIM(RTRIM(CIDADE)) AS CIDADE, LTRIM(RTRIM(ESTADO)) AS ESTADO
        FROM corpore_mao.dbo.GFILIAL
        UNION
        SELECT DISTINCT CODCOLIGADA, CODFILIAL,
            LTRIM(RTRIM(NOMEFANTASIA)) AS NOMEFANTASIA, LTRIM(RTRIM(NOME)) AS NOME,
            LTRIM(RTRIM(CGC)) AS CGC, LTRIM(RTRIM(CIDADE)) AS CIDADE, LTRIM(RTRIM(ESTADO)) AS ESTADO
        FROM corpore_neu.dbo.GFILIAL
        """;

    public async Task<List<GfilialUniao>> ListarTodasColigadasAsync(CancellationToken cancellationToken)
    {
        return await context.Database.SqlQueryRaw<GfilialUniao>(Sql)
            .OrderBy(f => f.NomeFantasia)
            .ToListAsync(cancellationToken);
    }
}
