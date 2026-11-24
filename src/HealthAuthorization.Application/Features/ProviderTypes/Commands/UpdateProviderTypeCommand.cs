using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using MapsterMapper;
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
    private readonly IMapper _mapper;

    public UpdateProviderTypeCommandHandler(IRepositoryAsync<ProviderType> repository, IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task Handle(UpdateProviderTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("El tipo de prestador no existe.");
        if (entity.IsDelete)
        {
            throw new NotFoundException("El tipo de prestador no existe.");
        }

        _mapper.Map(request.Input, entity);
        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
