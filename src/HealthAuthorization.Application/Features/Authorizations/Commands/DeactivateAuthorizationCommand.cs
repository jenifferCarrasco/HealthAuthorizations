using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using MediatR;

namespace HealthAuthorization.Application.Features.Authorizations.Commands;

public sealed record DeactivateAuthorizationCommand(Guid Id) : IRequest;

public sealed class DeactivateAuthorizationCommandHandler(
    IAuthorizationRepository authorizations, IUnitOfWork unitOfWork) : IRequestHandler<DeactivateAuthorizationCommand>
{
    public async Task Handle(DeactivateAuthorizationCommand request, CancellationToken cancellationToken)
    {
        var authorization = await authorizations.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("La autorización no existe.");
        if (!authorization.IsActive)
        {
            return;
        }

        authorization.IsActive = false;
        authorizations.Update(authorization);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
