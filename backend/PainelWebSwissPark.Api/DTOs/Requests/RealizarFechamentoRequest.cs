namespace PainelWebSwissPark.Api.DTOs.Requests;

public class RealizarFechamentoRequest : ChaveFechamentoRequest
{
    /// <summary>Atualiza o saldo atual no momento do fechamento, se informado.</summary>
    public decimal? SaldoAtual { get; set; }
}
