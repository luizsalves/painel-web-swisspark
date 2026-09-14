namespace PainelWebSwissPark.Api.Data.Entities.Totvs;

/// <summary>
/// Tabela nativa do TOTVS RM (SQL Server) — empresa/filial. Chave real é
/// CODCOLIGADA + CODFILIAL. Só as colunas usadas pelo painel estão mapeadas
/// aqui; o restante das ~50 colunas da tabela real fica de fora.
/// </summary>
public class Gfilial
{
    public short CodColigada { get; set; }
    public short CodFilial { get; set; }
    public string? Nome { get; set; }
    public string? NomeFantasia { get; set; }
    public string? Cgc { get; set; }
    public string? Cidade { get; set; }
    public string? Estado { get; set; }
    public short Ativo { get; set; }
}
