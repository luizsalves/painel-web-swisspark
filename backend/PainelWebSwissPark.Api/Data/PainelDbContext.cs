using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PainelWebSwissPark.Api.Entities;
using PainelWebSwissPark.Api.Options;

namespace PainelWebSwissPark.Api.Data;

/// <summary>
/// Contexto do Postgres do painel (hoje o banco contabil). Só as tabelas do próprio
/// painel moram aqui — nada do ETL do Pentaho.
/// </summary>
public class PainelDbContext(
    DbContextOptions<PainelDbContext> options,
    IOptions<PainelDbOptions> painelDbOptions) : DbContext(options)
{
    private readonly string _schema = painelDbOptions.Value.Schema;

    public DbSet<BiUser> BiUsers => Set<BiUser>();

    public DbSet<BiUserAudit> BiUserAudits => Set<BiUserAudit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BiUser>(entity =>
        {
            entity.ToTable("bi_users", _schema);
            entity.HasKey(e => e.Id);

            // Sem HasDefaultValueSql: gen_random_uuid() não existe em Postgres
            // anterior ao 13 sem a pgcrypto. O id é gerado na aplicação.
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.Email).HasColumnName("email").IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash").IsRequired();
            entity.Property(e => e.MustChangePassword).HasColumnName("must_change_password").HasDefaultValue(true);
            entity.Property(e => e.Role).HasColumnName("role").IsRequired();
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.LastLoginAt).HasColumnName("last_login_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        });

        modelBuilder.Entity<BiUserAudit>(entity =>
        {
            entity.ToTable("bi_users_audit", _schema);
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.TargetUserId).HasColumnName("target_user_id").IsRequired();
            entity.Property(e => e.Action).HasColumnName("action").IsRequired();
            entity.Property(e => e.Details).HasColumnName("details");
            entity.Property(e => e.PerformedBy).HasColumnName("performed_by");
            entity.Property(e => e.PerformedAt).HasColumnName("performed_at").HasDefaultValueSql("now()");

            entity.HasIndex(e => e.TargetUserId);
            entity.HasIndex(e => e.PerformedAt);
        });
    }
}
