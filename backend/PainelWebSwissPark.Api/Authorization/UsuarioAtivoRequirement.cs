using Microsoft.AspNetCore.Authorization;

namespace PainelWebSwissPark.Api.Authorization;

/// <summary>
/// Exige que quem apresentou o token do Entra também esteja cadastrado e ativo na bi_users.
/// É essa checagem, feita a cada requisição, que torna a revogação imediata: o administrador
/// desativa na tela e o próximo request já toma 403 — sem esperar token nenhum expirar.
/// </summary>
public class UsuarioAtivoRequirement : IAuthorizationRequirement
{
}
