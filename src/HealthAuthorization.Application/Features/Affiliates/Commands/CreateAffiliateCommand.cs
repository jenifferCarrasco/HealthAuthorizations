using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.Affiliates.Commands;

public sealed record CreateAffiliateCommand(AffiliateInput Input) : IRequest<Guid>;

public sealed class CreateAffiliateCommandHandler(
    IAffiliateRepository affiliates, IRepositoryAsync<Contractor> contractors, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateAffiliateCommand, Guid>
{
    public async Task<Guid> Handle(CreateAffiliateCommand request, CancellationToken cancellationToken)
    {
        var affiliate = new Affiliate();
        await AffiliateWriter.ApplyAsync(affiliate, request.Input, null, affiliates, contractors, cancellationToken);
        affiliates.Add(affiliate);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return affiliate.Id;
    }
}
