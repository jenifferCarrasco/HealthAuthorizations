using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.Affiliates.Commands;

public class CreateAffiliateCommand : IRequest<Guid>
{
    public AffiliateInput Input { get; set; }

    public CreateAffiliateCommand(AffiliateInput Input)
    {
        this.Input = Input;
    }
}

public class CreateAffiliateCommandHandler : IRequestHandler<CreateAffiliateCommand, Guid>
{
    private readonly IRepositoryAsync<Affiliate> _affiliates;
    private readonly IRepositoryAsync<Contractor> _contractors;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAffiliateCommandHandler(IRepositoryAsync<Affiliate> affiliates, IRepositoryAsync<Contractor> contractors, IUnitOfWork unitOfWork)
    {
        _affiliates = affiliates;
        _contractors = contractors;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateAffiliateCommand request, CancellationToken cancellationToken)
    {
        var affiliate = new Affiliate();
        await AffiliateWriter.ApplyAsync(affiliate, request.Input, null, _affiliates, _contractors, cancellationToken);
        _affiliates.Add(affiliate);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return affiliate.Id;
    }
}
