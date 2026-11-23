using HealthAuthorization.Application.Common;
using MediatR;

namespace HealthAuthorization.Application.Features.Authorizations.Queries;

public sealed record GetAuthorizationsQuery(AuthorizationFilters? Filters = null, int PageNumber = 1, int PageSize = 20)
    : IRequest<Page<AuthorizationSummaryDto>>;

public sealed class GetAuthorizationsQueryHandler(IAuthorizationRepository authorizations)
    : IRequestHandler<GetAuthorizationsQuery, Page<AuthorizationSummaryDto>>
{
    public Task<Page<AuthorizationSummaryDto>> Handle(GetAuthorizationsQuery request, CancellationToken cancellationToken)
    {
        InputLimits.ValidatePage(request.PageNumber, request.PageSize);
        var filters = request.Filters ?? new AuthorizationFilters();
        if (filters.CreatedFrom >= filters.CreatedBefore)
        {
            throw new ArgumentException("La fecha inicial debe ser anterior a la fecha final.", nameof(request));
        }

        return authorizations.ListAsync(filters, request.PageNumber, request.PageSize, cancellationToken);
    }
}
