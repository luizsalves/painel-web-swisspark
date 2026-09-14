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

    public DbSet<BiUserModule> BiUserModules => Set<BiUserModule>();

    public DbSet<BiModule> BiModules => Set<BiModule>();

    public DbSet<BiUserAudit> BiUserAudits => Set<BiUserAudit>();

    public DbSet<FechamentoValidacao> FechamentoValidacoes => Set<FechamentoValidacao>();

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
            entity.Property(e => e.IsSuperAdmin).HasColumnName("is_superadmin").HasDefaultValue(false);
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.LastLoginAt).HasColumnName("last_login_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasMany(e => e.Modules)
                  .WithOne(m => m.User!)
                  .HasForeignKey(m => m.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BiUserModule>(entity =>
        {
            entity.ToTable("bi_user_modules", _schema);

            // Uma pessoa tem no máximo um perfil por módulo.
            entity.HasKey(e => new { e.UserId, e.ModuleKey });

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ModuleKey).HasColumnName("module_key").IsRequired();
            entity.Property(e => e.Role).HasColumnName("role").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");

            entity.HasIndex(e => e.ModuleKey);
        });

        modelBuilder.Entity<BiModule>(entity =>
        {
            entity.ToTable("bi_modules", _schema);
            entity.HasKey(e => e.Key);

            entity.Property(e => e.Key).HasColumnName("key").ValueGeneratedNever();
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
        });

        modelBuilder.Entity<BiUserAudit>(entity =>
        {
            entity.ToTable("bi_users_audit", _schema);
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.TargetUserId).HasColumnName("target_user_id").IsRequired();
            entity.Property(e => e.Action).HasColumnName("action").IsRequired();
            entity.Property(e => e.Details).HasColumnName("details");
            entity.Property(e => e.ModuleKey).HasColumnName("module_key");
            entity.Property(e => e.PerformedBy).HasColumnName("performed_by");
            entity.Property(e => e.PerformedAt).HasColumnName("performed_at").HasDefaultValueSql("now()");

            entity.HasIndex(e => e.TargetUserId);
            entity.HasIndex(e => e.PerformedAt);
        });

        modelBuilder.Entity<FechamentoValidacao>(entity =>
        {
            entity.ToTable("bi_fechamento_validacao", _schema);
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.CodColigada).HasColumnName("cod_coligada");
            entity.Property(e => e.CodFilial).HasColumnName("cod_filial");
            entity.Property(e => e.CodConta).HasColumnName("cod_conta").IsRequired();
            entity.Property(e => e.AnoMes).HasColumnName("ano_mes").IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").IsRequired();
            entity.Property(e => e.Prioridade).HasColumnName("prioridade");
            entity.Property(e => e.SaldoAnterior).HasColumnName("saldo_anterior").HasColumnType("numeric(18,4)");
            entity.Property(e => e.SaldoAtual).HasColumnName("saldo_atual").HasColumnType("numeric(18,4)");
            entity.Property(e => e.DataFechamento).HasColumnName("data_fechamento");
            entity.Property(e => e.PendenciaTexto).HasColumnName("pendencia_texto");
            entity.Property(e => e.PendenciaResponsavelId).HasColumnName("pendencia_responsavel_id");
            entity.Property(e => e.PendenciaRegistradaEm).HasColumnName("pendencia_registrada_em");
            entity.Property(e => e.RealizadoPor).HasColumnName("realizado_por").IsRequired();
            entity.Property(e => e.RealizadoEm).HasColumnName("realizado_em").HasDefaultValueSql("now()");

            entity.HasIndex(e => new { e.CodColigada, e.CodFilial, e.CodConta, e.AnoMes, e.RealizadoEm });
        });
    }
}
