using HealthAuthorization.Domain.Common.Entities;
using HealthAuthorization.Domain.Common.Interfaces;

namespace HealthAuthorization.Domain.Entities;

public sealed class ProviderType : AuditableEntity, ISoftDelete, IIsActive
{
    public bool IsActive { get; set; } = true;
    public bool IsDelete { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
