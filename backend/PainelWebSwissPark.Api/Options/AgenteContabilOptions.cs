namespace PainelWebSwissPark.Api.Options;

/// <summary>
/// Limites do ciclo da CFO Tati (seção AgenteContabil do appsettings). Sem a seção, valem os
/// padrões daqui.
/// </summary>
public class AgenteContabilOptions
{
    public const string SectionName = "AgenteContabil";

    /// <summary>
    /// Idas ao modelo por resposta. Cada ida pode pedir várias ferramentas de uma vez; é só um
    /// limite de segurança contra o modelo entrar num laço de chamadas.
    /// </summary>
    public int MaxIteracoes { get; init; } = 6;

    /// <summary>
    /// Tempo máximo de uma resposta inteira, com as idas ao modelo e as ferramentas. 90 s porque uma
    /// consulta só, numa coligada grande, já levou 65 s; o livro do projeto pedia 60.
    /// </summary>
    public int TempoLimiteSegundos { get; init; } = 90;

    /// <summary>
    /// Qual AgenteContabil/Instrucoes/instrucoes-{versão}.md a Tati segue. As versões antigas
    /// continuam no build, para dar para voltar trocando só este valor.
    /// </summary>
    public string VersaoInstrucoes { get; init; } = "v3";
}
