using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.Affiliates.Commands;

public class UpdateAffiliateCommand : IRequest
{
    public Guid Id { get; set; }
    public AffiliateInput Input { get; set; }

    public UpdateAffiliateCommand(Guid Id, AffiliateInput Input)
    {
        this.Id = Id;
        this.Input = Input;
    }
}

public class UpdateAffiliateCommandHandler : IRequestHandler<UpdateAffiliateCommand>
{
    private readonly IRepositoryAsync<Affiliate> _affiliates;
    private readonly IRepositoryAsync<Contractor> _contractors;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAffiliateCommandHandler(IRepositoryAsync<Affiliate> affiliates, IRepositoryAsync<Contractor> contractors, IUnitOfWork unitOfWork)
    {
        _affiliates = affiliates;
        _contractors = contractors;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateAffiliateCommand request, CancellationToken cancellationToken)
    {
        var affiliate = await _affiliates.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("El afiliado no existe.");
        await AffiliateWriter.ApplyAsync(affiliate, request.Input, affiliate.Id, _affiliates, _contractors, cancellationToken);
        _affiliates.Update(affiliate);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
