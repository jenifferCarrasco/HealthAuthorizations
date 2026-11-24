using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.Providers.Commands;

public class DeleteProviderCommand : IRequest
{
    public Guid Id { get; set; }

    public DeleteProviderCommand(Guid Id)
    {
        this.Id = Id;
    }
}

public class DeleteProviderCommandHandler : IRequestHandler<DeleteProviderCommand>
{
    private readonly IRepositoryAsync<Provider> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProviderCommandHandler(IRepositoryAsync<Provider> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteProviderCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("El prestador no existe.");
        if (entity.IsDelete)
        {
            throw new NotFoundException("El prestador no existe.");
        }

        entity.IsDelete = true;
        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
