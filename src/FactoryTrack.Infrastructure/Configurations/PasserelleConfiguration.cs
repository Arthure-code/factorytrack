using FactoryTrack.Domain.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FactoryTrack.Infrastructure.Configurations;

public class PasserelleConfiguration : IEntityTypeConfiguration<Passerelle>
{
    public void Configure(EntityTypeBuilder<Passerelle> builder)
    {
        builder.ToTable("passerelles");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Identifiant).HasMaxLength(100).IsRequired();
        builder.HasIndex(p => p.Identifiant).IsUnique();
    }
}
