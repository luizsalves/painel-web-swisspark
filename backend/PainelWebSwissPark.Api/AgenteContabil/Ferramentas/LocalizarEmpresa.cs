using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using PainelWebSwissPark.Api.DTOs.ViewModels;
using PainelWebSwissPark.Api.Services.BusinessLogic;

namespace PainelWebSwissPark.Api.AgenteContabil.Ferramentas;

/// <summary>
/// O que as ferramentas das demonstrações (indicadores, balancete, balanço e DFC) fazem antes de
/// chamar o service: ler os parâmetros comuns, conferir o período e achar a empresa pelo nome. A
/// busca é tolerante — o nome inteiro primeiro e, só sem nenhum igual, um trecho; maiúsculas e
/// acentos não importam. Quando não dá para seguir, devolve o erro já pronto para o modelo.
/// </summary>
public static class LocalizarEmpresa
{
    /// <summary>
    /// A empresa achada: a coligada inteira, ou uma filial dela quando veio o codFilial. As
    /// <paramref name="Filiais"/> são todas as da coligada, como no combo de filial da tela.
    /// </summary>
    public sealed record Empresa(string Nome, short CodColigada, EmpresaViewModel? Filial, IReadOnlyList<EmpresaViewModel> Filiais)
    {
        public short? CodFilial => Filial?.CodFilial;

        public string Abrangencia => Filial is null ? "todas as filiais da coligada" : Filial.NomeFilial;

        /// <summary>Com a coligada inteira e mais de uma filial, quais são — para oferecer uma só; senão nulo.</summary>
        public IEnumerable<string>? FiliaisDaColigada => Filial is null && Filiais.Count > 1 ? Filiais.Select(f => f.NomeFilial) : null;
    }

    /// <summary>
    /// Os parâmetros que o modelo mandou. Campo ausente, nulo ou vazio vale como não informado; campo
    /// com o tipo errado não é ignorado em silêncio — vai para <see cref="Erro"/>, porque uma filial
    /// descartada mudaria o escopo da resposta sem ninguém perceber.
    /// </summary>
    public sealed class Parametros(JsonElement input)
    {
        private readonly List<string> invalidos = [];

        /// <summary>O erro para o modelo quando algum campo veio com o tipo errado; senão nulo.</summary>
        public object? Erro => invalidos.Count == 0 ? null : new
        {
            erro = $"Parâmetro inválido: {string.Join(", ", invalidos)}. codColigada, codFilial e nivel são números " +
                "inteiros; detalhado é true ou false; os demais são texto.",
        };

        public string? Texto(string nome)
        {
            if (Ausente(nome, out var valor)) return null;
            if (valor.ValueKind == JsonValueKind.String) return valor.GetString()!.Trim();

            invalidos.Add(nome);
            return null;
        }

        public short? Inteiro(string nome)
        {
            if (Ausente(nome, out var valor)) return null;
            if (valor.ValueKind == JsonValueKind.Number && valor.TryGetInt16(out var numero)) return numero;
            // O modelo às vezes manda o número entre aspas.
            if (valor.ValueKind == JsonValueKind.String
                && short.TryParse(valor.GetString(), NumberStyles.None, CultureInfo.InvariantCulture, out numero)) return numero;

            invalidos.Add(nome);
            return null;
        }

        public bool Booleano(string nome)
        {
            if (Ausente(nome, out var valor)) return false;
            if (valor.ValueKind is JsonValueKind.True or JsonValueKind.False) return valor.GetBoolean();
            if (valor.ValueKind == JsonValueKind.String && bool.TryParse(valor.GetString(), out var booleano)) return booleano;

            invalidos.Add(nome);
            return false;
        }

        private bool Ausente(string nome, out JsonElement valor) =>
            !input.TryGetProperty(nome, out valor)
            || valor.ValueKind == JsonValueKind.Null
            || (valor.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(valor.GetString()));
    }

