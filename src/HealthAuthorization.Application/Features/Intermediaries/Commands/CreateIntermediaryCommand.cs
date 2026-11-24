using HealthAuthorization.Application.Abstractions;
using MapsterMapper;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.Intermediaries.Commands;

public class CreateIntermediaryCommand : IRequest<Guid>
{
    public IntermediaryInput Input { get; set; }

    public CreateIntermediaryCommand(IntermediaryInput Input)
    {
        this.Input = Input;
    }
}

public class CreateIntermediaryCommandHandler : IRequestHandler<CreateIntermediaryCommand, Guid>
{
    private readonly IRepositoryAsync<Intermediary> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateIntermediaryCommandHandler(IRepositoryAsync<Intermediary> repository, IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Guid> Handle(CreateIntermediaryCommand request, CancellationToken cancellationToken)
    {
        var entity = _mapper.Map<Intermediary>(request.Input);
        _repository.Add(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
