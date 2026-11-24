using HealthAuthorization.Application;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Application.Features.Authorizations;
using HealthAuthorization.Application.Features.Authorizations.Commands;
using HealthAuthorization.Application.Features.Authorizations.Notifications;
using HealthAuthorization.Domain.Entities;
using HealthAuthorization.Infrastructure;
using HealthAuthorization.Infrastructure.Persistence;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace HealthAuthorization.IntegrationTests.Persistence;

public class AuthorizationIdempotencySqlServerTests
{
    public const string ConnectionVariable = "HEALTHAUTHORIZATION_IDEMPOTENCY_TEST_CONNECTION";
    private const string TestDatabase = "HealthAuthorizationIdempotencyTests";

    [SqlServerIdempotencyTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConcurrentRequestsUseTheDatabaseWinner(bool differentPayloads)
    {
        var configured = Environment.GetEnvironmentVariable(ConnectionVariable);
        var connection = new SqlConnectionStringBuilder(configured!);
        Assert.Equal(TestDatabase, connection.InitialCatalog);
        Assert.True(string.IsNullOrEmpty(connection.AttachDBFilename));

        var barrier = new ConcurrentInsertBarrier();
        var notifications = new NotificationCounter();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication().AddInfrastructure(connection.ConnectionString);
        services.AddSingleton<IAuthorizationNotificationService>(notifications);
        services.ConfigureDbContext<HealthAuthorizationDbContext>(options => options.AddInterceptors(barrier));
        await using var provider = services.BuildServiceProvider();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var token = timeout.Token;
        var key = Guid.NewGuid();

        await using var verificationScope = provider.CreateAsyncScope();
        var context = verificationScope.ServiceProvider.GetRequiredService<HealthAuthorizationDbContext>();
        await context.Database.OpenConnectionAsync(token);
        await using var checkDatabase = context.Database.GetDbConnection().CreateCommand();
        checkDatabase.CommandText = "SELECT DB_NAME()";
        Assert.Equal(TestDatabase, await checkDatabase.ExecuteScalarAsync(token));
        Assert.Empty(await context.Database.GetPendingMigrationsAsync(token));

        var outcomes = await Task.WhenAll(
            SendAsync(provider, key, 100m, token),
            SendAsync(provider, key, differentPayloads ? 200m : 100m, token));

        var stored = await context.Authorizations.AsNoTracking()
            .Include(authorization => authorization.Details)
            .SingleAsync(authorization => authorization.IdempotencyScopeId == Guid.Empty &&
                authorization.IdempotencyKey == key, token);

        Assert.Single(stored.Details);
        Assert.Equal(1, notifications.Created);
        Assert.Equal(2, barrier.Attempts);
        Assert.Single(outcomes, outcome => !outcome.TracksAuthorization);
        Assert.All(outcomes, outcome => Assert.Equal(EntityState.Unchanged, outcome.UnrelatedState));

        if (differentPayloads)
        {
            Assert.Single(outcomes, outcome => outcome.Conflict);
            Assert.Equal(stored.Id, Assert.Single(outcomes, outcome => !outcome.Conflict).Id);
        }
        else
        {
            Assert.All(outcomes, outcome =>
            {
                Assert.False(outcome.Conflict);
                Assert.Equal(stored.Id, outcome.Id);
            });
        }
    }

    private static async Task<(Guid Id, bool Conflict, bool TracksAuthorization, EntityState UnrelatedState)> SendAsync(
        IServiceProvider provider, Guid key, decimal claimed, CancellationToken cancellationToken)
    {
        await using var scope = provider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var context = scope.ServiceProvider.GetRequiredService<HealthAuthorizationDbContext>();
        var unrelated = new ProviderType();
        context.Attach(unrelated);
        var input = new AuthorizationInput(
            Guid.Parse("000000cb-0000-0000-0000-000000000000"),
            new[]
            {
                new AuthorizationServiceInput(
                    Guid.Parse("00000065-0000-0000-0000-000000000000"),
                    Guid.Parse("0000000b-0000-0000-0000-000000000000"), claimed)
            });

        try
        {
            var id = await sender.Send(new CreateAuthorizationCommand(input, key), cancellationToken);
            return (id, false, TracksAuthorization(context), context.Entry(unrelated).State);
        }
        catch (ConflictException exception)
        {
            Assert.Equal("La clave de idempotencia ya se utilizó con datos diferentes.", exception.Message);
            return (Guid.Empty, true, TracksAuthorization(context), context.Entry(unrelated).State);
        }
    }

    private static bool TracksAuthorization(HealthAuthorizationDbContext context)
    {
        return context.ChangeTracker.Entries().Any(entry => entry.Entity is Authorization or AuthorizationDetail);
    }

    private class ConcurrentInsertBarrier : SaveChangesInterceptor
    {
        private readonly TaskCompletionSource _ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _attempts;

        public int Attempts => _attempts;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _attempts) == 2)
            {
                _ready.TrySetResult();
            }

            await _ready.Task.WaitAsync(cancellationToken);
            return result;
        }
    }

    private class NotificationCounter : IAuthorizationNotificationService
    {
        private int _created;
        public int Created => _created;

        public Task NotifyCreatedAsync(AuthorizationCreatedNotification notification, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _created);
            return Task.CompletedTask;
        }

        public Task NotifyStatusChangedAsync(AuthorizationStatusChangedNotification notification, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

public class SqlServerIdempotencyTheoryAttribute : TheoryAttribute
{
    public SqlServerIdempotencyTheoryAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(
            AuthorizationIdempotencySqlServerTests.ConnectionVariable)))
        {
            Skip = "Configure una base independiente para ejecutar las pruebas de concurrencia con SQL Server.";
        }
    }
}
