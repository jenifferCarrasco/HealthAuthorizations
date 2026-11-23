using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.Providers.Commands;

public class UpdateProviderCommand : IRequest
{
    public Guid Id { get; set; }
    public ProviderInput Input { get; set; }

    public UpdateProviderCommand(Guid Id, ProviderInput Input)
    {
        this.Id = Id;
        this.Input = Input;
    }
}

public class UpdateProviderCommandHandler : IRequestHandler<UpdateProviderCommand>
{
    private readonly IRepositoryAsync<Provider> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepositoryAsync<ProviderType> _providerTypes;

    public UpdateProviderCommandHandler(IRepositoryAsync<Provider> repository, IUnitOfWork unitOfWork, IRepositoryAsync<ProviderType> providerTypes)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _providerTypes = providerTypes;
    }

    public async Task Handle(UpdateProviderCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("El prestador no existe.");
        if (entity.IsDelete)
        {
            throw new NotFoundException("El prestador no existe.");
        }

        if (entity.ProviderTypeId != request.Input.ProviderTypeId &&
            !await _providerTypes.ExistsAsync(request.Input.ProviderTypeId, cancellationToken))
        {
            throw new NotFoundException("El tipo de prestador no existe.");
        }

        entity.Name = request.Input.Name.Trim();
        entity.Identification = request.Input.Identification.Trim();
        entity.ProviderTypeId = request.Input.ProviderTypeId;
        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
