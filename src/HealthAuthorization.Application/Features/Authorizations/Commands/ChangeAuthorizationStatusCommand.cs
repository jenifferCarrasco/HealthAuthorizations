using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.Authorizations.Commands;

public class ChangeAuthorizationStatusCommand : IRequest
{
    public Guid Id { get; set; }
    public bool IsActive { get; set; }

    public ChangeAuthorizationStatusCommand(Guid Id, bool IsActive)
    {
        this.Id = Id;
        this.IsActive = IsActive;
    }
}

public class ChangeAuthorizationStatusCommandHandler : IRequestHandler<ChangeAuthorizationStatusCommand>
{
    private readonly IRepositoryAsync<Authorization> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthorizationReactivationPolicy _reactivationPolicy;

    public ChangeAuthorizationStatusCommandHandler(IRepositoryAsync<Authorization> repository, IUnitOfWork unitOfWork, IAuthorizationReactivationPolicy reactivationPolicy)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _reactivationPolicy = reactivationPolicy;
    }

    public async Task Handle(ChangeAuthorizationStatusCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("La autorización no existe.");
        if (entity.IsActive == request.IsActive)
        {
            return;
        }

        if (request.IsActive && !_reactivationPolicy.CanReactivate(entity))
        {
            throw new ConflictException("La reactivación de la autorización no está permitida.");
        }

        entity.IsActive = request.IsActive;
        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
