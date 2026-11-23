using System.Linq.Expressions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Application.Features.Authorizations;
using HealthAuthorization.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HealthAuthorization.Infrastructure.Persistence.Repositories;

internal sealed class AuthorizationRepository(HealthAuthorizationDbContext context)
    : EfRepository<Authorization>(context), IAuthorizationRepository
{
    private static readonly Expression<Func<Authorization, AuthorizationSummaryDto>> Summary = authorization =>
        new(authorization.Id, authorization.AffiliateId, authorization.ContractorId, authorization.IntermediaryId,
            authorization.IsActive, authorization.CreatedDate,
            authorization.Details.Sum(detail => detail.ClaimedAmount),
            authorization.Details.Sum(detail => detail.ArsAmount),
            authorization.Details.Sum(detail => detail.AffiliateAmount));

    public override Task<Authorization?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken) =>
        Entities.AsTracking().Include(authorization => authorization.Details)
            .SingleOrDefaultAsync(authorization => authorization.Id == id, cancellationToken);

    public async Task<AuthorizationDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await Entities.AsNoTracking().Where(authorization => authorization.Id == id)
            .Select(authorization => new
            {
                authorization.Id, authorization.AffiliateId, authorization.ContractorId,
                authorization.IntermediaryId, authorization.IsActive, authorization.CreatedDate,
                Details = authorization.Details.OrderBy(detail => detail.Id)
                    .Select(detail => new AuthorizationDetailDto(detail.Id, detail.ProviderId, detail.ServiceTypeId,
                        detail.ClaimedAmount, detail.ArsAmount, detail.AffiliateAmount)).ToList()
            }).SingleOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var summary = new AuthorizationSummaryDto(row.Id, row.AffiliateId, row.ContractorId, row.IntermediaryId,
            row.IsActive, row.CreatedDate, row.Details.Sum(detail => detail.ClaimedAmount),
            row.Details.Sum(detail => detail.ArsAmount), row.Details.Sum(detail => detail.AffiliateAmount));
        return new AuthorizationDto(summary, row.Details);
    }

    public async Task<Page<AuthorizationSummaryDto>> ListAsync(
        AuthorizationFilters filters, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = Entities.AsNoTracking();
        if (filters.AffiliateId is Guid affiliateId)
            query = query.Where(authorization => authorization.AffiliateId == affiliateId);
        if (filters.ContractorId is Guid contractorId)
            query = query.Where(authorization => authorization.ContractorId == contractorId);
        if (filters.IntermediaryId is Guid intermediaryId)
            query = query.Where(authorization => authorization.IntermediaryId == intermediaryId);
        if (filters.IsActive is bool isActive)
            query = query.Where(authorization => authorization.IsActive == isActive);
        if (filters.CreatedFrom is DateTimeOffset from)
            query = query.Where(authorization => authorization.CreatedDate >= from);
        if (filters.CreatedBefore is DateTimeOffset before)
            query = query.Where(authorization => authorization.CreatedDate < before);

        var count = await query.LongCountAsync(cancellationToken);
        var items = await query.OrderByDescending(authorization => authorization.CreatedDate)
            .ThenByDescending(authorization => authorization.Id)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).Select(Summary).ToListAsync(cancellationToken);
        return new Page<AuthorizationSummaryDto>(items, pageNumber, pageSize, count);
    }
}
