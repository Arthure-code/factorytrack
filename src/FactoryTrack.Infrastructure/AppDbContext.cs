using FactoryTrack.Domain.Entites;
using Microsoft.EntityFrameworkCore;

namespace FactoryTrack.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Equipement> Equipements => Set<Equipement>();
    public DbSet<Balise> Balises => Set<Balise>();
    public DbSet<Passerelle> Passerelles => Set<Passerelle>();
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<MachineFixe> MachinesFixes => Set<MachineFixe>();
    public DbSet<AlerteHistorique> Alertes => Set<AlerteHistorique>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
