using HealthAuthorization.Application.Common;
using MediatR;

namespace HealthAuthorization.Application.Features.Affiliates.Queries;

public sealed record GetAffiliateByIdQuery(Guid Id) : IRequest<AffiliateDto>;

public sealed class GetAffiliateByIdQueryHandler(IAffiliateRepository affiliates)
    : IRequestHandler<GetAffiliateByIdQuery, AffiliateDto>
{
    public async Task<AffiliateDto> Handle(GetAffiliateByIdQuery request, CancellationToken cancellationToken) =>
        await affiliates.GetAsync(request.Id, cancellationToken)
        ?? throw new NotFoundException("El afiliado no existe.");
}
