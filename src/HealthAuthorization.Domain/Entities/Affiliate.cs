using HealthAuthorization.Domain.Common.Entities;
using HealthAuthorization.Domain.Enums;

namespace HealthAuthorization.Domain.Entities;

public sealed class Affiliate : AuditableEntity
{
    public string AffiliateNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Identification { get; set; }
    public DocumentType DocumentType { get; set; } = DocumentType.None;
    public DateOnly BirthDate { get; set; }
    public Guid? ContractorId { get; set; }
    public Contractor? Contractor { get; set; }
}
