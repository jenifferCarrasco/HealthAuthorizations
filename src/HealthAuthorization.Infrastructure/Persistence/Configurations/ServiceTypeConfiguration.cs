using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthAuthorization.Infrastructure.Persistence.Configurations;

internal class ServiceTypeConfiguration : IEntityTypeConfiguration<ServiceType>
{
    public void Configure(EntityTypeBuilder<ServiceType> builder)
    {
        builder.Property(entity => entity.IsActive).HasDefaultValue(true).HasSentinel(true);
        builder.Property(entity => entity.IsDelete).HasDefaultValue(false).IsConcurrencyToken();
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.Name).IsRequired().HasMaxLength(InputLimits.Name);
        builder.Property(entity => entity.Description).HasMaxLength(InputLimits.Description);
        builder.HasData(CatalogSeed.ServiceTypes.Select(type => new
        {
            Id = CatalogSeed.Id(type.Number), type.Name, IsActive = true, CreatedDate = CatalogSeed.CreatedDate
        }));
    }
}
