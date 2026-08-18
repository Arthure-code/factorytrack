using FactoryTrack.Domain.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FactoryTrack.Infrastructure.Configurations;

public class BaliseConfiguration : IEntityTypeConfiguration<Balise>
{
    public void Configure(EntityTypeBuilder<Balise> builder)
    {
        builder.ToTable("balises");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Identifiant).HasMaxLength(100).IsRequired();
        builder.Property(b => b.Technologie).HasConversion<int>();

        builder.HasIndex(b => b.Identifiant).IsUnique();
    }
}
