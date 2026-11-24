using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using MapsterMapper;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.Contractors.Commands;

public class UpdateContractorCommand : IRequest
{
    public Guid Id { get; set; }
    public ContractorInput Input { get; set; }

    public UpdateContractorCommand(Guid Id, ContractorInput Input)
    {
        this.Id = Id;
        this.Input = Input;
    }
}

public class UpdateContractorCommandHandler : IRequestHandler<UpdateContractorCommand>
{
    private readonly IRepositoryAsync<Contractor> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateContractorCommandHandler(IRepositoryAsync<Contractor> repository, IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task Handle(UpdateContractorCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("El contratante no existe.");
        if (entity.IsDelete)
        {
            throw new NotFoundException("El contratante no existe.");
        }

        _mapper.Map(request.Input, entity);
        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
