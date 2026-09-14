namespace PainelWebSwissPark.Api.Entities;

/// <summary>
/// Uma linha da trilha de preparação/conferência/fechamento de uma conta num mês.
/// Nunca é alterada — cada ação (Preparar, Conferir, Realizar Fechamento, Reverter)
/// grava uma linha nova. O status "atual" de uma conta/mês é o status da linha
/// mais recente para aquela chave.
/// </summary>
public class FechamentoValidacao
{
    public long Id { get; set; }

    public short CodColigada { get; set; }
    public short CodFilial { get; set; }
    public string CodConta { get; set; } = string.Empty;

    /// <summary>Formato AAAA-MM.</summary>
    public string AnoMes { get; set; } = string.Empty;

    /// <summary>Ver <see cref="StatusFechamento"/>.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>1 (mais alta) a 4. Carregada adiante entre as ações quando não reinformada.</summary>
    public short? Prioridade { get; set; }

    public decimal? SaldoAnterior { get; set; }
    public decimal? SaldoAtual { get; set; }

    /// <summary>Primeiro dia do período (AnoMes) que está sendo fechado.</summary>
    public DateOnly? DataFechamento { get; set; }

    public string? PendenciaTexto { get; set; }
    public Guid? PendenciaResponsavelId { get; set; }
    public DateTimeOffset? PendenciaRegistradaEm { get; set; }

    /// <summary>Quem executou esta ação especificamente (não é "quem preparou", é quem gravou ESTA linha).</summary>
    public Guid RealizadoPor { get; set; }
    public DateTimeOffset RealizadoEm { get; set; }
}

public static class StatusFechamento
{
    /// <summary>Nenhuma ação ainda — estado inicial, não tem linha no banco.</summary>
    public const string Pendente = "Pendente";
    public const string Preparado = "Preparado";
    public const string Conferido = "Conferido";
    public const string Fechado = "Fechado";
}
