namespace PainelWebSwissPark.Api.Entities;

/// <summary>
/// Um módulo é um sistema dentro do painel. Quem pertence a um não enxerga o
/// outro — nem as telas, nem os usuários.
///
/// Fica em tabela, e não como constante no código, porque o segundo módulo ainda
/// não tem nome definido: acrescentar um é um INSERT, não um deploy.
/// </summary>
public class BiModule
{
    /// <summary>Chave usada no código e nas rotas. Ex.: "conciliacao".</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Nome exibido na tela.</summary>
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }
}

public static class Modulos
{
    /// <summary>O painel de conciliação contábil — as telas que existem hoje.</summary>
    public const string Conciliacao = "conciliacao";
}
