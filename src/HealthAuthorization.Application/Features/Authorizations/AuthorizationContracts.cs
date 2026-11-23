using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;

namespace HealthAuthorization.Application.Features.Authorizations;

public sealed record AuthorizationServiceInput(Guid ProviderId, Guid ServiceTypeId, decimal ClaimedAmount, Guid? DetailId = null);

public sealed record AuthorizationInput(
    Guid AffiliateId, IReadOnlyList<AuthorizationServiceInput> Services,
    Guid? ContractorId = null, Guid? IntermediaryId = null);

public sealed record AuthorizationFilters(
    Guid? AffiliateId = null, Guid? ContractorId = null, Guid? IntermediaryId = null,
    bool? IsActive = null, DateTimeOffset? CreatedFrom = null, DateTimeOffset? CreatedBefore = null);

public sealed record AuthorizationSummaryDto(
    Guid Id, Guid AffiliateId, Guid? ContractorId, Guid? IntermediaryId, bool IsActive,
    DateTimeOffset CreatedDate, decimal TotalClaimedAmount, decimal TotalArsAmount, decimal TotalAffiliateAmount);

public sealed record AuthorizationDetailDto(
    Guid Id, Guid ProviderId, Guid ServiceTypeId, decimal ClaimedAmount, decimal ArsAmount, decimal AffiliateAmount);

public sealed record AuthorizationDto(AuthorizationSummaryDto Summary, IReadOnlyList<AuthorizationDetailDto> Details);

public interface IAuthorizationRepository : IRepositoryAsync<Authorization>
{
    Task<AuthorizationDto?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<Page<AuthorizationSummaryDto>> ListAsync(
        AuthorizationFilters filters, int pageNumber, int pageSize, CancellationToken cancellationToken);
}
