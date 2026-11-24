using HealthAuthorization.Domain.Common.Entities;
using HealthAuthorization.Domain.Common.Interfaces;

namespace HealthAuthorization.Domain.Entities;

public sealed class AuthorizationDetail : BaseEntity, ISoftDelete
{
    public Guid AuthorizationId { get; set; }
    public Authorization Authorization { get; set; } = null!;
    public Guid ProviderId { get; set; }
    public Provider Provider { get; set; } = null!;
    public Guid ServiceTypeId { get; set; }
    public ServiceType ServiceType { get; set; } = null!;
    public decimal ClaimedAmount { get; set; }
    public decimal ArsAmount { get; set; }
    public decimal AffiliateAmount { get; set; }
    public bool IsDelete { get; set; }
}
