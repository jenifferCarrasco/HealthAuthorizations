using HealthAuthorization.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthAuthorization.Infrastructure.Persistence.Configurations;

internal sealed class AuthorizationConfiguration : IEntityTypeConfiguration<Authorization>
{
    public void Configure(EntityTypeBuilder<Authorization> builder)
    {
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property<byte[]>("RowVersion").IsRequired().IsRowVersion();
        builder.Ignore(entity => entity.TotalClaimedAmount);
        builder.Ignore(entity => entity.TotalArsAmount);
        builder.Ignore(entity => entity.TotalAffiliateAmount);
        builder.HasOne(entity => entity.Affiliate).WithMany().HasForeignKey(entity => entity.AffiliateId)
            .IsRequired().OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(entity => entity.Contractor).WithMany().HasForeignKey(entity => entity.ContractorId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(entity => entity.Intermediary).WithMany().HasForeignKey(entity => entity.IntermediaryId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasMany(entity => entity.Details).WithOne(detail => detail.Authorization)
            .HasForeignKey(detail => detail.AuthorizationId).IsRequired().OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(entity => new { entity.CreatedDate, entity.Id }).IsDescending(true, true);
        builder.HasIndex(entity => new { entity.AffiliateId, entity.CreatedDate, entity.Id }).IsDescending(false, true, true);
        builder.HasIndex(entity => new { entity.IsActive, entity.CreatedDate, entity.Id }).IsDescending(false, true, true);
    }
}
