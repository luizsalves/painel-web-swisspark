using Microsoft.EntityFrameworkCore;
using PainelWebSwissPark.Api.Data.Entities;
using PainelWebSwissPark.Api.Data.Entities.Totvs;

namespace PainelWebSwissPark.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<ContaContabil> ContasContabeis => Set<ContaContabil>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Fechamento> Fechamentos => Set<Fechamento>();
    public DbSet<Lancamento> Lancamentos => Set<Lancamento>();
    public DbSet<Pendencia> Pendencias => Set<Pendencia>();

    // Tabela nativa do TOTVS RM — só leitura, mapeada 1:1 com o schema real do SQL Server.
    public DbSet<Gfilial> Gfiliais => Set<Gfilial>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Gfilial>(entity =>
        {
            entity.ToTable("GFILIAL");
            entity.HasKey(e => new { e.CodColigada, e.CodFilial });

            entity.Property(e => e.CodColigada).HasColumnName("CODCOLIGADA");
            entity.Property(e => e.CodFilial).HasColumnName("CODFILIAL");
            entity.Property(e => e.Nome).HasColumnName("NOME");
            entity.Property(e => e.NomeFantasia).HasColumnName("NOMEFANTASIA");
            entity.Property(e => e.Cgc).HasColumnName("CGC");
            entity.Property(e => e.Cidade).HasColumnName("CIDADE");
            entity.Property(e => e.Estado).HasColumnName("ESTADO");
            entity.Property(e => e.Ativo).HasColumnName("ATIVO");
        });

        modelBuilder.Entity<ContaContabil>()
            .HasOne(c => c.Empresa)
            .WithMany(e => e.ContasContabeis)
            .HasForeignKey(c => c.EmpresaId);

        modelBuilder.Entity<Fechamento>()
            .HasOne(f => f.Empresa)
            .WithMany(e => e.Fechamentos)
            .HasForeignKey(f => f.EmpresaId);

        modelBuilder.Entity<Fechamento>()
            .HasOne(f => f.ContaContabil)
            .WithMany(c => c.Fechamentos)
            .HasForeignKey(f => f.ContaContabilId);

        modelBuilder.Entity<Fechamento>()
            .HasOne(f => f.Responsavel)
            .WithMany(u => u.FechamentosResponsavel)
            .HasForeignKey(f => f.ResponsavelId)
            .IsRequired(false);

        modelBuilder.Entity<Fechamento>()
            .Property(f => f.SaldoRazao).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Fechamento>()
            .Property(f => f.ComposicaoSaldo).HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Lancamento>()
            .HasOne(l => l.Fechamento)
            .WithMany(f => f.Lancamentos)
            .HasForeignKey(l => l.FechamentoId);
        modelBuilder.Entity<Lancamento>()
            .Property(l => l.Valor).HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Pendencia>()
            .HasOne(p => p.Fechamento)
            .WithMany(f => f.Pendencias)
            .HasForeignKey(p => p.FechamentoId);
    }
}
