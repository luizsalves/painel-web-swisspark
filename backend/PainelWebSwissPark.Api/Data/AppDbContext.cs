using Microsoft.EntityFrameworkCore;
using PainelWebSwissPark.Api.Data.Entities;

namespace PainelWebSwissPark.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<ContaContabil> ContasContabeis => Set<ContaContabil>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Fechamento> Fechamentos => Set<Fechamento>();
    public DbSet<Lancamento> Lancamentos => Set<Lancamento>();
    public DbSet<Pendencia> Pendencias => Set<Pendencia>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
