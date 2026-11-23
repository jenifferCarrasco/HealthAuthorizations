using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.Authorizations.Queries;

public class GetAuthorizationsQuery : IRequest<Page<AuthorizationSummaryDto>>
{
    public AuthorizationFilters? Filters { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }

    public GetAuthorizationsQuery(AuthorizationFilters? Filters = null, int PageNumber = 1, int PageSize = 20)
    {
        this.Filters = Filters;
        this.PageNumber = PageNumber;
        this.PageSize = PageSize;
    }
}

public class GetAuthorizationsQueryHandler : IRequestHandler<GetAuthorizationsQuery, Page<AuthorizationSummaryDto>>
{
    private readonly IRepositoryAsync<Authorization> _repository;

    public GetAuthorizationsQueryHandler(IRepositoryAsync<Authorization> repository)
    {
        _repository = repository;
    }

    public Task<Page<AuthorizationSummaryDto>> Handle(GetAuthorizationsQuery request, CancellationToken cancellationToken)
    {
        var filters = request.Filters ?? new AuthorizationFilters();
        return _repository.ListAsync(entity =>
            (filters.AffiliateId == null || entity.AffiliateId == filters.AffiliateId) &&
            (filters.ContractorId == null || entity.ContractorId == filters.ContractorId) &&
            (filters.IntermediaryId == null || entity.IntermediaryId == filters.IntermediaryId) &&
            (filters.IsActive == null || entity.IsActive == filters.IsActive) &&
            (filters.CreatedFrom == null || entity.CreatedDate >= filters.CreatedFrom) &&
            (filters.CreatedBefore == null || entity.CreatedDate < filters.CreatedBefore),
            query => query.OrderByDescending(entity => entity.CreatedDate).ThenByDescending(entity => entity.Id),
            entity => new AuthorizationSummaryDto(entity.Id, entity.AffiliateId, entity.ContractorId, entity.IntermediaryId,
                entity.IsActive, entity.CreatedDate, entity.Details.Where(detail => !detail.IsDelete).Sum(detail => detail.ClaimedAmount),
                entity.Details.Where(detail => !detail.IsDelete).Sum(detail => detail.ArsAmount), entity.Details.Where(detail => !detail.IsDelete).Sum(detail => detail.AffiliateAmount)),
            request.PageNumber, request.PageSize, cancellationToken);
    }
}
