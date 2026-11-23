using System.Linq.Expressions;
using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Domain.Common.Interfaces;
using HealthAuthorization.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HealthAuthorization.Infrastructure.Persistence;

public sealed class HealthAuthorizationDbContext(
    DbContextOptions<HealthAuthorizationDbContext> options, TimeProvider clock, ICurrentUser currentUser)
    : DbContext(options)
{
    public DbSet<Affiliate> Affiliates => Set<Affiliate>();
    public DbSet<Contractor> Contractors => Set<Contractor>();
    public DbSet<Intermediary> Intermediaries => Set<Intermediary>();
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<ProviderType> ProviderTypes => Set<ProviderType>();
    public DbSet<ServiceType> ServiceTypes => Set<ServiceType>();
    public DbSet<Authorization> Authorizations => Set<Authorization>();
    public DbSet<AuthorizationDetail> AuthorizationDetails => Set<AuthorizationDetail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HealthAuthorizationDbContext).Assembly);
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDelete).IsAssignableFrom(entity.ClrType))
            {
                var row = Expression.Parameter(entity.ClrType, "row");
                var visible = Expression.Not(Expression.Property(row, nameof(ISoftDelete.IsDelete)));
                modelBuilder.Entity(entity.ClrType).HasQueryFilter(Expression.Lambda(visible, row));
            }
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PrepareChanges();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        PrepareChanges();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void PrepareChanges()
    {
        foreach (var entry in ChangeTracker.Entries().Where(entry => entry.State == EntityState.Deleted).ToArray())
        {
            if (entry.Entity is not ISoftDelete)
            {
                throw new InvalidOperationException("Esta entidad no admite eliminación.");
            }

            entry.State = EntityState.Unchanged;
            entry.Property(nameof(ISoftDelete.IsDelete)).CurrentValue = true;
            entry.Property(nameof(ISoftDelete.IsDelete)).IsModified = true;
        }

        var now = clock.GetUtcNow();
        var userId = currentUser.UserId;
        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedDate = now;
                entry.Entity.CreatedBy = userId;
                entry.Entity.LastUpdatedDate = null;
                entry.Entity.LastUpdatedBy = null;
            }
            else if (entry.State == EntityState.Modified)
            {
                var createdDate = entry.Property(entity => entity.CreatedDate);
                var createdBy = entry.Property(entity => entity.CreatedBy);
                createdDate.CurrentValue = createdDate.OriginalValue;
                createdBy.CurrentValue = createdBy.OriginalValue;
                createdDate.IsModified = false;
                createdBy.IsModified = false;
                entry.Entity.LastUpdatedDate = now;
                entry.Entity.LastUpdatedBy = userId;
            }
        }
    }
}
