using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using PainelWebSwissPark.Api.AgenteContabil.Ferramentas;
using PainelWebSwissPark.Api.DTOs.Requests;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.AgenteContabil;

/// <summary>
/// O resumo do mês do botão do Dashboard: as ferramentas que ele chama, sempre as mesmas, com os
/// filtros da tela. Com empresa, a saúde financeira do período (indicadores e DFC, como as telas)
/// e o balanço no fim dele; num mês só, também a conciliação, que a ferramenta conta por mês (com
/// vários meses o número não bateria com a Produtividade, que soma o período). Sem empresa, só a
/// conciliação de todas. Classe pura: quem executa é o <see cref="HarnessService"/>.
/// </summary>
public static class ResumoDoMesTati
{
    /// <summary>As perguntas que vão para o modelo; a seção "Resumo do mês" das instruções diz o que fazer.</summary>
    public const string PerguntaDoMes = "Resumo do mês", PerguntaDoPeriodo = "Resumo do período";

    /// <summary>A resposta quando não há o que resumir: sem empresa e com vários meses.</summary>
    public const string SemOQueResumir =
        "Para o resumo, escolha uma empresa nos filtros — ou um mês só, para eu resumir a conciliação de todas as empresas.";

    /// <summary>Uma ferramenta com os argumentos em JSON, como o modelo pediria.</summary>
    public sealed record Chamada(string Ferramenta, string Argumentos);

    // Os argumentos vazios ficam de fora, como o modelo faz.
    private static readonly JsonSerializerOptions OpcoesJson = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// "Resumo do mês" ou, com vários meses nos filtros, "Resumo do período": pedir o resumo "do mês"
    /// com um trimestre fazia o modelo querer os números de um mês só, que ele não tinha.
    /// </summary>
    public static string Pergunta(PerguntarCfoTatiComFiltrosRequest pedido) =>
        AnoMesInicial(pedido) is null ? PerguntaDoMes : PerguntaDoPeriodo;

    public static IReadOnlyList<Chamada> Chamadas(PerguntarCfoTatiComFiltrosRequest pedido)
    {
        var anoMes = pedido.AnoMes.Trim();
        var anoMesInicial = AnoMesInicial(pedido);
        var empresa = string.IsNullOrWhiteSpace(pedido.Empresa) ? null : pedido.Empresa.Trim();

        var chamadas = new List<Chamada>();
        if (empresa is not null)
        {
            // Nas empresas de filiais consolidadas a filial não recorta nada, como no contexto da tela.
            var codFilial = EmpresasConsolidadas.Consolida(empresa) ? null : pedido.CodFilial;
            var codColigada = pedido.CodColigada;
            chamadas.Add(new(ConsultarIndicadores.NomeFerramenta, Json(new { empresa, anoMes, anoMesInicial, codColigada, codFilial })));
            chamadas.Add(new(ConsultarDfc.NomeFerramenta, Json(new { empresa, anoMes, anoMesInicial, codColigada, codFilial })));
            // Detalhado, como a tela: os grupos (Disponível, Estoques, Fornecedores...) estão no card do balanço.
            chamadas.Add(new(ConsultarBalanco.NomeFerramenta, Json(new { empresa, anoMes, codColigada, codFilial, detalhado = true })));
        }
        if (anoMesInicial is null)
        {
            chamadas.Add(new(ConsultarBacklogStatus.NomeFerramenta, Json(new { anoMes, empresa })));
            if (empresa is null)
            {
                chamadas.Add(new(ConsultarBacklogPorEmpresa.NomeFerramenta, Json(new { anoMes })));
            }
        }
        return chamadas;
    }

    // O primeiro mês só quando o período tem mais de um mês.
    private static string? AnoMesInicial(PerguntarCfoTatiComFiltrosRequest pedido) =>
        string.IsNullOrWhiteSpace(pedido.AnoMesInicial) || pedido.AnoMesInicial.Trim() == pedido.AnoMes.Trim()
            ? null
            : pedido.AnoMesInicial.Trim();

    private static string Json(object argumentos) => JsonSerializer.Serialize(argumentos, OpcoesJson);
}
