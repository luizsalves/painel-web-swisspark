using Microsoft.EntityFrameworkCore;

namespace PainelWebSwissPark.Api.Data.Repositories;

/// <summary>
/// Conta encontrada no crédito ou débito da CPARTIDA, identificada por coligada.
/// </summary>
public class ContaContabilMovimentada
{
    public short CodColigada { get; set; }
    public short CodFilial { get; set; }
    public string CodConta { get; set; } = string.Empty;
    public string? Reduzido { get; set; }
    public string? Descricao { get; set; }
}

public class ColigadaFilialMovimentada
{
    public short CodColigada { get; set; }
    public short CodFilial { get; set; }
}

public class EmpresaContabil
{
    public short CodColigada { get; set; }
    public string Nome { get; set; } = string.Empty;
}

public class ContaContabilTotvsRepository(AppDbContext context) : IContaContabilTotvsRepository
{
    // Contas, coligadas e filiais vêm da CPARTIDA. Descrição e reduzido são
    // atributos da conta: primeiro na coligada, depois no cadastro global (0).
    // O LEFT JOIN mantém contas movimentadas mesmo sem descrição cadastrada.
    // Os nomes dos bancos vêm exclusivamente da lista fixa de BasesTotvs.
    private static string SqlDaBase(BaseTotvs baseDados)
    {
        var banco = BasesTotvs.NomeDoBanco(baseDados);
        return $"""
            SELECT P.CODCOLIGADA, P.CODFILIAL, P.CODCONTA,
                COALESCE(CL.DESCRICAO, C.DESCRICAO_GLOBAL, C.DESCRICAO_OUTRA) AS DESCRICAO,
                COALESCE(CL.REDUZIDO, C.REDUZIDO_GLOBAL) AS REDUZIDO
            FROM (
                SELECT DISTINCT CODCOLIGADA, CODFILIAL, LTRIM(RTRIM(CREDITO)) AS CODCONTA
                FROM {banco}.dbo.CPARTIDA WITH (NOLOCK)
                WHERE CREDITO IS NOT NULL AND CODCOLIGADA <> 0
                UNION
                SELECT DISTINCT CODCOLIGADA, CODFILIAL, LTRIM(RTRIM(DEBITO)) AS CODCONTA
                FROM {banco}.dbo.CPARTIDA WITH (NOLOCK)
                WHERE DEBITO IS NOT NULL AND CODCOLIGADA <> 0
            ) P
            LEFT JOIN (
                SELECT CODCOLIGADA, LTRIM(RTRIM(CODCONTA)) AS CODCONTA,
                    MAX(NULLIF(LTRIM(RTRIM(DESCRICAO)), '')) AS DESCRICAO,
                    MAX(NULLIF(LTRIM(RTRIM(CONVERT(varchar(80), REDUZIDO))), '')) AS REDUZIDO
                FROM {banco}.dbo.CCONTA WITH (NOLOCK)
                WHERE CODCOLIGADA <> 0
                GROUP BY CODCOLIGADA, LTRIM(RTRIM(CODCONTA))
            ) CL ON CL.CODCOLIGADA = P.CODCOLIGADA AND CL.CODCONTA = P.CODCONTA
            LEFT JOIN (
                SELECT LTRIM(RTRIM(CODCONTA)) AS CODCONTA,
                    MAX(CASE WHEN CODCOLIGADA = 0 THEN NULLIF(LTRIM(RTRIM(DESCRICAO)), '') END) AS DESCRICAO_GLOBAL,
                    MAX(CASE WHEN CODCOLIGADA = 0 THEN NULLIF(LTRIM(RTRIM(CONVERT(varchar(80), REDUZIDO))), '') END) AS REDUZIDO_GLOBAL,
                    MIN(NULLIF(LTRIM(RTRIM(DESCRICAO)), '')) AS DESCRICAO_OUTRA
                FROM {banco}.dbo.CCONTA WITH (NOLOCK)
                GROUP BY LTRIM(RTRIM(CODCONTA))
            ) C ON C.CODCONTA = P.CODCONTA
            """;
    }

    private static readonly string Sql = string.Join("\nUNION\n", BasesTotvs.Todas.Select(SqlDaBase));
    private static readonly string SqlBsb = SqlDaBase(BaseTotvs.CorporeBsb);
    private static readonly string SqlEmpresas = string.Join("\nUNION\n", BasesTotvs.Todas.Select(baseDados => $"""
        SELECT DISTINCT F.CODCOLIGADA, LTRIM(RTRIM(F.NOME)) AS NOME
        FROM {BasesTotvs.NomeDoBanco(baseDados)}.dbo.GFILIAL F WITH (NOLOCK)
        WHERE F.ATIVO = 1 AND F.CODCOLIGADA <> 0
          AND EXISTS (SELECT 1 FROM {BasesTotvs.NomeDoBanco(baseDados)}.dbo.CPARTIDA P WITH (NOLOCK)
                      WHERE P.CODCOLIGADA = F.CODCOLIGADA)
        """));
    private static readonly string SqlColigadasFiliais = string.Join("\nUNION\n", BasesTotvs.Todas.Select(baseDados => $"""
        SELECT DISTINCT CODCOLIGADA, CODFILIAL
        FROM {BasesTotvs.NomeDoBanco(baseDados)}.dbo.CPARTIDA WITH (NOLOCK)
        WHERE CODCOLIGADA <> 0
        """));

    public Task<List<EmpresaContabil>> ListarEmpresasAsync(CancellationToken cancellationToken) =>
        context.Database.SqlQueryRaw<EmpresaContabil>(SqlEmpresas)
            .OrderBy(e => e.Nome).ToListAsync(cancellationToken);

    public Task<List<ColigadaFilialMovimentada>> ListarColigadasFiliaisAsync(
        CancellationToken cancellationToken, short? codColigada = null)
    {
        var query = context.Database.SqlQueryRaw<ColigadaFilialMovimentada>(SqlColigadasFiliais);
        if (codColigada.HasValue)
            query = query.Where(c => c.CodColigada == codColigada.Value);
        return query
            .OrderBy(c => c.CodColigada).ThenBy(c => c.CodFilial)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ContaContabilMovimentada>> ListarTodasColigadasAsync(
        CancellationToken cancellationToken, bool somenteBsb = false, short? codColigada = null, short? codFilial = null)
    {
        var query = context.Database.SqlQueryRaw<ContaContabilMovimentada>(somenteBsb ? SqlBsb : Sql);
        if (codColigada.HasValue)
            query = query.Where(c => c.CodColigada == codColigada.Value);
        if (codFilial.HasValue)
            query = query.Where(c => c.CodFilial == codFilial.Value);
        return await query.OrderBy(c => c.CodConta)
            .ToListAsync(cancellationToken);
    }
}
