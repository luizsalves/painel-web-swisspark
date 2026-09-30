using System.Text.Json;
using OpenAI.Chat;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.AgenteContabil.Ferramentas;

/// <summary>
/// Empresas e contas com diferença na conciliação: saldo do razão − composição
/// do saldo (NFs de fornecedores e adiantamentos em aberto) ≠ 0, no fim do mês.
/// Mesmo cálculo do GET /api/diferencas.
/// </summary>
public class ConsultarDiferencas(IDiferencasService diferencasService)
{
    public const string NomeFerramenta = "consultar_diferencas";
    private const int MaxEmpresas = 15;
    private const int MaxContas = 20;

    public static ChatTool Definicao => ChatTool.CreateFunctionTool(
        functionName: NomeFerramenta,
        functionDescription:
            "Lista as empresas e contas com diferença na conciliação contábil num período: diferença = saldo do " +
            "razão − composição do saldo (NFs de fornecedores e adiantamentos em aberto). Use para 'quais " +
            "fechamentos/empresas/contas têm diferença', 'onde está a diferença', 'quanto de diferença tem a empresa X'.",
        functionParameters: BinaryData.FromString(JsonSerializer.Serialize(new
        {
            type = "object",
            properties = new
            {
                anoMes = new { type = "string", description = "Período no formato AAAA-MM." },
                empresa = new { type = "string", description = "Nome (razão social) da empresa — opcional, para ver só as contas dela." },
            },
            required = new[] { "anoMes" },
        })));

    public async Task<string> ExecutarAsync(JsonElement input, CancellationToken cancellationToken)
    {
        var anoMes = input.TryGetProperty("anoMes", out var anoMesEl) ? anoMesEl.GetString() ?? "" : "";
        var empresa = input.TryGetProperty("empresa", out var empresaEl) ? empresaEl.GetString() : null;

        var resultado = await diferencasService.ObterAsync(anoMes, empresa, cancellationToken);

        return JsonSerializer.Serialize(new
        {
            anoMes = resultado.AnoMes,
            empresa = string.IsNullOrWhiteSpace(empresa) ? "todas" : empresa,
            totalEmpresasComDiferenca = resultado.TotalEmpresas,
            totalContasComDiferenca = resultado.TotalContas,
            somaDasDiferencasEmValorAbsoluto = resultado.ValorTotal,
            empresas = resultado.Empresas.Take(MaxEmpresas).Select(e => new
            {
                e.Empresa,
                contas = e.Contas,
                diferencaAbsoluta = e.ValorAbsoluto,
                diferencaLiquida = e.ValorLiquido,
            }),
            contas = resultado.Contas.Take(MaxContas).Select(c => new
            {
                c.Empresa,
                filial = c.NomeFilial,
                c.CodConta,
                c.Descricao,
                saldoRazao = c.SaldoRazao,
                composicao = c.Composicao,
                diferenca = c.Diferenca,
            }),
            observacao = resultado.TotalContas > MaxContas
                ? $"Mostrando as {MaxContas} contas com maior diferença de {resultado.TotalContas}."
                : null,
        });
    }
}
