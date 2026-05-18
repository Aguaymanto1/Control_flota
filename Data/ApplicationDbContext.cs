using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Control_flota.Models.Operaciones;
using Control_flota.Models.Login;

namespace Control_flota.Data;

public class ApplicationDbContext : IdentityDbContext<Usuario>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    public DbSet<Unidad> Unidades => Set<Unidad>();
    public DbSet<Conductor> Conductores => Set<Conductor>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<SolicitudServicio> SolicitudesServicio => Set<SolicitudServicio>();
    public DbSet<Orden> Ordenes => Set<Orden>();

    public DbSet<Inspeccion> Inspecciones { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Unidad>()
            .HasIndex(u => u.Placa)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.Entity<Conductor>()
            .HasIndex(c => c.Dni)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.Entity<Conductor>()
            .HasIndex(c => c.NumeroLicencia)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.Entity<SolicitudServicio>()
            .HasIndex(s => s.Codigo)
            .IsUnique();

        builder.Entity<Orden>()
            .HasIndex(o => o.Codigo)
            .IsUnique();

        builder.Entity<Usuario>()
            .HasOne(u => u.Conductor)
            .WithMany()
            .HasForeignKey(u => u.ConductorId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
