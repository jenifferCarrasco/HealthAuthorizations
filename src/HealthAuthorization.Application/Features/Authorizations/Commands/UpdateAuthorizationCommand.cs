using HealthAuthorization.Domain.Entities;
using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using MediatR;

namespace HealthAuthorization.Application.Features.Authorizations.Commands;

public class UpdateAuthorizationCommand : IRequest
{
    public Guid Id { get; set; }
    public AuthorizationInput Input { get; set; }

    public UpdateAuthorizationCommand(Guid Id, AuthorizationInput Input)
    {
        this.Id = Id;
        this.Input = Input;
    }
}

public class UpdateAuthorizationCommandHandler : IRequestHandler<UpdateAuthorizationCommand>
{
    private readonly IRepositoryAsync<Authorization> _authorizations;
    private readonly AuthorizationWriter _writer;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAuthorizationCommandHandler(IRepositoryAsync<Authorization> authorizations, AuthorizationWriter writer, IUnitOfWork unitOfWork)
    {
        _authorizations = authorizations;
        _writer = writer;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateAuthorizationCommand request, CancellationToken cancellationToken)
    {
        var authorization = await _authorizations.GetForUpdateAsync(request.Id, entity => entity.Details, cancellationToken)
            ?? throw new NotFoundException("La autorización no existe.");
        await _writer.ApplyAsync(authorization, request.Input, cancellationToken);
        // Persist the header as well so detail-only changes check its rowversion.
        _authorizations.Update(authorization, forceWrite: true);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