    /// <summary>
    /// Confere empresa, período, coligada e filial como os controllers da tela conferem. Devolve o
    /// erro para o modelo, ou nulo com o primeiro e o último mês do período. Sem
    /// <paramref name="anoMesInicial"/>, o período é um mês só.
    /// </summary>
    public static object? ValidarParametros(
        string? empresa, string? anoMes, string? anoMesInicial, short? codColigada, short? codFilial,
        out DateTime mesInicial, out DateTime mesFinal)
    {
        mesInicial = mesFinal = default;

        if (empresa is null)
        {
            return new { erro = "Empresa não informada: use a do contexto da tela ou pergunte ao usuário." };
        }
        if (anoMes is null)
        {
            return new { erro = "Período não informado: anoMes no formato AAAA-MM." };
        }
        if (LerMes(anoMes) is not { } final || LerMes(anoMesInicial ?? anoMes) is not { } inicial)
        {
            return new { erro = "Período inválido — use o formato AAAA-MM." };
        }
        if (inicial > final)
        {
            return new { erro = "O mês inicial não pode ser depois do mês final." };
        }
        if (codColigada is <= 0 || codFilial is <= 0)
        {
            return new { erro = "Coligada ou filial inválida." };
        }

        mesInicial = inicial;
        mesFinal = final;
        return null;
    }

    public static bool NoFuturo(DateTime mesFinal) => mesFinal > new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

    /// <summary>A resposta sem dados, com a empresa e o período para a Tati citar; nunca zeros fingindo dado.</summary>
    public static object SemDados(Empresa empresa, object periodo, string motivo) => new
    {
        empresa = empresa.Nome,
        coligada = empresa.CodColigada,
        filial = empresa.CodFilial,
        abrangencia = empresa.Abrangencia,
        filiaisDaColigada = empresa.FiliaisDaColigada,
        periodo,
        semDados = true,
        motivo,
    };

    public static string MotivoNoFuturo(DateTime mesFinal) =>
        $"Ainda não há dados para {mesFinal:yyyy-MM}: o período está no futuro.";

