using HealthAuthorization.Application.Common;
using MediatR;

namespace HealthAuthorization.Application.Features.Affiliates.Queries;

public sealed record GetAffiliatesQuery(string? Search = null, int PageNumber = 1, int PageSize = 20)
    : IRequest<Page<AffiliateDto>>;

public sealed class GetAffiliatesQueryHandler(IAffiliateRepository affiliates)
    : IRequestHandler<GetAffiliatesQuery, Page<AffiliateDto>>
{
    public Task<Page<AffiliateDto>> Handle(GetAffiliatesQuery request, CancellationToken cancellationToken)
    {
        InputLimits.ValidatePage(request.PageNumber, request.PageSize);
        return affiliates.ListAsync(InputLimits.NormalizeOptional(request.Search),
            request.PageNumber, request.PageSize, cancellationToken);
    }
}
