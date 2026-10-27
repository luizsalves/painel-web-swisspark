using System.Diagnostics;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using OpenAI.Chat;
using PainelWebSwissPark.Api.DTOs.ViewModels;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.AgenteContabil.Ferramentas;

/// <summary>
/// Ferramenta que traz o balanço patrimonial de uma empresa no fim de um mês e no fim do mês
/// anterior, com os mesmos números da tela Balanço. Quem calcula é o <see cref="IBalancoService"/>
/// que já existe, chamado para os dois meses ao mesmo tempo; esta classe só acha a empresa pelo nome, junta as
/// linhas da tela em blocos e grupos e põe os dois meses lado a lado.
/// </summary>
public class ConsultarBalanco(
    IEmpresasService empresasService, IBalancoService balancoService, IServiceScopeFactory escopos,
    ILogger<ConsultarBalanco> logger)
{
    public const string NomeFerramenta = "consultar_balanco";

    // O bloco do passivo que é o PL; o total "PASSIVO" da tela já o inclui.
    private const string PatrimonioLiquido = "Patrimônio Líquido";

    // Sem escapar os acentos: o JSON vai para o modelo, não para uma página.
    private static readonly JsonSerializerOptions OpcoesJson = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static ChatTool Definicao => ChatTool.CreateFunctionTool(
        functionName: NomeFerramenta,
        functionDescription:
            "Consulta o balanço patrimonial de uma empresa no último dia de um mês, com os mesmos números da tela " +
            "Balanço: ativo, passivo e patrimônio líquido por bloco (Ativo Circulante, Ativo Não Circulante, Passivo " +
            "Circulante, Passivo Não Circulante, Patrimônio Líquido), cada um com o valor do fim do mês anterior e a " +
            "variação, e os totais de ativo, passivo exigível, PL e passivo + PL. Com detalhado = true, vêm também os " +
            "grupos (Disponível, Realizável...) e as linhas (Caixa, Bancos, Fornecedores, Empréstimos...). Use para " +
            "posição patrimonial, total do ativo ou do passivo, patrimônio líquido ou 'compare com o mês anterior'.",
        functionParameters: BinaryData.FromString(JsonSerializer.Serialize(new
        {
            type = "object",
            properties = new
            {
                empresa = new
                {
                    type = "string",
                    description = "Nome da empresa ou parte dele; maiúsculas e acentos não importam.",
                },
                anoMes = new { type = "string", description = "Mês do balanço, no formato AAAA-MM (posição no último dia)." },
                codColigada = new
                {
                    type = "integer",
                    description = "Código da coligada — só quando a ferramenta devolver mais de uma empresa com o mesmo nome.",
                },
                codFilial = new
                {
                    type = "integer",
                    description = "Código da filial — a que o usuário pedir ou, se ele não disser, a do contexto da tela. " +
                        "Sem ele, entram todas as filiais da coligada.",
                },
                detalhado = new
                {
                    type = "boolean",
                    description = "true para trazer também os grupos e as linhas de cada bloco. Padrão: false, só os blocos.",
                },
            },
            required = new[] { "empresa", "anoMes" },
        })));

    public async Task<string> ExecutarAsync(JsonElement input, CancellationToken cancellationToken)
    {
        var parametros = new LocalizarEmpresa.Parametros(input);
        var empresa = parametros.Texto("empresa");
        var anoMes = parametros.Texto("anoMes");
        var codColigada = parametros.Inteiro("codColigada");
        var codFilial = parametros.Inteiro("codFilial");
        var detalhado = parametros.Booleano("detalhado");

        if (parametros.Erro is { } erroDeTipo)
        {
            return Serializar(erroDeTipo);
        }
        if (LocalizarEmpresa.ValidarParametros(
                empresa, anoMes, anoMesInicial: null, codColigada, codFilial, out var mes, out _) is { } erro)
        {
            return Serializar(erro);
        }

        var (achada, erroEmpresa) = await LocalizarEmpresa.ExecutarAsync(
            empresasService, empresa!, codColigada, codFilial, cancellationToken);
        if (achada is null)
        {
            return Serializar(erroEmpresa!);
        }
        if (LocalizarEmpresa.NoFuturo(mes))
        {
            return Serializar(LocalizarEmpresa.SemDados(achada, LocalizarEmpresa.Periodo(mes, mes), LocalizarEmpresa.MotivoNoFuturo(mes)));
        }

        // Os dois meses ao mesmo tempo: o anterior num escopo próprio, porque o DbContext do escopo
        // atual não aceita duas consultas juntas.
        var cronometro = Stopwatch.StartNew();
        using var escopoAnterior = escopos.CreateScope();
        var tarefaAtual = balancoService.CalcularAsync(mes, achada.CodColigada, achada.CodFilial, cancellationToken);
        var tarefaAnterior = escopoAnterior.ServiceProvider.GetRequiredService<IBalancoService>().CalcularAsync(
            mes.AddMonths(-1), achada.CodColigada, achada.CodFilial, cancellationToken);
        await Task.WhenAll(tarefaAtual, tarefaAnterior);
        var atual = await tarefaAtual;
        var anterior = await tarefaAnterior;
        logger.LogInformation(
            "CFO Tati: balanço de {Empresa} (coligada {Coligada}, {Mes:yyyy-MM} e o mês anterior) calculado em {DuracaoMs} ms",
            achada.Nome, achada.CodColigada, mes, cronometro.ElapsedMilliseconds);

        return Serializar(MontarResposta(achada, mes, detalhado, atual, anterior));
    }

    /// <summary>
    /// O JSON enxuto a partir dos dois balanços do <see cref="IBalancoService"/>. Valores como na
    /// tela: ativo e passivo positivos; no PL, prejuízo negativo. Sem saldos no mês anterior, o
    /// valor anterior fica nulo em vez de zero.
    /// </summary>
    public static object MontarResposta(
        LocalizarEmpresa.Empresa empresa, DateTime mes, bool detalhado, BalancoViewModel atual, BalancoViewModel? anterior)
    {
        var periodo = LocalizarEmpresa.Periodo(mes, mes);
        if (SemDados(atual))
        {
            return LocalizarEmpresa.SemDados(empresa, periodo, "Não há saldos dessa empresa no fim desse mês.");
        }

        var blocos = Blocos(atual);
        var blocosAnteriores = anterior is null || SemDados(anterior)
            ? null
            : Blocos(anterior).ToDictionary(b => (b.Lado, b.Nome));

        decimal? Pl(List<Bloco> lista) => lista.FirstOrDefault(b => b.Lado == "passivo" && b.Nome == PatrimonioLiquido)?.Valor;
        var plAtual = Pl(blocos) ?? 0;
        var plAnterior = blocosAnteriores is null ? null : Pl(blocosAnteriores.Values.ToList());
        var temAnterior = blocosAnteriores is not null;

        return new
        {
            empresa = empresa.Nome,
            coligada = empresa.CodColigada,
            filial = empresa.CodFilial,
            abrangencia = empresa.Abrangencia,
            filiaisDaColigada = empresa.FiliaisDaColigada,
            periodo,
            periodoAnterior = new
            {
                mes = mes.AddMonths(-1).ToString("yyyy-MM", CultureInfo.InvariantCulture),
                semDados = !temAnterior,
            },
            convencaoDeSinais = "como na tela: ativo e passivo positivos; no patrimônio líquido, prejuízo negativo",
            totais = new
            {
                ativo = Comparar(atual.TotalAtivo, temAnterior ? anterior!.TotalAtivo : null),
                passivoExigivel = Comparar(
                    atual.TotalPassivo - plAtual, temAnterior ? anterior!.TotalPassivo - (plAnterior ?? 0) : null),
                patrimonioLiquido = Comparar(plAtual, plAnterior),
                // O "PASSIVO" da tela: passivo exigível + PL; fecha com o ativo.
                passivoMaisPl = Comparar(atual.TotalPassivo, temAnterior ? anterior!.TotalPassivo : null),
            },
            diferencaAtivoMenosPassivo = LocalizarEmpresa.Reais(atual.TotalAtivo - atual.TotalPassivo),
            blocos = blocos.Select(b =>
            {
                var antes = blocosAnteriores?.GetValueOrDefault((b.Lado, b.Nome));
                var comparado = Comparar(b.Valor, temAnterior ? antes?.Valor ?? 0 : null);
                if (!detalhado)
                {
                    return (object)new { lado = b.Lado, bloco = b.Nome, comparado.valor, comparado.valorAnterior, comparado.variacao };
                }

                return new
                {
                    lado = b.Lado,
                    bloco = b.Nome,
                    comparado.valor,
                    comparado.valorAnterior,
                    comparado.variacao,
                    grupos = b.Grupos.Where(g => g.Nome is not null).Select(g =>
                    {
                        var grupoAntes = antes?.Grupos.FirstOrDefault(x => x.Nome == g.Nome);
                        var grupoComparado = Comparar(g.Valor, temAnterior ? grupoAntes?.Valor ?? 0 : null);
                        return new
                        {
                            grupo = g.Nome,
                            grupoComparado.valor,
                            grupoComparado.valorAnterior,
                            grupoComparado.variacao,
                            linhas = Linhas(g.Linhas, temAnterior ? grupoAntes?.Linhas ?? [] : null),
                        };
                    }),
                    // Linhas que somam direto no bloco, sem subtotal (Investimentos, Imobilizado, Capital Social...).
                    linhas = Linhas(
                        b.Grupos.Where(g => g.Nome is null).SelectMany(g => g.Linhas).ToList(),
                        temAnterior ? antes?.Grupos.Where(g => g.Nome is null).SelectMany(g => g.Linhas).ToList() ?? [] : null),
                };
            }),
        };
    }

    private sealed record Grupo(string? Nome, decimal Valor, List<BalancoLinhaViewModel> Linhas);

    private sealed record Bloco(string Lado, string Nome, decimal Valor, List<Grupo> Grupos);

    /// <summary>
    /// As linhas da tela vêm em ordem: as de um grupo e depois o subtotal ("grupo"); os grupos de um
    /// bloco e depois o total dele ("bloco"). Linhas sem subtotal ficam num grupo sem nome.
    /// </summary>
    private static List<Bloco> Blocos(BalancoViewModel balanco)
    {
        var blocos = new List<Bloco>();
        foreach (var (lado, linhas) in new[] { ("ativo", balanco.Ativo), ("passivo", balanco.Passivo) })
        {
            var grupos = new List<Grupo>();
            var soltas = new List<BalancoLinhaViewModel>();
            foreach (var linha in linhas)
            {
                switch (linha.Tipo)
                {
                    case "linha":
                        soltas.Add(linha);
                        break;
                    case "grupo":
                        grupos.Add(new Grupo(linha.Descricao, linha.Valor, soltas));
                        soltas = [];
                        break;
                    case "bloco":
                        if (soltas.Count > 0)
                        {
                            grupos.Add(new Grupo(null, soltas.Sum(l => l.Valor), soltas));
                        }
                        blocos.Add(new Bloco(lado, linha.Descricao, linha.Valor, grupos));
                        grupos = [];
                        soltas = [];
                        break;
                }
            }
        }
        return blocos;
    }

    /// <summary>
    /// As linhas do mês com o valor do mês anterior. A tela esconde linha zerada, então uma linha
    /// que só existe num dos meses vale zero no outro.
    /// </summary>
    private static IEnumerable<object> Linhas(List<BalancoLinhaViewModel> atuais, List<BalancoLinhaViewModel>? anteriores)
    {
        foreach (var linha in atuais)
        {
            var antes = anteriores?.FirstOrDefault(l => l.Descricao == linha.Descricao);
            var comparado = Comparar(linha.Valor, anteriores is null ? null : antes?.Valor ?? 0);
            yield return new { linha = linha.Descricao, comparado.valor, comparado.valorAnterior, comparado.variacao };
        }
        foreach (var antes in anteriores?.Where(l => atuais.All(a => a.Descricao != l.Descricao)) ?? [])
        {
            var comparado = Comparar(0, antes.Valor);
            yield return new { linha = antes.Descricao, comparado.valor, comparado.valorAnterior, comparado.variacao };
        }
    }

    // Nomes em minúscula: viram os campos do JSON.
    private sealed record Comparado(decimal valor, decimal? valorAnterior, decimal? variacao);

    private static Comparado Comparar(decimal valor, decimal? anterior) => new(
        LocalizarEmpresa.Reais(valor),
        anterior is { } a ? LocalizarEmpresa.Reais(a) : null,
        anterior is { } b ? LocalizarEmpresa.Reais(valor - b) : null);

    /// <summary>Sem nenhuma linha com valor nos dois lados: a empresa não tinha saldos nesse mês.</summary>
    private static bool SemDados(BalancoViewModel balanco) =>
        !balanco.Ativo.Concat(balanco.Passivo).Any(l => l.Tipo == "linha" && l.Valor != 0);

    private static string Serializar(object valor) => JsonSerializer.Serialize(valor, OpcoesJson);
}
