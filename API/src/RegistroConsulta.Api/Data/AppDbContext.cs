using Microsoft.EntityFrameworkCore;
using RegistroConsulta.Api.Entities;

namespace RegistroConsulta.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Entidad> Entidades => Set<Entidad>();
    public DbSet<RegistroCivil> Registros => Set<RegistroCivil>();
    public DbSet<LogConsulta> Logs => Set<LogConsulta>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Entidad>(e =>
        {
            e.ToTable("Entidades");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nombre).HasMaxLength(200).IsRequired();
            e.Property(x => x.CuotaDiaria).IsRequired();
        });

        modelBuilder.Entity<RegistroCivil>(e =>
        {
            e.ToTable("Registros");
            e.HasKey(x => x.Id);
            e.Property(x => x.Identificador).HasMaxLength(50).IsRequired();
            e.Property(x => x.Nombre).HasMaxLength(200).IsRequired();
            e.Property(x => x.Estado).HasMaxLength(50).IsRequired();
            e.Property(x => x.NumeroRegistro).HasMaxLength(50).IsRequired();
            // Índice compuesto: las búsquedas siempre son por identificador + nombre.
            e.HasIndex(x => new { x.Identificador, x.Nombre });
        });

        modelBuilder.Entity<LogConsulta>(e =>
        {
            e.ToTable("LogsConsulta");
            e.HasKey(x => x.Id);
            e.Property(x => x.Motivo).HasMaxLength(100).IsRequired();
            e.HasOne(x => x.Entidad)
                .WithMany(x => x.Logs)
                .HasForeignKey(x => x.EntidadId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => new { x.EntidadId, x.FechaHora });
        });
    }
}
