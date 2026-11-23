using System.Linq.Expressions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Application.Features.Affiliates;
using HealthAuthorization.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HealthAuthorization.Infrastructure.Persistence.Repositories;

internal sealed class AffiliateRepository(HealthAuthorizationDbContext context)
    : EfRepository<Affiliate>(context), IAffiliateRepository
{
    private static readonly Expression<Func<Affiliate, AffiliateDto>> Projection = affiliate =>
        new(affiliate.Id, affiliate.AffiliateNumber, affiliate.FirstName, affiliate.LastName,
            affiliate.BirthDate, affiliate.Identification, affiliate.DocumentType, affiliate.ContractorId);

    public Task<bool> NumberExistsAsync(string number, Guid? excludedId, CancellationToken cancellationToken) =>
        Entities.AnyAsync(affiliate => affiliate.AffiliateNumber == number && affiliate.Id != excludedId, cancellationToken);

    public Task<bool> IdentificationExistsAsync(string identification, Guid? excludedId, CancellationToken cancellationToken) =>
        Entities.AnyAsync(affiliate => affiliate.Identification == identification && affiliate.Id != excludedId, cancellationToken);

    public Task<AffiliateDto?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Entities.AsNoTracking().Where(affiliate => affiliate.Id == id)
            .Select(Projection).SingleOrDefaultAsync(cancellationToken);

    public async Task<Page<AffiliateDto>> ListAsync(
        string? search, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = Entities.AsNoTracking();
        if (search is not null)
        {
            query = query.Where(affiliate => affiliate.AffiliateNumber.StartsWith(search) ||
                (affiliate.Identification != null && affiliate.Identification.StartsWith(search)) ||
                affiliate.FirstName.StartsWith(search) || affiliate.LastName.StartsWith(search));
        }

        var total = await query.LongCountAsync(cancellationToken);
        var items = await query.OrderBy(affiliate => affiliate.AffiliateNumber).ThenBy(affiliate => affiliate.Id)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).Select(Projection).ToListAsync(cancellationToken);
        return new Page<AffiliateDto>(items, pageNumber, pageSize, total);
    }
}
