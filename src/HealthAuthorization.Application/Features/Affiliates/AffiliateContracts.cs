using HealthAuthorization.Domain.Enums;

namespace HealthAuthorization.Application.Features.Affiliates;

public class AffiliateInput
{
    public string AffiliateNumber { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateOnly BirthDate { get; set; }
    public string? Identification { get; set; }
    public DocumentType DocumentType { get; set; }
    public Guid? ContractorId { get; set; }

    public AffiliateInput(string AffiliateNumber, string FirstName, string LastName, DateOnly BirthDate, string? Identification = null, DocumentType DocumentType = DocumentType.None, Guid? ContractorId = null)
    {
        this.AffiliateNumber = AffiliateNumber;
        this.FirstName = FirstName;
        this.LastName = LastName;
        this.BirthDate = BirthDate;
        this.Identification = Identification;
        this.DocumentType = DocumentType;
        this.ContractorId = ContractorId;
    }
}

public class AffiliateDto
{
    public Guid Id { get; set; }
    public string AffiliateNumber { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateOnly BirthDate { get; set; }
    public string? Identification { get; set; }
    public DocumentType DocumentType { get; set; }
    public Guid? ContractorId { get; set; }
    public bool IsActive { get; set; }

    public AffiliateDto(Guid Id, string AffiliateNumber, string FirstName, string LastName, DateOnly BirthDate, string? Identification, DocumentType DocumentType, Guid? ContractorId, bool IsActive)
    {
        this.Id = Id;
        this.AffiliateNumber = AffiliateNumber;
        this.FirstName = FirstName;
        this.LastName = LastName;
        this.BirthDate = BirthDate;
        this.Identification = Identification;
        this.DocumentType = DocumentType;
        this.ContractorId = ContractorId;
        this.IsActive = IsActive;
    }
}
