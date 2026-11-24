using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.Contractors.Commands;

public class DeleteContractorCommand : IRequest
{
    public Guid Id { get; set; }

    public DeleteContractorCommand(Guid Id)
    {
        this.Id = Id;
    }
}

public class DeleteContractorCommandHandler : IRequestHandler<DeleteContractorCommand>
{
    private readonly IRepositoryAsync<Contractor> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteContractorCommandHandler(IRepositoryAsync<Contractor> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteContractorCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("El contratante no existe.");
        if (entity.IsDelete)
        {
            throw new NotFoundException("El contratante no existe.");
        }

        entity.IsDelete = true;
        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
