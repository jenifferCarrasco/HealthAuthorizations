using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Application.Features.Authorizations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HealthAuthorization.Infrastructure.Persistence;

internal class UnitOfWork : IUnitOfWork
{
    private readonly HealthAuthorizationDbContext _context;

    public UnitOfWork(HealthAuthorizationDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException("El registro cambió durante la operación. Vuelva a consultarlo antes de reintentar.", exception);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 } sqlException)
        {
            if (AuthorizationIdempotencyInsert.TryDetachFailedAggregate(_context, exception, sqlException))
            {
                throw new AuthorizationIdempotencyConflictException(exception);
            }

            throw new ConflictException("Ya existe un registro con los mismos datos únicos.", exception);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 547 })
        {
            throw new ConflictException("La operación incumple una restricción de integridad.", exception);
        }
    }
}
