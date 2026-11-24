using HealthAuthorization.Application.Abstractions;
using MapsterMapper;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.ProviderTypes.Commands;

public class CreateProviderTypeCommand : IRequest<Guid>
{
    public ProviderTypeInput Input { get; set; }

    public CreateProviderTypeCommand(ProviderTypeInput Input)
    {
        this.Input = Input;
    }
}

public class CreateProviderTypeCommandHandler : IRequestHandler<CreateProviderTypeCommand, Guid>
{
    private readonly IRepositoryAsync<ProviderType> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateProviderTypeCommandHandler(IRepositoryAsync<ProviderType> repository, IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Guid> Handle(CreateProviderTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = _mapper.Map<ProviderType>(request.Input);
        _repository.Add(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
