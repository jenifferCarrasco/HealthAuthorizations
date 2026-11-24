using HealthAuthorization.Domain.Common.Entities;
using HealthAuthorization.Domain.Enums;
using HealthAuthorization.Domain.Common.Interfaces;

namespace HealthAuthorization.Domain.Entities;

public sealed class Affiliate : AuditableEntity, ISoftDelete, IIsActive
{
    public bool IsActive { get; set; } = true;
    public bool IsDelete { get; set; }
    public string AffiliateNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Identification { get; set; }
    public DocumentType DocumentType { get; set; } = DocumentType.None;
    public DateOnly BirthDate { get; set; }
    public Guid? ContractorId { get; set; }
    public Contractor? Contractor { get; set; }
}
