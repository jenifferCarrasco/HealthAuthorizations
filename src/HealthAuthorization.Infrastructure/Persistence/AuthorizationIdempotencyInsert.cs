using HealthAuthorization.Domain.Entities;
using HealthAuthorization.Infrastructure.Persistence.Configurations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HealthAuthorization.Infrastructure.Persistence;

internal static class AuthorizationIdempotencyInsert
{
    internal static bool TryDetachFailedAggregate(HealthAuthorizationDbContext context, DbUpdateException exception,
        SqlException sqlException)
    {
        var indexName = $"'{AuthorizationConfiguration.IdempotencyIndexName}'";
        if (!sqlException.Errors.Cast<SqlError>().Any(error =>
                error.Number is 2601 or 2627 && error.Message.Contains(indexName, StringComparison.Ordinal)))
        {
            return false;
        }

        var candidates = exception.Entries.Where(entry => entry.State == EntityState.Added &&
                entry.Entity is Authorization { IdempotencyKey: not null })
            .Take(2).ToArray();
        if (candidates.Length != 1)
        {
            return false;
        }

        var root = candidates[0];
        var authorization = (Authorization)root.Entity;
        foreach (var detail in authorization.Details.ToArray())
        {
            var entry = context.Entry(detail);
            if (entry.State == EntityState.Added && detail.AuthorizationId == authorization.Id)
            {
                entry.State = EntityState.Detached;
            }
        }

        root.State = EntityState.Detached;
        return true;
    }
}
