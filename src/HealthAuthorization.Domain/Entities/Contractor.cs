using HealthAuthorization.Domain.Common.Entities;

namespace HealthAuthorization.Domain.Entities;

public sealed class Contractor : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Identification { get; set; } = string.Empty;
}
