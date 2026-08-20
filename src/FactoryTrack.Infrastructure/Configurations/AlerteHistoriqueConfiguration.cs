using FactoryTrack.Domain.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FactoryTrack.Infrastructure.Configurations;

public class AlerteHistoriqueConfiguration : IEntityTypeConfiguration<AlerteHistorique>
{
    public void Configure(EntityTypeBuilder<AlerteHistorique> builder)
    {
        builder.ToTable("alertes_historique");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.BaliseIdentifiant).HasMaxLength(100).IsRequired();
        builder.Property(a => a.CodeEquipement).HasMaxLength(50).IsRequired();
        builder.Property(a => a.ZoneNom).HasMaxLength(200).IsRequired();

        builder.HasIndex(a => a.Horodatage);
        builder.HasIndex(a => new { a.BaliseIdentifiant, a.Horodatage });
        builder.HasIndex(a => new { a.ZoneId, a.Horodatage });
    }
}
