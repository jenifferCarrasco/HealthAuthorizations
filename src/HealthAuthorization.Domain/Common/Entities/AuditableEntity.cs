using HealthAuthorization.Domain.Common.Interfaces;

namespace HealthAuthorization.Domain.Common.Entities;

public abstract class AuditableEntity : BaseEntity, IAuditable
{
    public DateTimeOffset CreatedDate { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? LastUpdatedDate { get; set; }
    public Guid? LastUpdatedBy { get; set; }
}
