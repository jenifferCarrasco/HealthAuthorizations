using FluentValidation;
using HealthAuthorization.Application;
using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Application.Features.Affiliates;
using HealthAuthorization.Application.Features.Affiliates.Commands;
using HealthAuthorization.Application.Features.Authorizations;
using HealthAuthorization.Application.Features.Authorizations.Notifications;
using HealthAuthorization.Domain.Entities;
using HealthAuthorization.Infrastructure;
using HealthAuthorization.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace HealthAuthorization.IntegrationTests.Persistence;

public sealed class SqlServerPersistenceTests
{
    private const string ConnectionString = "Server=localhost;Database=HealthAuthorizationModelOnly;Integrated Security=true;Encrypt=true";

    [Fact]
    public void GeneratesSqlServerSchemaWithFilteredIndexesAndIntegrityConstraints()
    {
        using var context = CreateContext();
        var sql = context.Database.GenerateCreateScript();

        Assert.Contains("CREATE UNIQUE INDEX [IX_Affiliates_AffiliateNumber]", sql);
        Assert.Contains("CREATE UNIQUE INDEX [IX_Affiliates_Identification] ON [Affiliates] ([Identification]) WHERE [Identification] IS NOT NULL", sql);
        Assert.Contains("[DocumentType] int NOT NULL", sql);
        Assert.Contains("[ClaimedAmount] decimal(19,2) NOT NULL", sql);
        Assert.Contains("CK_Affiliates_Document", sql);
        Assert.Contains("CK_AuthorizationDetails_Amounts", sql);
        Assert.Contains("rowversion", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[IdempotencyRequestHash] binary(32) NULL", sql);
        Assert.Contains("CK_Authorizations_Idempotency", sql);
        Assert.Contains("CREATE UNIQUE INDEX [UX_Authorizations_IdempotencyScope_Key] ON [Authorizations] ([IdempotencyScopeId], [IdempotencyKey]) WHERE [IdempotencyKey] IS NOT NULL", sql);
        Assert.Contains("DEMO-001", sql);
        Assert.DoesNotContain("ON DELETE CASCADE", sql);
        Assert.Equal(8, context.Model.GetEntityTypes().Count());
    }

    [Fact]
    public void PublishedMigrationsCanUpgradeAnExistingDatabaseWithoutRecreatingCatalogs()
    {
        using var context = CreateContext();
        var migrator = context.GetService<IMigrator>();
        const string initial = "20261123174056_InitialCreate";
        const string idempotency = "20261124004932_AddAuthorizationIdempotency";

        var freshInstall = migrator.GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
        var upgrade = migrator.GenerateScript(initial, idempotency, MigrationsSqlGenerationOptions.Idempotent);
        var downgrade = migrator.GenerateScript(idempotency, initial);

        Assert.Contains("CREATE TABLE [Authorizations]", freshInstall);
        Assert.Contains("DEMO-001", freshInstall);
        Assert.Contains("[__EFMigrationsHistory]", upgrade);
        Assert.Contains("ALTER TABLE [Authorizations] ADD [IdempotencyRequestHash] binary(32) NULL", upgrade);
        Assert.Contains("CREATE UNIQUE INDEX [UX_Authorizations_IdempotencyScope_Key]", upgrade);
        Assert.DoesNotContain("CREATE TABLE", upgrade);
        Assert.DoesNotContain("DROP TABLE", upgrade);
        Assert.DoesNotContain("DEMO-001", upgrade);
        Assert.Contains("DROP INDEX [UX_Authorizations_IdempotencyScope_Key] ON [Authorizations]", downgrade);
        Assert.DoesNotContain("DROP TABLE", downgrade);
        Assert.False(context.Database.HasPendingModelChanges());
    }

    [Fact]
    public void AppliesDeletionFilterToDetailsWhileKeepingInactiveAuthorizationsQueryable()
    {
        using var context = CreateContext();
        var detailSql = context.AuthorizationDetails.ToQueryString();
        var authorizationSql = context.Authorizations.ToQueryString();
        var includedDetailsSql = context.Authorizations.Include(authorization => authorization.Details).ToQueryString();

        Assert.Contains("WHERE", detailSql);
        Assert.Contains("WHERE", includedDetailsSql);
        Assert.Contains("[IsDelete]", detailSql);
        Assert.DoesNotContain("WHERE", authorizationSql);
        Assert.Contains("[IsDelete]", context.Affiliates.ToQueryString());

        var authorizationEntity = context.Model.FindEntityType(typeof(Authorization))!;
        Assert.True(authorizationEntity.FindProperty("RowVersion")!.IsConcurrencyToken);
        Assert.Equal(ValueGenerated.OnAddOrUpdate, authorizationEntity.FindProperty("RowVersion")!.ValueGenerated);
    }

    [Fact]
    public async Task StampsUtcAuditValuesAndProtectsCreationFields()
    {
        var originalUser = Guid.NewGuid();
        var updateUser = Guid.NewGuid();
        var current = new CurrentUser { UserId = originalUser };
        var clock = new FixedClock();
        await using var context = CreateContext(current, clock);
        var affiliate = new Affiliate { AffiliateNumber = "AF-1" };
        context.Affiliates.Add(affiliate);

        await context.SaveChangesAsync();
        Assert.Equal(clock.Now, affiliate.CreatedDate);
        Assert.Equal(originalUser, affiliate.CreatedBy);
        context.ChangeTracker.AcceptAllChanges();

        var createdDate = affiliate.CreatedDate;
        clock.Now = clock.Now.AddHours(1);
        current.UserId = updateUser;
        affiliate.CreatedDate = DateTimeOffset.MinValue;
        affiliate.CreatedBy = Guid.NewGuid();
        affiliate.FirstName = "Ana";

        await context.SaveChangesAsync();

        Assert.Equal(createdDate, affiliate.CreatedDate);
        Assert.Equal(originalUser, affiliate.CreatedBy);
        Assert.Equal(clock.Now, affiliate.LastUpdatedDate);
        Assert.Equal(updateUser, affiliate.LastUpdatedBy);
        Assert.False(context.Entry(affiliate).Property(entity => entity.CreatedBy).IsModified);
    }

    [Fact]
    public async Task RemovingADetailMarksItDeletedWithoutIssuingAPhysicalDelete()
    {
        await using var context = CreateContext();
        var detail = new AuthorizationDetail { AuthorizationId = Guid.NewGuid() };
        context.Attach(detail);
        context.Remove(detail);

        await context.SaveChangesAsync();

        Assert.True(detail.IsDelete);
        Assert.Equal(EntityState.Modified, context.Entry(detail).State);
        Assert.True(context.Entry(detail).Property(entity => entity.IsDelete).IsModified);
    }

    [Fact]
    public async Task TracksAnAggregateUpdateAndNewDetailInTheSameUnitOfWork()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(ConnectionString);
        services.ConfigureDbContext<HealthAuthorizationDbContext>(options =>
            options.AddInterceptors(new SuppressDatabaseWrite()));
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<HealthAuthorizationDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IRepositoryAsync<Authorization>>();
        var unit = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var authorization = new Authorization { AffiliateId = Guid.NewGuid() };
        var removed = new AuthorizationDetail { AuthorizationId = authorization.Id };
        authorization.Details.Add(removed);
        context.Attach(authorization);
        var version = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 };
        context.Entry(authorization).Property("RowVersion").OriginalValue = version;

