using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.Affiliates.Commands;

public class DeleteAffiliateCommand : IRequest
{
    public Guid Id { get; set; }

    public DeleteAffiliateCommand(Guid Id)
    {
        this.Id = Id;
    }
}

public class DeleteAffiliateCommandHandler : IRequestHandler<DeleteAffiliateCommand>
{
    private readonly IRepositoryAsync<Affiliate> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteAffiliateCommandHandler(IRepositoryAsync<Affiliate> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteAffiliateCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("El afiliado no existe.");
        if (entity.IsDelete)
        {
            throw new NotFoundException("El afiliado no existe.");
        }

        entity.IsDelete = true;
        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
