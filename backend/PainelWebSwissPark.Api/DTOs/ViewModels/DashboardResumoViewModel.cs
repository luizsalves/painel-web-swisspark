namespace PainelWebSwissPark.Api.DTOs.ViewModels;

/// <summary>
/// Resumo do backlog de conciliação de um período: das combinações de filial
/// ativa x conta contábil ativa (o universo esperado daquele mês), quantas
/// estão em cada etapa do fechamento.
/// </summary>
public class DashboardResumoViewModel
{
    /// <summary>Total de combinações filial-ativa x conta-ativa esperadas no período.</summary>
    public int BacklogTotal { get; set; }
    public int Preparados { get; set; }
    public int Conferidos { get; set; }
    public int Fechados { get; set; }

    /// <summary>Ainda sem nenhuma linha em bi_fechamento_validacao para o período.</summary>
    public int Pendentes { get; set; }
}
