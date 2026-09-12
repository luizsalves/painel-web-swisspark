namespace PainelWebSwissPark.Api.Data.Entities;

/// <summary>
/// Cabeçalho da conciliação de uma conta contábil em um período — equivalente
/// funcional ao que a VW_FT_COMPOSICAO expõe hoje para o Excel (saldo, composição,
/// status e prioridade), agora servido diretamente para o React via API.
/// </summary>
public class Fechamento
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int ContaContabilId { get; set; }
    public int? ResponsavelId { get; set; }

    /// <summary>Primeiro dia do mês de referência (ex.: 2026-01-01 para 01/2026).</summary>
    public DateOnly Periodo { get; set; }

    public decimal SaldoRazao { get; set; }
    public decimal ComposicaoSaldo { get; set; }
    public bool PossuiPendencia { get; set; }

    /// <summary>Backlog | Preparado | Pré-conferido | Conferido | Pendência | Fechado.</summary>
    public string Status { get; set; } = "Backlog";

    /// <summary>Alta | Média | Regular | Baixa.</summary>
    public string Prioridade { get; set; } = "Regular";

    public DateTime AtualizadoEm { get; set; }

    public Empresa? Empresa { get; set; }
    public ContaContabil? ContaContabil { get; set; }
    public Usuario? Responsavel { get; set; }
    public List<Lancamento> Lancamentos { get; set; } = [];
    public List<Pendencia> Pendencias { get; set; } = [];
}
