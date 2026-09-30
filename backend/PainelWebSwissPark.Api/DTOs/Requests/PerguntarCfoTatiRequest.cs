namespace PainelWebSwissPark.Api.DTOs.Requests;

public class MensagemChatDto
{
    /// <summary>"usuario" ou "tati".</summary>
    public string Autor { get; set; } = "";

    public string Texto { get; set; } = "";
}

public class PerguntarCfoTatiRequest
{
    public string Pergunta { get; set; } = "";

    /// <summary>AAAA-MM — mesmo período selecionado nos filtros da tela.</summary>
    public string AnoMes { get; set; } = "";

    /// <summary>Nome (razão social) da empresa filtrada, ou vazio/nulo para todas.</summary>
    public string? Empresa { get; set; }

    /// <summary>Mensagens trocadas antes desta pergunta, mais antiga primeiro.</summary>
    public List<MensagemChatDto> Historico { get; set; } = [];
}
