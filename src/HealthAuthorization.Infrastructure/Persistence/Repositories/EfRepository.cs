using System.Linq.Expressions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Domain.Common.Entities;
using Microsoft.EntityFrameworkCore;

namespace HealthAuthorization.Infrastructure.Persistence.Repositories;

internal class EfRepository<TEntity> : IRepositoryAsync<TEntity>
    where TEntity : BaseEntity
{
    private readonly HealthAuthorizationDbContext _context;

    public EfRepository(HealthAuthorizationDbContext context)
    {
        _context = context;
    }

    private DbSet<TEntity> Entities => _context.Set<TEntity>();

    public Task<TEntity?> GetForUpdateAsync(Guid id, Expression<Func<TEntity, object>> include,
        CancellationToken cancellationToken) =>
        Entities.AsTracking().Include(include).SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken);

    public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, bool includeDeleted,
        CancellationToken cancellationToken)
    {
        var query = Entities.AsNoTracking();
        return (includeDeleted ? query.IgnoreQueryFilters() : query).AnyAsync(predicate, cancellationToken);
    }

    public Task<TResult?> FindAsync<TResult>(Guid id, Expression<Func<TEntity, TResult>> projection,
        CancellationToken cancellationToken) where TResult : class =>
        FindAsync(entity => entity.Id == id, projection, cancellationToken);

    public Task<TResult?> FindAsync<TResult>(Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TResult>> projection, CancellationToken cancellationToken) where TResult : class =>
        Entities.AsNoTracking().Where(predicate).Select(projection).SingleOrDefaultAsync(cancellationToken);

    public async Task<Page<TResult>> ListAsync<TResult>(Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> order, Expression<Func<TEntity, TResult>> projection,
        int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = Entities.AsNoTracking().Where(predicate);
        var count = await query.LongCountAsync(cancellationToken);
        var items = await order(query).Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(projection).ToListAsync(cancellationToken);
        return new Page<TResult>(items, pageNumber, pageSize, count);
    }

    public Task<TEntity?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken) =>
        Entities.AsTracking().SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) =>
        Entities.AsNoTracking().AnyAsync(entity => entity.Id == id, cancellationToken);

    public async Task<bool> ExistAllAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        foreach (var keys in ids.Distinct().Chunk(500))
        {
            var count = await Entities.AsNoTracking()
                .CountAsync(entity => keys.Contains(entity.Id), cancellationToken);
            if (count != keys.Length)
            {
                return false;
            }
        }

        return true;
    }

    public void Add(TEntity entity) => Entities.Add(entity);

    public void Update(TEntity entity, bool forceWrite = false)
    {
        var entry = _context.Entry(entity);
        if (entry.State is EntityState.Detached or EntityState.Added or EntityState.Deleted)
        {
            throw new InvalidOperationException("La actualización requiere un registro existente cargado en el contexto actual.");
        }

        if (forceWrite)
        {
            entry.State = EntityState.Modified;
        }
    }
}
