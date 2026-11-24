using HealthAuthorization.Application.Abstractions;
using MapsterMapper;
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
    private readonly IMapper _mapper;

    public CreateServiceTypeCommandHandler(IRepositoryAsync<ServiceType> repository, IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Guid> Handle(CreateServiceTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = _mapper.Map<ServiceType>(request.Input);
        _repository.Add(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
