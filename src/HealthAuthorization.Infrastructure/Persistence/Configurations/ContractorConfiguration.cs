using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthAuthorization.Infrastructure.Persistence.Configurations;

internal class ContractorConfiguration : IEntityTypeConfiguration<Contractor>
{
    public void Configure(EntityTypeBuilder<Contractor> builder)
    {
        builder.Property(entity => entity.IsActive).HasDefaultValue(true).HasSentinel(true);
        builder.Property(entity => entity.IsDelete).HasDefaultValue(false).IsConcurrencyToken();
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.Name).IsRequired().HasMaxLength(InputLimits.Name);
        builder.Property(entity => entity.Identification).IsRequired().HasMaxLength(InputLimits.Code);
        builder.HasData(new { Id = CatalogSeed.ContractorId, Name = "Contratante de demostración",
            Identification = "DEMO-CONTRACTOR", IsActive = true, CreatedDate = CatalogSeed.CreatedDate });
    }
}
