using System.Linq.Expressions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Common.Entities;

namespace HealthAuthorization.Application.Abstractions;

public interface IRepositoryAsync<TEntity> where TEntity : BaseEntity
{
    Task<TEntity?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistAllAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, bool includeDeleted, CancellationToken cancellationToken);
    Task<TResult?> FindAsync<TResult>(Guid id, Expression<Func<TEntity, TResult>> projection,
        CancellationToken cancellationToken) where TResult : class;
    Task<Page<TResult>> ListAsync<TResult>(Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> order,
        Expression<Func<TEntity, TResult>> projection, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<TEntity?> GetForUpdateAsync(Guid id, Expression<Func<TEntity, object>> include, CancellationToken cancellationToken);
    void Add(TEntity entity);
    void Update(TEntity entity, bool forceWrite = false);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface ICurrentUser
{
    Guid? UserId { get; }
}
