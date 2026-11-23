using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using MediatR;

namespace HealthAuthorization.Application.Features.Authorizations.Commands;

public sealed record UpdateAuthorizationCommand(Guid Id, AuthorizationInput Input) : IRequest;

public sealed class UpdateAuthorizationCommandHandler(
    IAuthorizationRepository authorizations, AuthorizationWriter writer, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateAuthorizationCommand>
{
    public async Task Handle(UpdateAuthorizationCommand request, CancellationToken cancellationToken)
    {
        var authorization = await authorizations.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("La autorización no existe.");
        await writer.ApplyAsync(authorization, request.Input, cancellationToken);
        authorizations.Update(authorization);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