    /// <summary>
    /// A empresa pelo nome (e pela coligada, quando o nome se repete), ou o erro para o modelo. Como
    /// na tela, a empresa é a coligada: com o nome da empresa e sem filial, entram todas as filiais,
    /// como no filtro "Todas". Com o nome de uma filial — uma SCP, por exemplo, que tem nome próprio
    /// —, a consulta é só daquela filial, que é o que o usuário pediu.
    /// </summary>
    public static async Task<(Empresa? Empresa, object? Erro)> ExecutarAsync(
        IEmpresasService empresasService, string empresa, short? codColigada, short? codFilial,
        CancellationToken cancellationToken)
    {
        // O nome inteiro primeiro; só sem nenhum igual, um trecho do nome.
        var procurado = EmpresasConsolidadas.Normalizar(empresa);
        if (procurado.Length == 0)
        {
            return (null, new { erro = "Empresa não informada: use a do contexto da tela ou pergunte ao usuário." });
        }

        var filiais = await empresasService.ListarAsync(cancellationToken);
        var candidatas = filiais.Where(f => EmpresasConsolidadas.Normalizar(f.Nome) == procurado).ToList();
        if (candidatas.Count == 0)
        {
            candidatas = filiais.Where(f => EmpresasConsolidadas.Normalizar(f.Nome).Contains(procurado, StringComparison.Ordinal)).ToList();
        }

        // Sem empresa com esse nome, o nome da filial: o de uma SCP pode aparecer só lá.
        var peloNomeDaFilial = candidatas.Count == 0;
        if (peloNomeDaFilial)
        {
            candidatas = filiais.Where(f => EmpresasConsolidadas.Normalizar(f.NomeFilial).Contains(procurado, StringComparison.Ordinal)).ToList();
        }
        if (codColigada is { } coligadaPedida)
        {
            candidatas = candidatas.Where(f => f.CodColigada == coligadaPedida).ToList();
        }

        // As filiais de uma coligada podem ter nomes diferentes; só coligadas diferentes são empresas diferentes.
        var coligadas = candidatas.GroupBy(f => f.CodColigada).Select(g => g.Key).ToList();
        List<EmpresaViewModel> FiliaisDa(short coligada) => filiais
            .Where(f => f.CodColigada == coligada)
            .GroupBy(f => f.CodFilial)
            .Select(g => g.First())
            .OrderBy(f => f.CodFilial)
            .ToList();

        if (coligadas.Count == 0)
        {
            return (null, new { erro = $"Não encontrei nenhuma empresa com \"{empresa}\"." });
        }
        if (coligadas.Count > 1)
        {
            // Com o mesmo texto e a coligada escolhida, a busca de novo decide entre a empresa inteira e
            // uma filial ("scp" + coligada 1 lista as SCPs); com o nome da matriz, viria a empresa inteira.
            return (null, new
            {
                erro = "Mais de uma empresa combina com esse nome — ofereça as empresas como opções numeradas e, na " +
                    "escolha, chame de novo com o MESMO texto em empresa e o codColigada escolhido.",
                empresas = coligadas.Select(c => new
                {
                    empresa = FiliaisDa(c)[0].Nome,
                    coligada = c,
                    nomesQueCombinam = candidatas
                        .Where(f => f.CodColigada == c)
                        .Select(f => peloNomeDaFilial ? f.NomeFilial : f.Nome)
                        .Distinct()
                        .Take(5),
                }),
            });
        }

        var coligada = coligadas[0];
        var filiaisDaColigada = FiliaisDa(coligada);
        // O nome da empresa é o da primeira filial (a matriz).
        var nomeDaEmpresa = filiaisDaColigada[0].Nome;

        EmpresaViewModel? filial;
        if (codFilial is { } filialPedida)
        {
            filial = filiaisDaColigada.FirstOrDefault(f => f.CodFilial == filialPedida);
            if (filial is null)
            {
                return (null, new
                {
                    erro = $"A empresa \"{nomeDaEmpresa}\" (coligada {coligada}) não tem a filial {codFilial}.",
                    filiais = filiaisDaColigada.Select(f => new { f.CodFilial, f.NomeFilial }),
                });
            }
        }
        else
        {
            var combinam = candidatas.GroupBy(f => f.CodFilial).Select(g => g.First()).OrderBy(f => f.CodFilial).ToList();
            var ehAEmpresa = combinam.Count == filiaisDaColigada.Count
                || (!peloNomeDaFilial && combinam.Any(f =>
                    EmpresasConsolidadas.Normalizar(f.Nome) == EmpresasConsolidadas.Normalizar(nomeDaEmpresa)));
            if (!ehAEmpresa && combinam.Count > 1)
            {
                return (null, new
                {
                    erro = $"\"{empresa}\" combina com mais de uma filial de {nomeDaEmpresa} (coligada {coligada}) — ofereça " +
                        "essas filiais como opções numeradas e chame de novo com codColigada e codFilial. Para a empresa " +
                        "inteira, chame com o nome da empresa e sem codFilial.",
                    filiais = combinam.Select(f => new { f.CodFilial, f.NomeFilial }),
                });
            }

            filial = ehAEmpresa ? null : combinam[0];
        }

        return (new Empresa(filial?.Nome ?? nomeDaEmpresa, coligada, filial, filiaisDaColigada), null);
    }

    /// <summary>O período como a ferramenta devolve: primeiro e último dia, em meses.</summary>
    public static object Periodo(DateTime mesInicial, DateTime mesFinal) => new
    {
        inicio = mesInicial.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        fim = mesFinal.AddMonths(1).AddDays(-1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        meses = Meses(mesInicial, mesFinal),
    };

    public static int Meses(DateTime mesInicial, DateTime mesFinal) =>
        (mesFinal.Year - mesInicial.Year) * 12 + mesFinal.Month - mesInicial.Month + 1;

    public static decimal Reais(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);

    private static DateTime? LerMes(string valor) =>
        Regex.IsMatch(valor, @"^\d{4}-(0[1-9]|1[0-2])$")
            ? DateTime.ParseExact(valor + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture)
            : null;
}
