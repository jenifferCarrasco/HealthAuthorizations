using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.Authorizations.Commands;

public sealed record CreateAuthorizationCommand(AuthorizationInput Input) : IRequest<Guid>;

public sealed class CreateAuthorizationCommandHandler(
    IAuthorizationRepository authorizations, AuthorizationWriter writer, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateAuthorizationCommand, Guid>
{
    public async Task<Guid> Handle(CreateAuthorizationCommand request, CancellationToken cancellationToken)
    {
        var authorization = new Authorization();
        await writer.ApplyAsync(authorization, request.Input, cancellationToken);
        authorizations.Add(authorization);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return authorization.Id;
    }
}
