namespace PainelWebSwissPark.Api.DTOs.Requests;

/// <summary>
/// A pergunta feita pelo ícone da Tati nas telas do painel: os filtros que a tela usa e qual
/// tela (e aba) estava aberta, para a Tati saber de que assunto é "como está esse mês?".
/// </summary>
public class PerguntarCfoTatiNaTelaRequest : PerguntarCfoTatiComFiltrosRequest
{
    /// <summary>Nome da tela, como no menu (ex.: "Balancete").</summary>
    public string? Tela { get; set; }

    /// <summary>Aba aberta na tela (ex.: "Balanço"); vazio quando a tela não tem abas.</summary>
    public string? Aba { get; set; }
}
