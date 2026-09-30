namespace PainelWebSwissPark.Api.Data.Repositories;

public static class BasesTotvs
{
    public static readonly BaseTotvs[] Todas =
        [BaseTotvs.Corpore, BaseTotvs.CorporeBsb, BaseTotvs.CorporeMao, BaseTotvs.CorporeNeu];

    // O nome vai direto no texto do SQL, então nunca pode vir de entrada do usuário.
    public static string NomeDoBanco(BaseTotvs baseDados) => baseDados switch
    {
        BaseTotvs.Corpore => "corpore",
        BaseTotvs.CorporeBsb => "corpore_bsb",
        BaseTotvs.CorporeMao => "corpore_mao",
        BaseTotvs.CorporeNeu => "corpore_neu",
        _ => throw new ArgumentOutOfRangeException(nameof(baseDados)),
    };

    /// <summary>Aceita "corpore", "bsb", "mao", "neu" (ou o nome completo do banco).</summary>
    public static BaseTotvs? Ler(string? valor) => valor?.Trim().ToLowerInvariant() switch
    {
        "corpore" => BaseTotvs.Corpore,
        "bsb" or "corpore_bsb" => BaseTotvs.CorporeBsb,
        "mao" or "corpore_mao" => BaseTotvs.CorporeMao,
        "neu" or "corpore_neu" => BaseTotvs.CorporeNeu,
        _ => null,
    };
}
