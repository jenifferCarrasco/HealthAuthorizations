using HealthAuthorization.Application.Abstractions;
using MapsterMapper;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.Contractors.Commands;

public class CreateContractorCommand : IRequest<Guid>
{
    public ContractorInput Input { get; set; }

    public CreateContractorCommand(ContractorInput Input)
    {
        this.Input = Input;
    }
}

public class CreateContractorCommandHandler : IRequestHandler<CreateContractorCommand, Guid>
{
    private readonly IRepositoryAsync<Contractor> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateContractorCommandHandler(IRepositoryAsync<Contractor> repository, IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Guid> Handle(CreateContractorCommand request, CancellationToken cancellationToken)
    {
        var entity = _mapper.Map<Contractor>(request.Input);
        _repository.Add(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
