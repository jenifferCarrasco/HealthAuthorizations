using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;
using HealthAuthorization.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthAuthorization.Infrastructure.Persistence.Configurations;

internal sealed class AffiliateConfiguration : IEntityTypeConfiguration<Affiliate>
{
    public void Configure(EntityTypeBuilder<Affiliate> builder)
    {
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.ToTable("Affiliates", table => table.HasCheckConstraint("CK_Affiliates_Document",
            "([Identification] IS NULL AND [DocumentType] = 0) OR ([Identification] IS NOT NULL AND LEN(LTRIM(RTRIM([Identification]))) > 0 AND [DocumentType] IN (1,2,3))"));
        builder.Property(entity => entity.AffiliateNumber).IsRequired().HasMaxLength(InputLimits.Code);
        builder.Property(entity => entity.FirstName).IsRequired().HasMaxLength(InputLimits.Name);
        builder.Property(entity => entity.LastName).IsRequired().HasMaxLength(InputLimits.Name);
        builder.Property(entity => entity.Identification).HasMaxLength(InputLimits.Code);
        builder.Property(entity => entity.DocumentType).IsRequired();
        builder.Property(entity => entity.BirthDate).IsRequired();
        builder.Property<byte[]>("RowVersion").IsRequired().IsRowVersion();
        builder.HasIndex(entity => entity.AffiliateNumber).IsUnique();
        builder.HasIndex(entity => entity.Identification).IsUnique().HasFilter("[Identification] IS NOT NULL");
        builder.HasOne(entity => entity.Contractor).WithMany().HasForeignKey(entity => entity.ContractorId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasData(new
        {
            Id = CatalogSeed.AffiliateId, AffiliateNumber = "DEMO-001", FirstName = "Afiliado",
            LastName = "Demostración", BirthDate = new DateOnly(1990, 1, 1), DocumentType = DocumentType.None,
            ContractorId = CatalogSeed.ContractorId, CreatedDate = CatalogSeed.CreatedDate
        });
    }
}
