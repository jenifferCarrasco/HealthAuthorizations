using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.ServiceTypes.Commands;

public class ChangeServiceTypeStatusCommand : IRequest
{
    public Guid Id { get; set; }
    public bool IsActive { get; set; }

    public ChangeServiceTypeStatusCommand(Guid Id, bool IsActive)
    {
        this.Id = Id;
        this.IsActive = IsActive;
    }
}

public class ChangeServiceTypeStatusCommandHandler : IRequestHandler<ChangeServiceTypeStatusCommand>
{
    private readonly IRepositoryAsync<ServiceType> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeServiceTypeStatusCommandHandler(IRepositoryAsync<ServiceType> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ChangeServiceTypeStatusCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("El tipo de servicio no existe.");
        if (entity.IsDelete)
        {
            throw new NotFoundException("El tipo de servicio no existe.");
        }

        if (entity.IsActive == request.IsActive)
        {
            return;
        }

        entity.IsActive = request.IsActive;
        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
