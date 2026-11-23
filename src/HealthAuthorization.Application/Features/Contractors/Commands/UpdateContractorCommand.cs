using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
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

    public UpdateContractorCommandHandler(IRepositoryAsync<Contractor> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateContractorCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("El contratante no existe.");
        if (entity.IsDelete)
        {
            throw new NotFoundException("El contratante no existe.");
        }

        entity.Name = request.Input.Name.Trim();
        entity.Identification = request.Input.Identification.Trim();
        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
