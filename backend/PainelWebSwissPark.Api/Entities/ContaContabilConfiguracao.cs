namespace PainelWebSwissPark.Api.Entities;

/// <summary>
/// Override do painel para ativar/desativar uma conta contábil do TOTVS.
/// CCONTA (corpore/corpore_bsb/corpore_mao/corpore_neu) é somente leitura —
/// esse flag mora só aqui, no Postgres do painel. A ausência de linha para uma
/// coligada+conta significa ativa (default); só ganha linha quando alguém
/// explicitamente ativa ou desativa pela tela de Contas Contábeis.
/// </summary>
public class ContaContabilConfiguracao
{
    public short CodColigada { get; set; }
    public string CodConta { get; set; } = string.Empty;
    public bool Ativo { get; set; }
    public Guid AtualizadoPor { get; set; }
    public DateTimeOffset AtualizadoEm { get; set; }
}
