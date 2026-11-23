using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthAuthorization.Infrastructure.Persistence.Configurations;

internal sealed class IntermediaryConfiguration : IEntityTypeConfiguration<Intermediary>
{
    public void Configure(EntityTypeBuilder<Intermediary> builder)
    {
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.Name).IsRequired().HasMaxLength(InputLimits.Name);
        builder.Property(entity => entity.Identification).IsRequired().HasMaxLength(InputLimits.Code);
        builder.HasData(new { Id = CatalogSeed.IntermediaryId, Name = "Intermediario de demostración",
            Identification = "DEMO-INTERMEDIARY", CreatedDate = CatalogSeed.CreatedDate });
    }
}
