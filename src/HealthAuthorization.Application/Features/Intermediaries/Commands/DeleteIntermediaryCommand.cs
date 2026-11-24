using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.Intermediaries.Commands;

public class DeleteIntermediaryCommand : IRequest
{
    public Guid Id { get; set; }

    public DeleteIntermediaryCommand(Guid Id)
    {
        this.Id = Id;
    }
}

public class DeleteIntermediaryCommandHandler : IRequestHandler<DeleteIntermediaryCommand>
{
    private readonly IRepositoryAsync<Intermediary> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteIntermediaryCommandHandler(IRepositoryAsync<Intermediary> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteIntermediaryCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("El intermediario no existe.");
        if (entity.IsDelete)
        {
            throw new NotFoundException("El intermediario no existe.");
        }

        entity.IsDelete = true;
        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
