using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using MapsterMapper;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.ServiceTypes.Commands;

public class UpdateServiceTypeCommand : IRequest
{
    public Guid Id { get; set; }
    public ServiceTypeInput Input { get; set; }

    public UpdateServiceTypeCommand(Guid Id, ServiceTypeInput Input)
    {
        this.Id = Id;
        this.Input = Input;
    }
}

public class UpdateServiceTypeCommandHandler : IRequestHandler<UpdateServiceTypeCommand>
{
    private readonly IRepositoryAsync<ServiceType> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateServiceTypeCommandHandler(IRepositoryAsync<ServiceType> repository, IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task Handle(UpdateServiceTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("El tipo de servicio no existe.");
        if (entity.IsDelete)
        {
            throw new NotFoundException("El tipo de servicio no existe.");
        }

        _mapper.Map(request.Input, entity);
        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
