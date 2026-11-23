using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.ProviderTypes.Commands;

public class CreateProviderTypeCommand : IRequest<Guid>
{
    public ProviderTypeInput Input { get; set; }

    public CreateProviderTypeCommand(ProviderTypeInput Input)
    {
        this.Input = Input;
    }
}

public class CreateProviderTypeCommandHandler : IRequestHandler<CreateProviderTypeCommand, Guid>
{
    private readonly IRepositoryAsync<ProviderType> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProviderTypeCommandHandler(IRepositoryAsync<ProviderType> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateProviderTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = new ProviderType();
        entity.Name = request.Input.Name.Trim();
        entity.Description = InputLimits.NormalizeOptional(request.Input.Description);
        _repository.Add(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
