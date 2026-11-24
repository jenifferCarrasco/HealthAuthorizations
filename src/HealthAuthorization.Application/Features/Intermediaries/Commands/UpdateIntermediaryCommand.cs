using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using MapsterMapper;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.Intermediaries.Commands;

public class UpdateIntermediaryCommand : IRequest
{
    public Guid Id { get; set; }
    public IntermediaryInput Input { get; set; }

    public UpdateIntermediaryCommand(Guid Id, IntermediaryInput Input)
    {
        this.Id = Id;
        this.Input = Input;
    }
}

public class UpdateIntermediaryCommandHandler : IRequestHandler<UpdateIntermediaryCommand>
{
    private readonly IRepositoryAsync<Intermediary> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateIntermediaryCommandHandler(IRepositoryAsync<Intermediary> repository, IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task Handle(UpdateIntermediaryCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("El intermediario no existe.");
        if (entity.IsDelete)
        {
            throw new NotFoundException("El intermediario no existe.");
        }

        _mapper.Map(request.Input, entity);
        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
