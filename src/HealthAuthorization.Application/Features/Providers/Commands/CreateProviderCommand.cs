using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using MapsterMapper;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.Providers.Commands;

public class CreateProviderCommand : IRequest<Guid>
{
    public ProviderInput Input { get; set; }

    public CreateProviderCommand(ProviderInput Input)
    {
        this.Input = Input;
    }
}

public class CreateProviderCommandHandler : IRequestHandler<CreateProviderCommand, Guid>
{
    private readonly IRepositoryAsync<Provider> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IRepositoryAsync<ProviderType> _providerTypes;

    public CreateProviderCommandHandler(IRepositoryAsync<Provider> repository, IUnitOfWork unitOfWork, IRepositoryAsync<ProviderType> providerTypes,
        IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _providerTypes = providerTypes;
    }

    public async Task<Guid> Handle(CreateProviderCommand request, CancellationToken cancellationToken)
    {
        if (!await _providerTypes.ExistsAsync(request.Input.ProviderTypeId, cancellationToken))
        {
            throw new NotFoundException("El tipo de prestador no existe.");
        }

        var entity = _mapper.Map<Provider>(request.Input);
        _repository.Add(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
