using FactoryTrack.Domain.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FactoryTrack.Infrastructure.Configurations;

/// <summary>
/// Table convertie en hypertable TimescaleDB par db/init/01-schema.sql.
/// La cle primaire inclut l'horodatage : Timescale exige que la colonne de
/// partitionnement fasse partie de toute contrainte unique.
/// </summary>
public class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> builder)
    {
        builder.ToTable("positions");
        builder.HasKey(p => new { p.BaliseId, p.Horodatage });

        builder.Property(p => p.BaliseIdentifiant).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Technologie).HasConversion<int>();

        builder.HasIndex(p => new { p.Etage, p.Horodatage });
    }
}
