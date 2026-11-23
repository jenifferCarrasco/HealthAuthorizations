using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.Authorizations.Commands;

public class CreateAuthorizationCommand : IRequest<Guid>
{
    public AuthorizationInput Input { get; set; }

    public CreateAuthorizationCommand(AuthorizationInput Input)
    {
        this.Input = Input;
    }
}

public class CreateAuthorizationCommandHandler : IRequestHandler<CreateAuthorizationCommand, Guid>
{
    private readonly IRepositoryAsync<Authorization> _authorizations;
    private readonly AuthorizationWriter _writer;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAuthorizationCommandHandler(IRepositoryAsync<Authorization> authorizations, AuthorizationWriter writer, IUnitOfWork unitOfWork)
    {
        _authorizations = authorizations;
        _writer = writer;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateAuthorizationCommand request, CancellationToken cancellationToken)
    {
        var authorization = new Authorization();
        await _writer.ApplyAsync(authorization, request.Input, cancellationToken);
        _authorizations.Add(authorization);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return authorization.Id;
    }
}
