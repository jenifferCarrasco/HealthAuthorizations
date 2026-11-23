using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.Affiliates.Commands;

public sealed record UpdateAffiliateCommand(Guid Id, AffiliateInput Input) : IRequest;

public sealed class UpdateAffiliateCommandHandler(
    IAffiliateRepository affiliates, IRepositoryAsync<Contractor> contractors, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateAffiliateCommand>
{
    public async Task Handle(UpdateAffiliateCommand request, CancellationToken cancellationToken)
    {
        var affiliate = await affiliates.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("El afiliado no existe.");
        await AffiliateWriter.ApplyAsync(affiliate, request.Input, affiliate.Id, affiliates, contractors, cancellationToken);
        affiliates.Update(affiliate);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
