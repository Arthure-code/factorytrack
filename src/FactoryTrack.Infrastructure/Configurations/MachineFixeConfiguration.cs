using FactoryTrack.Domain.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FactoryTrack.Infrastructure.Configurations;

public class MachineFixeConfiguration : IEntityTypeConfiguration<MachineFixe>
{
    public void Configure(EntityTypeBuilder<MachineFixe> builder)
    {
        builder.ToTable("machines_fixes");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Code).HasMaxLength(50).IsRequired();
        builder.Property(m => m.Nom).HasMaxLength(200).IsRequired();
        builder.HasIndex(m => m.Code).IsUnique();
    }
}
