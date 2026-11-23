using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
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

    public CreateIntermediaryCommandHandler(IRepositoryAsync<Intermediary> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateIntermediaryCommand request, CancellationToken cancellationToken)
    {
        var entity = new Intermediary();
        entity.Name = request.Input.Name.Trim();
        entity.Identification = request.Input.Identification.Trim();
        _repository.Add(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
