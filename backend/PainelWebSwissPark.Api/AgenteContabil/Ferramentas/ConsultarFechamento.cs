using System.Text.Json;
using OpenAI.Chat;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.AgenteContabil.Ferramentas;

/// <summary>
/// Situação de um fechamento específico (empresa + filial + conta + período)
/// em bi_fechamento_validacao: status atual e quem preparou/conferiu/fechou.
/// Mesma leitura da tela de Fechamentos (<see cref="IFechamentoValidacaoService.ObterStatusAtualAsync"/>).
/// </summary>
public class ConsultarFechamento(IEmpresasService empresasService, IFechamentoValidacaoService fechamentoValidacaoService)
{
    public const string NomeFerramenta = "consultar_fechamento";

    public static ChatTool Definicao => ChatTool.CreateFunctionTool(
        functionName: NomeFerramenta,
        functionDescription:
            "Mostra a situação do fechamento de UMA conta numa empresa e período: status atual (Pendente, " +
            "Preparado, Conferido ou Fechado), quem preparou, conferiu e fechou e quando, pendência registrada " +
            "e saldos. Use para 'em que status está a conta X', 'quem fechou a conta Y', 'a conta Z já foi conferida?'.",
        functionParameters: BinaryData.FromString(JsonSerializer.Serialize(new
        {
            type = "object",
            properties = new
            {
                empresa = new { type = "string", description = "Nome (razão social) da empresa." },
                codConta = new { type = "string", description = "Código da conta contábil, ex.: 1.1.1.02.01.0023." },
                anoMes = new { type = "string", description = "Período no formato AAAA-MM." },
                codFilial = new
                {
                    type = "integer",
                    description = "Código da filial — só quando a empresa tem mais de uma filial.",
                },
            },
            required = new[] { "empresa", "codConta", "anoMes" },
        })));

    public async Task<string> ExecutarAsync(JsonElement input, CancellationToken cancellationToken)
    {
        var empresa = input.TryGetProperty("empresa", out var empresaEl) ? empresaEl.GetString() ?? "" : "";
        var codConta = input.TryGetProperty("codConta", out var codContaEl) ? codContaEl.GetString()?.Trim() ?? "" : "";
        var anoMes = input.TryGetProperty("anoMes", out var anoMesEl) ? anoMesEl.GetString() ?? "" : "";
        short? codFilial = input.TryGetProperty("codFilial", out var codFilialEl) && codFilialEl.TryGetInt16(out var f) ? f : null;

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
                filiais = filiais.Select(x => new { x.CodFilial, x.NomeFilial }),
            });
        }

        var filial = filiais[0];
        var situacao = await fechamentoValidacaoService.ObterStatusAtualAsync(
            filial.CodColigada, filial.CodFilial, codConta, anoMes, cancellationToken);

        return JsonSerializer.Serialize(new
        {
            empresa = filial.Nome,
            filial = filial.NomeFilial,
            codConta,
            anoMes,
            status = situacao.Status,
            observacao = situacao.Status == "Pendente"
                ? "Pendente = nenhuma ação registrada ainda para essa conta nesse período."
                : null,
            prioridade = situacao.Prioridade,
            preparadoPor = situacao.PreparadoPorNome,
            preparadoEm = Formatar(situacao.PreparadoEm),
            conferidoPor = situacao.ConferidoPorNome,
            conferidoEm = Formatar(situacao.ConferidoEm),
            fechadoPor = situacao.FechadoPorNome,
            fechadoEm = Formatar(situacao.FechadoEm),
            pendencia = situacao.PendenciaTexto,
            pendenciaResponsavel = situacao.PendenciaResponsavelNome,
            saldoAnterior = situacao.SaldoAnterior,
            saldoAtual = situacao.SaldoAtual,
        });
    }

    private static string? Formatar(DateTimeOffset? data) => data?.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
}
