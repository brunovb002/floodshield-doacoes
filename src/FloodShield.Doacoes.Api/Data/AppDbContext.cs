using FloodShield.Doacoes.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FloodShield.Doacoes.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<PontoColeta> PontosColeta => Set<PontoColeta>();
    public DbSet<Item> Itens => Set<Item>();
    public DbSet<Necessidade> Necessidades => Set<Necessidade>();
    public DbSet<Doacao> Doacoes => Set<Doacao>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(20);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<Item>().HasIndex(i => i.Nome).IsUnique();
        modelBuilder.Entity<Doacao>().HasIndex(d => d.CodigoEntrega).IsUnique();

        modelBuilder.Entity<Necessidade>()
            .HasOne(n => n.PontoColeta)
            .WithMany(p => p.Necessidades)
            .HasForeignKey(n => n.PontoColetaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Necessidade>()
            .HasOne(n => n.Item)
            .WithMany()
            .HasForeignKey(n => n.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Doacao>()
            .HasOne(d => d.Necessidade)
            .WithMany(n => n.Doacoes)
            .HasForeignKey(d => d.NecessidadeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Doacao>()
            .HasOne(d => d.Usuario)
            .WithMany(u => u.Doacoes)
            .HasForeignKey(d => d.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
