using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.Intermediaries.Commands;

public class ChangeIntermediaryStatusCommand : IRequest
{
    public Guid Id { get; set; }
    public bool IsActive { get; set; }

    public ChangeIntermediaryStatusCommand(Guid Id, bool IsActive)
    {
        this.Id = Id;
        this.IsActive = IsActive;
    }
}

public class ChangeIntermediaryStatusCommandHandler : IRequestHandler<ChangeIntermediaryStatusCommand>
{
    private readonly IRepositoryAsync<Intermediary> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeIntermediaryStatusCommandHandler(IRepositoryAsync<Intermediary> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ChangeIntermediaryStatusCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("El intermediario no existe.");
        if (entity.IsDelete)
        {
            throw new NotFoundException("El intermediario no existe.");
        }

        if (entity.IsActive == request.IsActive)
        {
            return;
        }

        entity.IsActive = request.IsActive;
        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
