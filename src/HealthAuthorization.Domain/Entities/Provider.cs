using HealthAuthorization.Domain.Common.Entities;

namespace HealthAuthorization.Domain.Entities;

public sealed class Provider : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Identification { get; set; } = string.Empty;
    public Guid ProviderTypeId { get; set; }
    public ProviderType ProviderType { get; set; } = null!;
}
