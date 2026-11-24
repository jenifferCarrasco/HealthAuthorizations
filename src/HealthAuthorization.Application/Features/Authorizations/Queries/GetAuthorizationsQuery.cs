using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Application.Common.Models;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.Authorizations.Queries;

public class GetAuthorizationsQuery : PaginationFilter, IRequest<Page<AuthorizationSummaryDto>>
{
    public Guid? AffiliateId { get; set; }
    public Guid? ContractorId { get; set; }
    public Guid? IntermediaryId { get; set; }
    public bool? IsActive { get; set; }
    public DateTimeOffset? CreatedFrom { get; set; }
    public DateTimeOffset? CreatedBefore { get; set; }
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
        return _repository.ListAsync(entity =>
            (request.AffiliateId == null || entity.AffiliateId == request.AffiliateId) &&
            (request.ContractorId == null || entity.ContractorId == request.ContractorId) &&
            (request.IntermediaryId == null || entity.IntermediaryId == request.IntermediaryId) &&
            (request.IsActive == null || entity.IsActive == request.IsActive) &&
            (request.CreatedFrom == null || entity.CreatedDate >= request.CreatedFrom) &&
            (request.CreatedBefore == null || entity.CreatedDate < request.CreatedBefore),
            query => query.OrderByDescending(entity => entity.CreatedDate).ThenByDescending(entity => entity.Id),
            entity => new AuthorizationSummaryDto(entity.Id, entity.AffiliateId, entity.ContractorId, entity.IntermediaryId,
                entity.IsActive, entity.CreatedDate, entity.Details.Where(detail => !detail.IsDelete).Sum(detail => detail.ClaimedAmount),
                entity.Details.Where(detail => !detail.IsDelete).Sum(detail => detail.ArsAmount), entity.Details.Where(detail => !detail.IsDelete).Sum(detail => detail.AffiliateAmount)),
            request.PageNumber, request.PageSize, cancellationToken);
    }
}
