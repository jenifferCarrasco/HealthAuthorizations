using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.Contractors.Commands;

public class CreateContractorCommand : IRequest<Guid>
{
    public ContractorInput Input { get; set; }

    public CreateContractorCommand(ContractorInput Input)
    {
        this.Input = Input;
    }
}

public class CreateContractorCommandHandler : IRequestHandler<CreateContractorCommand, Guid>
{
    private readonly IRepositoryAsync<Contractor> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateContractorCommandHandler(IRepositoryAsync<Contractor> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateContractorCommand request, CancellationToken cancellationToken)
    {
        var entity = new Contractor();
        entity.Name = request.Input.Name.Trim();
        entity.Identification = request.Input.Identification.Trim();
        _repository.Add(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
