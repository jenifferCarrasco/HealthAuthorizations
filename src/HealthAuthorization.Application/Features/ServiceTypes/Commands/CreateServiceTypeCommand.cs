using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.ServiceTypes.Commands;

public class CreateServiceTypeCommand : IRequest<Guid>
{
    public ServiceTypeInput Input { get; set; }

    public CreateServiceTypeCommand(ServiceTypeInput Input)
    {
        this.Input = Input;
    }
}

public class CreateServiceTypeCommandHandler : IRequestHandler<CreateServiceTypeCommand, Guid>
{
    private readonly IRepositoryAsync<ServiceType> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateServiceTypeCommandHandler(IRepositoryAsync<ServiceType> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateServiceTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = new ServiceType();
        entity.Name = request.Input.Name.Trim();
        entity.Description = InputLimits.NormalizeOptional(request.Input.Description);
        _repository.Add(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
