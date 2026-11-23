using System.Linq.Expressions;
using HealthAuthorization.Domain.Common.Interfaces;
using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Common.Entities;

namespace HealthAuthorization.UnitTests.Application;

internal sealed class MemoryRepository<TEntity> : IRepositoryAsync<TEntity> where TEntity : BaseEntity
{
    public MemoryRepository(params TEntity[] items) => Items = [.. items];

    public List<TEntity> Items { get; }
    private IEnumerable<TEntity> Visible => Items.Where(item => item is not ISoftDelete { IsDelete: true });

    public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, bool includeDeleted,
        CancellationToken cancellationToken) =>
        Task.FromResult((includeDeleted ? Items : Visible).Any(predicate.Compile()));

    public Task<TResult?> FindAsync<TResult>(Guid id, Expression<Func<TEntity, TResult>> projection,
        CancellationToken cancellationToken) where TResult : class =>
        Task.FromResult(Visible.Where(item => item.Id == id).Select(projection.Compile()).SingleOrDefault());

    public Task<Page<TResult>> ListAsync<TResult>(Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> order, Expression<Func<TEntity, TResult>> projection,
        int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = Visible.AsQueryable().Where(predicate);
        var page = order(query).Skip((pageNumber - 1) * pageSize).Take(pageSize).Select(projection).ToList();
        return Task.FromResult(new Page<TResult>(page, pageNumber, pageSize, query.LongCount()));
    }

    public Task<TEntity?> GetForUpdateAsync(Guid id, Expression<Func<TEntity, object>> include,
        CancellationToken cancellationToken) => GetForUpdateAsync(id, cancellationToken);

    public int Updates { get; private set; }

    public Task<TEntity?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Visible.SingleOrDefault(item => item.Id == id));
    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Visible.Any(item => item.Id == id));
    public Task<bool> ExistAllAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        Task.FromResult(ids.All(id => Visible.Any(item => item.Id == id)));

    public void Add(TEntity entity) => Items.Add(entity);
    public void Update(TEntity entity, bool forceWrite = false) => Updates++;
}

internal sealed class UnitOfWorkStub : IUnitOfWork
{
    public int SaveCount { get; private set; }
    public Exception? SaveError { get; set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (SaveError != null)
        {
            return Task.FromException<int>(SaveError);
        }

        return Task.FromResult(++SaveCount);
    }
}
