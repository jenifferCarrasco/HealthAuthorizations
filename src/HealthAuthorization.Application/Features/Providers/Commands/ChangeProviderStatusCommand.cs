using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.Providers.Commands;

public class ChangeProviderStatusCommand : IRequest
{
    public Guid Id { get; set; }
    public bool IsActive { get; set; }

    public ChangeProviderStatusCommand(Guid Id, bool IsActive)
    {
        this.Id = Id;
        this.IsActive = IsActive;
    }
}

public class ChangeProviderStatusCommandHandler : IRequestHandler<ChangeProviderStatusCommand>
{
    private readonly IRepositoryAsync<Provider> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeProviderStatusCommandHandler(IRepositoryAsync<Provider> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ChangeProviderStatusCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("El prestador no existe.");
        if (entity.IsDelete)
        {
            throw new NotFoundException("El prestador no existe.");
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
