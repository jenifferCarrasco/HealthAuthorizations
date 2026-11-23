using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.ProviderTypes.Commands;

public class UpdateProviderTypeCommand : IRequest
{
    public Guid Id { get; set; }
    public ProviderTypeInput Input { get; set; }

    public UpdateProviderTypeCommand(Guid Id, ProviderTypeInput Input)
    {
        this.Id = Id;
        this.Input = Input;
    }
}

public class UpdateProviderTypeCommandHandler : IRequestHandler<UpdateProviderTypeCommand>
{
    private readonly IRepositoryAsync<ProviderType> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProviderTypeCommandHandler(IRepositoryAsync<ProviderType> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateProviderTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("El tipo de prestador no existe.");
        if (entity.IsDelete)
        {
            throw new NotFoundException("El tipo de prestador no existe.");
        }

        entity.Name = request.Input.Name.Trim();
        entity.Description = InputLimits.NormalizeOptional(request.Input.Description);
        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
