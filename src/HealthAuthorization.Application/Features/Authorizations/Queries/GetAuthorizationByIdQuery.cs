using HealthAuthorization.Application.Common;
using MediatR;

namespace HealthAuthorization.Application.Features.Authorizations.Queries;

public sealed record GetAuthorizationByIdQuery(Guid Id) : IRequest<AuthorizationDto>;

public sealed class GetAuthorizationByIdQueryHandler(IAuthorizationRepository authorizations)
    : IRequestHandler<GetAuthorizationByIdQuery, AuthorizationDto>
{
    public async Task<AuthorizationDto> Handle(GetAuthorizationByIdQuery request, CancellationToken cancellationToken) =>
        await authorizations.GetAsync(request.Id, cancellationToken)
        ?? throw new NotFoundException("La autorización no existe.");
}