        removed.IsDelete = true;
        var added = new AuthorizationDetail { AuthorizationId = authorization.Id };
        authorization.Details.Add(added);
        repository.Update(authorization, forceWrite: true);
        await unit.SaveChangesAsync();

        Assert.Equal(EntityState.Modified, context.Entry(authorization).State);
        Assert.Equal(version, context.Entry(authorization).Property<byte[]>("RowVersion").OriginalValue);
        Assert.Equal(EntityState.Modified, context.Entry(removed).State);
        Assert.Equal(EntityState.Added, context.Entry(added).State);
        Assert.NotNull(authorization.LastUpdatedDate);
        Assert.Null(authorization.LastUpdatedBy);
    }

    [Fact]
    public async Task UnitOfWorkTranslatesConcurrencyExceptionWithoutRetryingTheWrite()
    {
        var failure = new DbUpdateConcurrencyException("No se actualizó ninguna fila.");
        var interceptor = new RejectConcurrentWrite(failure);
        var services = new ServiceCollection();
        services.AddInfrastructure(ConnectionString);
        services.ConfigureDbContext<HealthAuthorizationDbContext>(options => options.AddInterceptors(interceptor));
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<HealthAuthorizationDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IRepositoryAsync<Affiliate>>();
        var unit = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var affiliate = new Affiliate { AffiliateNumber = "CONC-1", FirstName = "Ana" };
        context.Attach(affiliate);
        affiliate.FirstName = "María";
        repository.Update(affiliate);

        var conflict = await Assert.ThrowsAsync<ConflictException>(() => unit.SaveChangesAsync());

        Assert.Same(failure, conflict.InnerException);
        Assert.Contains("Vuelva a consultarlo", conflict.Message);
        Assert.Equal(1, interceptor.Attempts);
    }

    [Fact]
    public async Task MediatorRejectsInvalidCommandsBeforeAccessingSqlServer()
    {
        var services = new ServiceCollection();
        services.AddApplication().AddInfrastructure(ConnectionString);
        services.AddLogging();
        services.AddSingleton<IAuthorizationNotificationService, NotificationServiceStub>();
        await using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        await using var scope = provider.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();
        var unit = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        Assert.Same(unit, scope.ServiceProvider.GetRequiredService<IUnitOfWork>());

        var exception = await Assert.ThrowsAsync<ValidationException>(() => mediator.Send(
            new CreateAffiliateCommand(new AffiliateInput("", "Ana", "Pérez", new DateOnly(2010, 1, 1)))));

        Assert.Contains(exception.Errors, error => error.ErrorMessage == "El número de afiliado es obligatorio.");
    }

    private static HealthAuthorizationDbContext CreateContext(CurrentUser? user = null, FixedClock? clock = null)
    {
        var options = new DbContextOptionsBuilder<HealthAuthorizationDbContext>()
            .UseSqlServer(ConnectionString)
            .AddInterceptors(new SuppressDatabaseWrite())
            .Options;
        return new HealthAuthorizationDbContext(options, clock ?? new FixedClock(), user ?? new CurrentUser());
    }

    private class NotificationServiceStub : IAuthorizationNotificationService
    {
        public Task NotifyCreatedAsync(AuthorizationCreatedNotification notification, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task NotifyStatusChangedAsync(AuthorizationStatusChangedNotification notification,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class CurrentUser : ICurrentUser
    {
        public Guid? UserId { get; set; }
    }

    private sealed class FixedClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 11, 23, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class SuppressDatabaseWrite : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(InterceptionResult<int>.SuppressWithResult(1));
    }

    private class RejectConcurrentWrite : SaveChangesInterceptor
    {
        private readonly DbUpdateConcurrencyException _failure;

        public RejectConcurrentWrite(DbUpdateConcurrencyException failure)
        {
            _failure = failure;
        }

        public int Attempts { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Attempts++;
            return ValueTask.FromException<InterceptionResult<int>>(_failure);
        }
    }
}
