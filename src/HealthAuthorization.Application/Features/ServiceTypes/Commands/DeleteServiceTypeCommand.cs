using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.ServiceTypes.Commands;

public class DeleteServiceTypeCommand : IRequest
{
    public Guid Id { get; set; }

    public DeleteServiceTypeCommand(Guid Id)
    {
        this.Id = Id;
    }
}

public class DeleteServiceTypeCommandHandler : IRequestHandler<DeleteServiceTypeCommand>
{
    private readonly IRepositoryAsync<ServiceType> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteServiceTypeCommandHandler(IRepositoryAsync<ServiceType> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteServiceTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("El tipo de servicio no existe.");
        if (entity.IsDelete)
        {
            throw new NotFoundException("El tipo de servicio no existe.");
        }

        entity.IsDelete = true;
        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
