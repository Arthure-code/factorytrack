using FactoryTrack.Domain.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FactoryTrack.Infrastructure.Configurations;

public class EquipementConfiguration : IEntityTypeConfiguration<Equipement>
{
    public void Configure(EntityTypeBuilder<Equipement> builder)
    {
        builder.ToTable("equipements");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Nom).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Categorie).HasMaxLength(100);
        builder.Property(e => e.Etat).HasConversion<int>();

        builder.HasIndex(e => e.Code).IsUnique();

        builder.HasOne(e => e.Balise)
               .WithMany()
               .HasForeignKey(e => e.BaliseId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
