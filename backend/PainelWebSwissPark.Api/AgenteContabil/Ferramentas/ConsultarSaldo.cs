using System.Text.Json;
using OpenAI.Chat;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.AgenteContabil.Ferramentas;

/// <summary>
/// Ferramenta que traz o saldo contábil de verdade (em R$) de uma conta,
/// filial e período — os mesmos quatro números (saldo inicial, débitos,
/// créditos, saldo final) que aparecem na tela de Fechamentos. O cálculo em si
/// não é duplicado aqui: quem soma é o <see cref="ILancamentosTotvsService"/>
/// que já existe; esta classe só resolve "empresa" (nome) pra coligada/filial
/// e formata o resultado pro agente.
/// </summary>
public class ConsultarSaldo(IEmpresasService empresasService, ILancamentosTotvsService lancamentosService)
{
    public const string NomeFerramenta = "consultar_saldo";

    public static ChatTool Definicao => ChatTool.CreateFunctionTool(
        functionName: NomeFerramenta,
        functionDescription:
            "Consulta o valor do razão (saldo contábil real, em R$) de uma conta específica, numa empresa e " +
            "período, direto dos lançamentos do TOTVS: saldo inicial, débitos, créditos e saldo final (= saldo do " +
            "razão). Use para qualquer pergunta sobre valor/saldo/razão de conta em R$ — não confundir com " +
            "consultar_status_backlog, que é sobre pendências, não sobre dinheiro.",
        functionParameters: BinaryData.FromString(JsonSerializer.Serialize(new
        {
            type = "object",
            properties = new
            {
                empresa = new { type = "string", description = "Nome (razão social) da empresa." },
                codConta = new { type = "string", description = "Código da conta contábil, ex.: 2.2.2.05.01.0005." },
                anoMes = new { type = "string", description = "Período no formato AAAA-MM." },
                codFilial = new
                {
                    type = "integer",
                    description = "Código da filial — só quando a empresa tem mais de uma filial.",
                },
            },
            // Sem conta, empresa ou período não dá pra consultar nada — o modelo
            // precisa perguntar ao usuário em vez de chamar a ferramenta faltando algo.
            required = new[] { "empresa", "codConta", "anoMes" },
        })));

    public async Task<string> ExecutarAsync(JsonElement input, CancellationToken cancellationToken)
    {
        var empresa = input.TryGetProperty("empresa", out var empresaEl) ? empresaEl.GetString() ?? "" : "";
        var codConta = input.TryGetProperty("codConta", out var codContaEl) ? codContaEl.GetString() ?? "" : "";
        var anoMes = input.TryGetProperty("anoMes", out var anoMesEl) ? anoMesEl.GetString() ?? "" : "";
        short? codFilial = input.TryGetProperty("codFilial", out var codFilialEl) && codFilialEl.TryGetInt16(out var cf) ? cf : null;

        var empresas = await empresasService.ListarAsync(cancellationToken);
        var filiais = empresas
            .Where(e => string.Equals(e.Nome, empresa, StringComparison.OrdinalIgnoreCase))
            .Where(e => codFilial is null || e.CodFilial == codFilial)
            .ToList();

        if (filiais.Count == 0)
        {
            return JsonSerializer.Serialize(new
            {
                erro = codFilial is null
                    ? $"Não encontrei nenhuma empresa chamada \"{empresa}\"."
                    : $"A empresa \"{empresa}\" não tem a filial {codFilial}.",
            });
        }

        if (filiais.Count > 1)
        {
            return JsonSerializer.Serialize(new
            {
                erro = "Essa empresa tem mais de uma filial — ofereça as filiais como opções numeradas e chame de novo com codFilial.",
                filiais = filiais.Select(f => new { f.CodFilial, f.NomeFilial }),
            });
        }

        var filial = filiais[0];

        var linhas = await lancamentosService.ListarAsync(
            filial.CodColigada, filial.CodFilial, anoMes, codConta, cancellationToken);

        // A linha-resumo (Data nula) traz o período inteiro já somado; as demais
        // são os lançamentos individuais, que aqui não interessam.
        var resumo = linhas.FirstOrDefault(l => l.Data is null);

        if (resumo is null)
        {
            return JsonSerializer.Serialize(new
            {
                empresa,
                codConta,
                anoMes,
                semLancamentos = true,
                mensagem = "Não há lançamentos para essa conta nesse período.",
            });
        }

        return JsonSerializer.Serialize(new
        {
            empresa,
            filial = filial.NomeFilial,
            codConta,
            anoMes,
            saldoInicial = resumo.SaldoAnterior ?? 0,
            debitos = resumo.Debitos,
            creditos = resumo.Creditos,
            saldoFinal = resumo.SaldoRazao ?? 0,
            observacao = "saldoFinal é o saldo do razão (saldo inicial + débitos + créditos do período).",
        });
    }
}
