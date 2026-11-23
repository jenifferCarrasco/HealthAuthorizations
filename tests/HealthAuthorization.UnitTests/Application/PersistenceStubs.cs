using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Application.Features.Affiliates;
using HealthAuthorization.Application.Features.Authorizations;
using HealthAuthorization.Domain.Common.Entities;
using HealthAuthorization.Domain.Entities;

namespace HealthAuthorization.UnitTests.Application;

internal class MemoryRepository<TEntity>(params TEntity[] items) : IRepositoryAsync<TEntity> where TEntity : BaseEntity
{
    public List<TEntity> Items { get; } = [.. items];
    public int Updates { get; private set; }

    public Task<TEntity?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Items.SingleOrDefault(item => item.Id == id));
    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Items.Any(item => item.Id == id));
    public Task<bool> ExistAllAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        Task.FromResult(ids.All(id => Items.Any(item => item.Id == id)));

    public void Add(TEntity entity) => Items.Add(entity);
    public void Update(TEntity entity) => Updates++;
}

internal sealed class AffiliateRepositoryStub(params Affiliate[] items) : MemoryRepository<Affiliate>(items), IAffiliateRepository
{
    public Task<bool> NumberExistsAsync(string number, Guid? excludedId, CancellationToken cancellationToken) =>
        Task.FromResult(Items.Any(item => item.Id != excludedId && item.AffiliateNumber == number));

    public Task<bool> IdentificationExistsAsync(string identification, Guid? excludedId, CancellationToken cancellationToken) =>
        Task.FromResult(Items.Any(item => item.Id != excludedId && item.Identification == identification));

    public Task<AffiliateDto?> GetAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<Page<AffiliateDto>> ListAsync(string? search, int pageNumber, int pageSize, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}

internal sealed class AuthorizationRepositoryStub(params Authorization[] items)
    : MemoryRepository<Authorization>(items), IAuthorizationRepository
{
    public Task<AuthorizationDto?> GetAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<Page<AuthorizationSummaryDto>> ListAsync(
        AuthorizationFilters filters, int pageNumber, int pageSize, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}

internal sealed class UnitOfWorkStub : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(++SaveCount);
}
