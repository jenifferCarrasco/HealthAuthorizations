using HealthAuthorization.Domain.Common.Entities;

namespace HealthAuthorization.Domain.Entities;

public sealed class ProviderType : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
