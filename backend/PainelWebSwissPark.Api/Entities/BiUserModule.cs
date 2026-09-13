namespace PainelWebSwissPark.Api.Entities;

/// <summary>
/// O perfil de uma pessoa DENTRO de um módulo. Alguém pode ser Administrador na
/// conciliação e Analista no outro sistema — ou não existir no outro sistema.
///
/// É por isso que o perfil não mora na bi_users: ele não é uma propriedade da
/// pessoa, é da relação entre a pessoa e o módulo.
/// </summary>
public class BiUserModule
{
    public Guid UserId { get; set; }

    public string ModuleKey { get; set; } = string.Empty;

    public string Role { get; set; } = PerfisUsuario.Analista;

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public BiUser? User { get; set; }
}
