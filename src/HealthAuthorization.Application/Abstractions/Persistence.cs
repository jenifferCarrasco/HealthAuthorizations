using HealthAuthorization.Domain.Common.Entities;

namespace HealthAuthorization.Application.Abstractions;

public interface IRepositoryAsync<TEntity> where TEntity : BaseEntity
{
    Task<TEntity?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistAllAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
    void Add(TEntity entity);
    void Update(TEntity entity);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface ICurrentUser
{
    Guid? UserId { get; }
}
