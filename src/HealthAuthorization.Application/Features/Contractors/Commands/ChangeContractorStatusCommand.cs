using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.Contractors.Commands;

public class ChangeContractorStatusCommand : IRequest
{
    public Guid Id { get; set; }
    public bool IsActive { get; set; }

    public ChangeContractorStatusCommand(Guid Id, bool IsActive)
    {
        this.Id = Id;
        this.IsActive = IsActive;
    }
}

public class ChangeContractorStatusCommandHandler : IRequestHandler<ChangeContractorStatusCommand>
{
    private readonly IRepositoryAsync<Contractor> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeContractorStatusCommandHandler(IRepositoryAsync<Contractor> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ChangeContractorStatusCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("El contratante no existe.");
        if (entity.IsDelete)
        {
            throw new NotFoundException("El contratante no existe.");
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
