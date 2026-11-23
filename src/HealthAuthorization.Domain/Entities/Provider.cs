using HealthAuthorization.Domain.Common.Entities;
using HealthAuthorization.Domain.Common.Interfaces;

namespace HealthAuthorization.Domain.Entities;

public sealed class Provider : AuditableEntity, ISoftDelete, IIsActive
{
    public bool IsActive { get; set; } = true;
    public bool IsDelete { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Identification { get; set; } = string.Empty;
    public Guid ProviderTypeId { get; set; }
    public ProviderType ProviderType { get; set; } = null!;
}
