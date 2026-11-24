using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.Affiliates.Commands;

public class ChangeAffiliateStatusCommand : IRequest
{
    public Guid Id { get; set; }
    public bool IsActive { get; set; }

    public ChangeAffiliateStatusCommand(Guid Id, bool IsActive)
    {
        this.Id = Id;
        this.IsActive = IsActive;
    }
}

public class ChangeAffiliateStatusCommandHandler : IRequestHandler<ChangeAffiliateStatusCommand>
{
    private readonly IRepositoryAsync<Affiliate> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeAffiliateStatusCommandHandler(IRepositoryAsync<Affiliate> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ChangeAffiliateStatusCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("El afiliado no existe.");
        if (entity.IsDelete)
        {
            throw new NotFoundException("El afiliado no existe.");
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
