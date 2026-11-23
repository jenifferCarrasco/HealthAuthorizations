using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Domain.Common.Entities;
using Microsoft.EntityFrameworkCore;

namespace HealthAuthorization.Infrastructure.Persistence.Repositories;

internal class EfRepository<TEntity>(HealthAuthorizationDbContext context) : IRepositoryAsync<TEntity>
    where TEntity : BaseEntity
{
    protected DbSet<TEntity> Entities => context.Set<TEntity>();

    public virtual Task<TEntity?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken) =>
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

    public void Update(TEntity entity)
    {
        var entry = context.Entry(entity);
        if (entry.State is EntityState.Detached or EntityState.Added or EntityState.Deleted)
        {
            throw new InvalidOperationException("La actualización requiere un registro existente cargado en el contexto actual.");
        }

        entry.State = EntityState.Modified;
    }
}
