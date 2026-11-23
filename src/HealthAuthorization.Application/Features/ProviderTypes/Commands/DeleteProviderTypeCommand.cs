using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.ProviderTypes.Commands;

public class DeleteProviderTypeCommand : IRequest
{
    public Guid Id { get; set; }

    public DeleteProviderTypeCommand(Guid Id)
    {
        this.Id = Id;
    }
}

public class DeleteProviderTypeCommandHandler : IRequestHandler<DeleteProviderTypeCommand>
{
    private readonly IRepositoryAsync<ProviderType> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProviderTypeCommandHandler(IRepositoryAsync<ProviderType> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteProviderTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("El tipo de prestador no existe.");
        if (entity.IsDelete)
        {
            throw new NotFoundException("El tipo de prestador no existe.");
        }

        entity.IsDelete = true;
        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
