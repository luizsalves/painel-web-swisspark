namespace PainelWebSwissPark.Api.Entities;

/// <summary>
/// Override do painel para ativar/desativar uma coligada+filial do TOTVS.
/// GFILIAL (corpore/corpore_bsb/corpore_mao/corpore_neu) é somente leitura —
/// esse flag mora só aqui, no Postgres do painel. A ausência de linha para uma
/// coligada+filial significa ativa (default); só ganha linha quando alguém
/// explicitamente ativa ou desativa pela tela de Empresas.
/// </summary>
public class EmpresaConfiguracao
{
    public short CodColigada { get; set; }
    public short CodFilial { get; set; }
    public bool Ativo { get; set; }
    public Guid AtualizadoPor { get; set; }
    public DateTimeOffset AtualizadoEm { get; set; }
}
