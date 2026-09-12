namespace PainelWebSwissPark.Api.Exceptions;

/// <summary>
/// Erro previsto de regra de negócio — a mensagem é escrita para o usuário final
/// e vai inteira para a resposta HTTP 400.
/// </summary>
public class RegraDeNegocioException(string message) : Exception(message)
{
}
