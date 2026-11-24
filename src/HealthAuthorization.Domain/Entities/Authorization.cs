using HealthAuthorization.Domain.Common.Entities;
using HealthAuthorization.Domain.Common.Interfaces;
using HealthAuthorization.Domain.Policies;

namespace HealthAuthorization.Domain.Entities;

public class Authorization : AuditableEntity, IIsActive
{
    public Guid AffiliateId { get; set; }
    public Affiliate Affiliate { get; set; } = null!;
    public Guid? ContractorId { get; set; }
    public Contractor? Contractor { get; set; }
    public Guid? IntermediaryId { get; set; }
    public Intermediary? Intermediary { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? IdempotencyScopeId { get; set; }
    public Guid? IdempotencyKey { get; set; }
    public byte[]? IdempotencyRequestHash { get; set; }
    public ICollection<AuthorizationDetail> Details { get; set; } = new List<AuthorizationDetail>();

    public decimal TotalClaimedAmount => Details.Where(detail => !detail.IsDelete).Aggregate(0m,
        (total, detail) => CoveragePolicy.AddExact(total, detail.ClaimedAmount));

    public decimal TotalArsAmount => Details.Where(detail => !detail.IsDelete).Aggregate(0m,
        (total, detail) => CoveragePolicy.AddExact(total, detail.ArsAmount));

    public decimal TotalAffiliateAmount => Details.Where(detail => !detail.IsDelete).Aggregate(0m,
        (total, detail) => CoveragePolicy.AddExact(total, detail.AffiliateAmount));
}
