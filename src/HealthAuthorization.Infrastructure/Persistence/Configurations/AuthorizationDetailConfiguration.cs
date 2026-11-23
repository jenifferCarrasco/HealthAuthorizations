using HealthAuthorization.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthAuthorization.Infrastructure.Persistence.Configurations;

internal sealed class AuthorizationDetailConfiguration : IEntityTypeConfiguration<AuthorizationDetail>
{
    public void Configure(EntityTypeBuilder<AuthorizationDetail> builder)
    {
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.ToTable("AuthorizationDetails", table => table.HasCheckConstraint("CK_AuthorizationDetails_Amounts",
            "[ClaimedAmount] >= 0 AND [ArsAmount] >= 0 AND [AffiliateAmount] >= 0 AND [ClaimedAmount] = [ArsAmount] + [AffiliateAmount]"));
        builder.Property(entity => entity.ClaimedAmount).HasPrecision(19, 2);
        builder.Property(entity => entity.ArsAmount).HasPrecision(19, 2);
        builder.Property(entity => entity.AffiliateAmount).HasPrecision(19, 2);
        builder.HasOne(entity => entity.Provider).WithMany().HasForeignKey(entity => entity.ProviderId)
            .IsRequired().OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(entity => entity.ServiceType).WithMany().HasForeignKey(entity => entity.ServiceTypeId)
            .IsRequired().OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(entity => entity.AuthorizationId).HasFilter("[IsDelete] = 0");
    }
}
