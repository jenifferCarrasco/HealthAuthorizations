using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.ProviderTypes.Commands;

public class ChangeProviderTypeStatusCommand : IRequest
{
    public Guid Id { get; set; }
    public bool IsActive { get; set; }

    public ChangeProviderTypeStatusCommand(Guid Id, bool IsActive)
    {
        this.Id = Id;
        this.IsActive = IsActive;
    }
}

public class ChangeProviderTypeStatusCommandHandler : IRequestHandler<ChangeProviderTypeStatusCommand>
{
    private readonly IRepositoryAsync<ProviderType> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeProviderTypeStatusCommandHandler(IRepositoryAsync<ProviderType> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ChangeProviderTypeStatusCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("El tipo de prestador no existe.");
        if (entity.IsDelete)
        {
            throw new NotFoundException("El tipo de prestador no existe.");
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
