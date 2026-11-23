using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthAuthorization.Infrastructure.Persistence.Configurations;

internal sealed class ProviderConfiguration : IEntityTypeConfiguration<Provider>
{
    public void Configure(EntityTypeBuilder<Provider> builder)
    {
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.Name).IsRequired().HasMaxLength(InputLimits.Name);
        builder.Property(entity => entity.Identification).IsRequired().HasMaxLength(InputLimits.Code);
        builder.HasOne(entity => entity.ProviderType).WithMany().HasForeignKey(entity => entity.ProviderTypeId)
            .IsRequired().OnDelete(DeleteBehavior.NoAction);
        builder.HasData(CatalogSeed.ProviderTypes.Select(type => new
        {
            Id = CatalogSeed.Id(100 + type.Number), Name = $"Prestador de demostración ({type.Name})",
            Identification = $"DEMO-PROVIDER-{type.Number}", ProviderTypeId = CatalogSeed.Id(type.Number),
            CreatedDate = CatalogSeed.CreatedDate
        }));
    }
}
