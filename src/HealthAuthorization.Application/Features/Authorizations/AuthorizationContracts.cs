namespace HealthAuthorization.Application.Features.Authorizations;

public class AuthorizationServiceInput
{
    public Guid ProviderId { get; set; }
    public Guid ServiceTypeId { get; set; }
    public decimal ClaimedAmount { get; set; }
    public Guid? DetailId { get; set; }

    public AuthorizationServiceInput(Guid ProviderId, Guid ServiceTypeId, decimal ClaimedAmount, Guid? DetailId = null)
    {
        this.ProviderId = ProviderId;
        this.ServiceTypeId = ServiceTypeId;
        this.ClaimedAmount = ClaimedAmount;
        this.DetailId = DetailId;
    }
}

public class AuthorizationInput
{
    public Guid AffiliateId { get; set; }
    public IReadOnlyList<AuthorizationServiceInput> Services { get; set; }
    public Guid? ContractorId { get; set; }
    public Guid? IntermediaryId { get; set; }

    public AuthorizationInput(Guid AffiliateId, IReadOnlyList<AuthorizationServiceInput> Services, Guid? ContractorId = null, Guid? IntermediaryId = null)
    {
        this.AffiliateId = AffiliateId;
        this.Services = Services;
        this.ContractorId = ContractorId;
        this.IntermediaryId = IntermediaryId;
    }
}

public class AuthorizationFilters
{
    public Guid? AffiliateId { get; set; }
    public Guid? ContractorId { get; set; }
    public Guid? IntermediaryId { get; set; }
    public bool? IsActive { get; set; }
    public DateTimeOffset? CreatedFrom { get; set; }
    public DateTimeOffset? CreatedBefore { get; set; }

    public AuthorizationFilters(Guid? AffiliateId = null, Guid? ContractorId = null, Guid? IntermediaryId = null, bool? IsActive = null, DateTimeOffset? CreatedFrom = null, DateTimeOffset? CreatedBefore = null)
    {
        this.AffiliateId = AffiliateId;
        this.ContractorId = ContractorId;
        this.IntermediaryId = IntermediaryId;
        this.IsActive = IsActive;
        this.CreatedFrom = CreatedFrom;
        this.CreatedBefore = CreatedBefore;
    }
}

public class AuthorizationSummaryDto
{
    public Guid Id { get; set; }
    public Guid AffiliateId { get; set; }
    public Guid? ContractorId { get; set; }
    public Guid? IntermediaryId { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
    public decimal TotalClaimedAmount { get; set; }
    public decimal TotalArsAmount { get; set; }
    public decimal TotalAffiliateAmount { get; set; }

    public AuthorizationSummaryDto(Guid Id, Guid AffiliateId, Guid? ContractorId, Guid? IntermediaryId, bool IsActive, DateTimeOffset CreatedDate, decimal TotalClaimedAmount, decimal TotalArsAmount, decimal TotalAffiliateAmount)
    {
        this.Id = Id;
        this.AffiliateId = AffiliateId;
        this.ContractorId = ContractorId;
        this.IntermediaryId = IntermediaryId;
        this.IsActive = IsActive;
        this.CreatedDate = CreatedDate;
        this.TotalClaimedAmount = TotalClaimedAmount;
        this.TotalArsAmount = TotalArsAmount;
        this.TotalAffiliateAmount = TotalAffiliateAmount;
    }
}

public class AuthorizationDetailDto
{
    public Guid Id { get; set; }
    public Guid ProviderId { get; set; }
    public Guid ServiceTypeId { get; set; }
    public decimal ClaimedAmount { get; set; }
    public decimal ArsAmount { get; set; }
    public decimal AffiliateAmount { get; set; }

    public AuthorizationDetailDto(Guid Id, Guid ProviderId, Guid ServiceTypeId, decimal ClaimedAmount, decimal ArsAmount, decimal AffiliateAmount)
    {
        this.Id = Id;
        this.ProviderId = ProviderId;
        this.ServiceTypeId = ServiceTypeId;
        this.ClaimedAmount = ClaimedAmount;
        this.ArsAmount = ArsAmount;
        this.AffiliateAmount = AffiliateAmount;
    }
}

public class AuthorizationDto
{
    public AuthorizationSummaryDto Summary { get; set; }
    public IReadOnlyList<AuthorizationDetailDto> Details { get; set; }

    public AuthorizationDto(AuthorizationSummaryDto Summary, IReadOnlyList<AuthorizationDetailDto> Details)
    {
        this.Summary = Summary;
        this.Details = Details;
    }
}
