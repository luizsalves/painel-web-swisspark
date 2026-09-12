namespace PainelWebSwissPark.Api.Entities;

/// <summary>
/// Trilha de auditoria das alterações feitas na bi_users pela área administrativa.
/// Alimenta o item "Auditoria" do menu quando a tela existir.
/// </summary>
public class BiUserAudit
{
    public long Id { get; set; }

    /// <summary>Usuário que sofreu a alteração.</summary>
    public Guid TargetUserId { get; set; }

    /// <summary>Ver <see cref="AcoesAuditoria"/>.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Resumo legível do que mudou (ex.: "role: Analista -> Gerente").</summary>
    public string? Details { get; set; }

    /// <summary>Administrador que executou a alteração.</summary>
    public Guid? PerformedBy { get; set; }

    public DateTimeOffset PerformedAt { get; set; }
}

public static class AcoesAuditoria
{
    public const string Criado = "criado";
    public const string Atualizado = "atualizado";
    public const string Ativado = "ativado";
    public const string Desativado = "desativado";
    public const string PrimeiroLogin = "primeiro-login";
}
