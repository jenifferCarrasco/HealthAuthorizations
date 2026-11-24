using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using HealthAuthorization.API.Controllers;
using HealthAuthorization.API.Errors;
using HealthAuthorization.API.Hubs;
using HealthAuthorization.API.Notifications;
using HealthAuthorization.API.Security;
using HealthAuthorization.Application;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Application.Features.Affiliates;
using HealthAuthorization.Application.Features.Authorizations;
using HealthAuthorization.Application.Features.Authorizations.Notifications;
using HealthAuthorization.Domain.Entities;
using HealthAuthorization.Infrastructure;
using HealthAuthorization.Infrastructure.Persistence;
using HealthAuthorization.IntegrationTests.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Authorization = HealthAuthorization.Domain.Entities.Authorization;

namespace HealthAuthorization.IntegrationTests.Persistence;

public class SqlServerBusinessLifecycleTests
{
    private static readonly Guid ProviderId = Guid.Parse("00000065-0000-0000-0000-000000000000");
    private static readonly Guid ServiceId = Guid.Parse("0000000b-0000-0000-0000-000000000000");

    [SqlServerBusinessFact]
    public async Task ConstraintFailureRollsBackTheAggregateAndTheSameKeyCanBeRetried()
    {
        var invalidDetail = new InvalidDetailOnFirstInsert();
        var tokens = new DevelopmentJwtTestTokens();
        var notifications = new NotificationCounter();
        await using var app = await StartAsync(tokens, notifications, invalidDetail);
        using var client = CreateClient(app, tokens, Guid.NewGuid());
        var affiliateId = await CreateAffiliateAsync(client);
        var key = Guid.NewGuid();
        var input = new AuthorizationInput(affiliateId,
            [new AuthorizationServiceInput(ProviderId, ServiceId, 100m), new AuthorizationServiceInput(ProviderId, ServiceId, 50m)]);

        using var rejected = await CreateAuthorizationAsync(client, key, input);
        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
        Assert.Equal(547, invalidDetail.SqlErrorNumber);
        Assert.Contains("CK_AuthorizationDetails_Amounts", invalidDetail.SqlErrorMessage);
        Assert.Empty(await ReadAuthorizationsAsync(app, key));
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HealthAuthorizationDbContext>();
            Assert.False(await context.AuthorizationDetails.IgnoreQueryFilters()
                .AnyAsync(detail => invalidDetail.DetailIds.Contains(detail.Id)));
        }
        Assert.Equal(0, notifications.Created);

        using var accepted = await CreateAuthorizationAsync(client, key, input);
        Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);
        var stored = Assert.Single(await ReadAuthorizationsAsync(app, key));
        Assert.Equal(2, stored.Details.Count);
        Assert.Equal(150m, stored.TotalClaimedAmount);
        Assert.Equal(105m, stored.TotalArsAmount);
        Assert.Equal(45m, stored.TotalAffiliateAmount);
        Assert.Equal(1, notifications.Created);

        using var replay = await CreateAuthorizationAsync(client, key, input);
        Assert.Equal(HttpStatusCode.Created, replay.StatusCode);
        Assert.Equal(accepted.Headers.Location, replay.Headers.Location);
        Assert.Equal(1, notifications.Created);

        using var otherUser = CreateClient(app, tokens, Guid.NewGuid());
        using var independent = await CreateAuthorizationAsync(otherUser, key, input);
        Assert.Equal(HttpStatusCode.Created, independent.StatusCode);
        var rows = await ReadAuthorizationsAsync(app, key);
        Assert.Equal(2, rows.Count);
        Assert.Equal(2, rows.Select(row => row.IdempotencyScopeId).Distinct().Count());
        Assert.All(rows, row => Assert.Equal(2, row.Details.Count));
        Assert.Equal(2, notifications.Created);
    }

    [SqlServerBusinessTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConcurrentAuthorizationUpdateReturns409AndKeepsOnlyWinnerChanges(bool inactivate)
    {
        var pause = new PausedWrite(context => context.ChangeTracker.Entries<AuthorizationDetail>()
            .Any(entry => entry.State == EntityState.Added && entry.Entity.ClaimedAmount == 99m));
        var tokens = new DevelopmentJwtTestTokens();
        var notifications = new NotificationCounter();
        await using var app = await StartAsync(tokens, notifications, pause);
        var creator = Guid.NewGuid();
        var winnerUser = Guid.NewGuid();
        using var firstClient = CreateClient(app, tokens, creator);
        using var winnerClient = CreateClient(app, tokens, winnerUser);
        var affiliateId = await CreateAffiliateAsync(firstClient);
        var key = Guid.NewGuid();
        using var created = await CreateAuthorizationAsync(firstClient, key, new AuthorizationInput(affiliateId,
            [new AuthorizationServiceInput(ProviderId, ServiceId, 100m), new AuthorizationServiceInput(ProviderId, ServiceId, 50m)]));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var location = created.Headers.Location!;
        var original = Assert.Single(await ReadAuthorizationsAsync(app, key));
        var retained = Assert.Single(original.Details, detail => detail.ClaimedAmount == 100m);
        var omitted = Assert.Single(original.Details, detail => detail.ClaimedAmount == 50m);
        var originalVersion = await ReadVersionAsync<Authorization>(app, original.Id);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(25));
        var rejected = await CompleteRaceAsync(pause, timeout.Token,
            async () => await firstClient.PutAsJsonAsync(location, new AuthorizationInput(affiliateId,
                [new AuthorizationServiceInput(ProviderId, ServiceId, 1m, retained.Id),
                 new AuthorizationServiceInput(ProviderId, ServiceId, 99m)]), timeout.Token),
            async () => inactivate
                ? await winnerClient.PatchAsJsonAsync(location.AbsoluteUri + "/status", new { isActive = false }, timeout.Token)
                : await winnerClient.PutAsJsonAsync(location, new AuthorizationInput(affiliateId,
                    [new AuthorizationServiceInput(ProviderId, ServiceId, 200m, retained.Id)]), timeout.Token));
        Assert.Equal(HttpStatusCode.Conflict, rejected.Status);
        Assert.Contains("Vuelva a consultarlo", rejected.Body);
        var final = Assert.Single(await ReadAuthorizationsAsync(app, key));
        Assert.Equal(creator, final.CreatedBy);
        Assert.Equal(winnerUser, final.LastUpdatedBy);
        Assert.Equal(!inactivate, final.IsActive);
        Assert.Equal(inactivate ? 150m : 200m, final.TotalClaimedAmount);
        Assert.Equal(inactivate ? 105m : 140m, final.TotalArsAmount);
        Assert.Equal(inactivate ? 45m : 60m, final.TotalAffiliateAmount);
        Assert.Equal(2, final.Details.Count);
        Assert.DoesNotContain(final.Details, detail => detail.ClaimedAmount is 1m or 99m);
        var historical = Assert.Single(final.Details, detail => detail.Id == omitted.Id);
        Assert.Equal(50m, historical.ClaimedAmount);
        Assert.Equal(!inactivate, historical.IsDelete);
        var finalVersion = await ReadVersionAsync<Authorization>(app, original.Id);
        Assert.False(originalVersion.SequenceEqual(finalVersion));

        using var deleted = await winnerClient.DeleteAsync($"/api/affiliates/{affiliateId}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        using var hidden = await winnerClient.GetAsync($"/api/affiliates/{affiliateId}");
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        var detailView = await winnerClient.GetFromJsonAsync<AuthorizationDto>(location);
        Assert.NotNull(detailView);
        Assert.Equal(final.TotalClaimedAmount, detailView.Summary.TotalClaimedAmount);
        var list = await winnerClient.GetFromJsonAsync<Page<AuthorizationSummaryDto>>($"/api/authorizations?affiliateId={affiliateId}");
        Assert.NotNull(list);
        Assert.Equal(original.Id, Assert.Single(list.Items).Id);

        await using var scope = app.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<HealthAuthorizationDbContext>();
        var archivedAffiliate = await context.Affiliates.IgnoreQueryFilters().SingleAsync(row => row.Id == affiliateId);
        Assert.True(archivedAffiliate.IsDelete);
        Assert.Equal(winnerUser, archivedAffiliate.LastUpdatedBy);
    }

    [SqlServerBusinessFact]
    public async Task ConcurrentAffiliateUpdatesReturn409AndPreserveWinningIdentity()
    {
        var pause = new PausedWrite(context => context.ChangeTracker.Entries<Affiliate>()
            .Any(entry => entry.State == EntityState.Modified && entry.Entity.FirstName == "Perdedor"));
        var tokens = new DevelopmentJwtTestTokens();
        await using var app = await StartAsync(tokens, new NotificationCounter(), pause);
        var creator = Guid.NewGuid();
        var winnerUser = Guid.NewGuid();
        using var firstClient = CreateClient(app, tokens, creator);
        using var winnerClient = CreateClient(app, tokens, winnerUser);
        var id = await CreateAffiliateAsync(firstClient);
        var path = $"/api/affiliates/{id}";
        var original = await firstClient.GetFromJsonAsync<AffiliateDto>(path);
        Assert.NotNull(original);
        var oldVersion = await ReadVersionAsync<Affiliate>(app, id);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(25));
        var rejected = await CompleteRaceAsync(pause, timeout.Token,
            async () => await firstClient.PutAsJsonAsync(path, new AffiliateInput(original.AffiliateNumber,
                "Perdedor", "No persistir", original.BirthDate), timeout.Token),
            async () => await winnerClient.PutAsJsonAsync(path, new AffiliateInput(original.AffiliateNumber,
                "Ganador", original.LastName, original.BirthDate), timeout.Token));
        Assert.Equal(HttpStatusCode.Conflict, rejected.Status);

        await using var scope = app.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<HealthAuthorizationDbContext>();
        var stored = await context.Affiliates.AsNoTracking().SingleAsync(row => row.Id == id);
        Assert.Equal("Ganador", stored.FirstName);
        Assert.Equal(original.LastName, stored.LastName);
        Assert.Equal(creator, stored.CreatedBy);
        Assert.Equal(winnerUser, stored.LastUpdatedBy);
        Assert.NotNull(stored.LastUpdatedDate);
        var finalVersion = await ReadVersionAsync<Affiliate>(app, id);
        Assert.False(oldVersion.SequenceEqual(finalVersion));
    }

    [SqlServerBusinessFact]
    public async Task SqlServerRejectsAnOrphanAndDuplicateAffiliateNumber()
    {
        var tokens = new DevelopmentJwtTestTokens();
        await using var app = await StartAsync(tokens, new NotificationCounter());
        using var client = CreateClient(app, tokens, Guid.NewGuid());
        var id = await CreateAffiliateAsync(client);

        await using var scope = app.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<HealthAuthorizationDbContext>();
        var original = await context.Affiliates.AsNoTracking().SingleAsync(row => row.Id == id);
        var duplicate = new Affiliate
        {
            AffiliateNumber = original.AffiliateNumber,
            FirstName = "Duplicado",
            LastName = "Prueba",
            BirthDate = original.BirthDate
        };
        context.Affiliates.Add(duplicate);
        var unique = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        var uniqueError = Assert.IsType<SqlException>(unique.InnerException);
        Assert.Contains(uniqueError.Number, new[] { 2601, 2627 });
        Assert.Contains("IX_Affiliates_AffiliateNumber", uniqueError.Message);
        context.Entry(duplicate).State = EntityState.Detached;

        var orphan = new Authorization { AffiliateId = Guid.NewGuid() };
        context.Authorizations.Add(orphan);
        var foreignKey = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        var foreignKeyError = Assert.IsType<SqlException>(foreignKey.InnerException);
        Assert.Equal(547, foreignKeyError.Number);
        Assert.Contains("FK_Authorizations_Affiliates_AffiliateId", foreignKeyError.Message);
        Assert.False(await context.Authorizations.AnyAsync(row => row.Id == orphan.Id));
        Assert.Equal(1, await context.Affiliates.CountAsync(row => row.AffiliateNumber == original.AffiliateNumber));
    }

    [SqlServerBusinessFact]
    public async Task InactiveCatalogReferencesAreRejectedForNewDetailsButHistoricalReferencesAndReplayRemainAvailable()
    {
        var tokens = new DevelopmentJwtTestTokens();
        await using var app = await StartAsync(tokens, new NotificationCounter());
        using var client = CreateClient(app, tokens, Guid.NewGuid());
        var affiliateId = await CreateAffiliateAsync(client);
        using var service = await client.PostAsJsonAsync("/api/service-types", new { name = $"Prueba {Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.Created, service.StatusCode);
        var servicePath = service.Headers.Location!;
        var serviceId = Guid.Parse(servicePath.Segments.Last());
        var key = Guid.NewGuid();
        var original = new AuthorizationInput(affiliateId, [new AuthorizationServiceInput(ProviderId, serviceId, 100m)]);
        using var created = await CreateAuthorizationAsync(client, key, original);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var location = created.Headers.Location!;
        var stored = Assert.Single(await ReadAuthorizationsAsync(app, key));
        var detail = Assert.Single(stored.Details);

        using var deactivateService = await client.PatchAsJsonAsync(servicePath.AbsoluteUri + "/status", new { isActive = false });
        Assert.Equal(HttpStatusCode.NoContent, deactivateService.StatusCode);
        using var deactivateAffiliate = await client.PatchAsJsonAsync($"/api/affiliates/{affiliateId}/status", new { isActive = false });
        Assert.Equal(HttpStatusCode.NoContent, deactivateAffiliate.StatusCode);
        using var fresh = await CreateAuthorizationAsync(client, Guid.NewGuid(), original);
        Assert.Equal(HttpStatusCode.NotFound, fresh.StatusCode);
        using var replay = await CreateAuthorizationAsync(client, key, original);
        Assert.Equal(HttpStatusCode.Created, replay.StatusCode);
        Assert.Equal(location, replay.Headers.Location);

        var retained = new AuthorizationServiceInput(ProviderId, serviceId, 100m, detail.Id);
        using var replace = await client.PutAsJsonAsync(location, new AuthorizationInput(affiliateId,
            [new AuthorizationServiceInput(ProviderId, serviceId, 10m), retained]));
        Assert.Equal(HttpStatusCode.NotFound, replace.StatusCode);
        var unchanged = Assert.Single(await ReadAuthorizationsAsync(app, key));
        Assert.Equal(100m, unchanged.TotalClaimedAmount);
        Assert.Equal(detail.Id, Assert.Single(unchanged.Details).Id);

        using var keep = await client.PutAsJsonAsync(location, new AuthorizationInput(affiliateId, [retained]));
        Assert.Equal(HttpStatusCode.NoContent, keep.StatusCode);
        var current = await client.GetFromJsonAsync<AuthorizationDto>(location);
        Assert.NotNull(current);
        Assert.Equal(100m, current.Summary.TotalClaimedAmount);
        Assert.Equal(detail.Id, Assert.Single(current.Details).Id);
    }

    [SqlServerBusinessFact]
    public async Task ConnectedSignalRClientReceivesOnlyEffectiveChanges()
    {
        var tokens = new DevelopmentJwtTestTokens();
        await using var app = await StartAsync(tokens, null, new InvalidDetailOnFirstInsert());
        var user = Guid.NewGuid();
        using var client = CreateClient(app, tokens, user);
        var hub = new UriBuilder(app.Urls.Single()) { Scheme = "ws", Path = AuthorizationHub.Route }.Uri;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        using var anonymous = new ClientWebSocket();
        anonymous.Options.CollectHttpResponseDetails = true;
        await Assert.ThrowsAsync<WebSocketException>(() => anonymous.ConnectAsync(hub, timeout.Token));
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.HttpStatusCode);
        using var forbidden = new ClientWebSocket();
        forbidden.Options.CollectHttpResponseDetails = true;
        forbidden.Options.SetRequestHeader("Authorization", $"Bearer {tokens.Create("Visitor", user.ToString())}");
        await Assert.ThrowsAsync<WebSocketException>(() => forbidden.ConnectAsync(hub, timeout.Token));
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.HttpStatusCode);

        using var socket = new ClientWebSocket();
        socket.Options.SetRequestHeader("Authorization", $"Bearer {tokens.Create("Operator", user.ToString())}");
        await socket.ConnectAsync(hub, timeout.Token);
        await socket.SendAsync(Encoding.UTF8.GetBytes("{\"protocol\":\"json\",\"version\":1}\u001e"),
            WebSocketMessageType.Text, true, timeout.Token);
        var frames = new Queue<JsonElement>();
        Assert.Empty((await ReadHubFrameAsync(socket, frames, timeout.Token)).EnumerateObject());

        var affiliateId = await CreateAffiliateAsync(client);
        var key = Guid.NewGuid();
        var input = new AuthorizationInput(affiliateId,
            [new AuthorizationServiceInput(ProviderId, ServiceId, 100m), new AuthorizationServiceInput(ProviderId, ServiceId, 50m)]);
        using var failed = await CreateAuthorizationAsync(client, key, input);
        Assert.Equal(HttpStatusCode.Conflict, failed.StatusCode);
        using var created = await CreateAuthorizationAsync(client, key, input);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var location = created.Headers.Location!;
        var id = Guid.Parse(location.Segments.Last());
        await AssertHubEventAsync(socket, frames, "AuthorizationCreated", id, null, timeout.Token);

        using var replay = await CreateAuthorizationAsync(client, key, input);
        Assert.Equal(location, replay.Headers.Location);
        var statusPath = location.AbsoluteUri + "/status";
        using var changed = await client.PatchAsJsonAsync(statusPath, new { isActive = false }, timeout.Token);
        Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);
        await AssertHubEventAsync(socket, frames, "AuthorizationStatusChanged", id, false, timeout.Token);
        using var unchanged = await client.PatchAsJsonAsync(statusPath, new { isActive = false }, timeout.Token);
        Assert.Equal(HttpStatusCode.NoContent, unchanged.StatusCode);
        using var next = await CreateAuthorizationAsync(client, Guid.NewGuid(), input);
        Assert.Equal(HttpStatusCode.Created, next.StatusCode);
        await AssertHubEventAsync(socket, frames, "AuthorizationCreated",
            Guid.Parse(next.Headers.Location!.Segments.Last()), null, timeout.Token);
    }

    private static async Task AssertHubEventAsync(ClientWebSocket socket, Queue<JsonElement> frames,
        string expectedEvent, Guid expectedId, bool? expectedStatus, CancellationToken cancellationToken)
    {
        JsonElement frame;
        do
        {
            frame = await ReadHubFrameAsync(socket, frames, cancellationToken);
        } while (frame.GetProperty("type").GetInt32() == 6);

        Assert.Equal(1, frame.GetProperty("type").GetInt32());
        Assert.Equal(expectedEvent, frame.GetProperty("target").GetString());
        var payload = Assert.Single(frame.GetProperty("arguments").EnumerateArray());
        Assert.Equal(expectedId, payload.GetProperty("authorizationId").GetGuid());
        Assert.True(payload.GetProperty("occurredAt").TryGetDateTimeOffset(out _));
        Assert.Equal(expectedStatus.HasValue ? 3 : 2, payload.EnumerateObject().Count());
        if (expectedStatus.HasValue)
        {
            Assert.Equal(expectedStatus.Value, payload.GetProperty("isActive").GetBoolean());
        }
    }

    private static async Task<JsonElement> ReadHubFrameAsync(
        ClientWebSocket socket, Queue<JsonElement> frames, CancellationToken cancellationToken)
    {
        while (frames.Count == 0)
        {
            using var message = new MemoryStream();
            var buffer = new byte[4096];
            WebSocketReceiveResult chunk;
            do
            {
                chunk = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                Assert.Equal(WebSocketMessageType.Text, chunk.MessageType);
                message.Write(buffer, 0, chunk.Count);
            } while (!chunk.EndOfMessage);

            foreach (var frame in Encoding.UTF8.GetString(message.ToArray()).Split('\u001e', StringSplitOptions.RemoveEmptyEntries))
            {
                using var document = JsonDocument.Parse(frame);
                frames.Enqueue(document.RootElement.Clone());
            }
        }
        return frames.Dequeue();
    }

    private static async Task<(HttpStatusCode Status, string Body)> CompleteRaceAsync(
        PausedWrite pause, CancellationToken cancellationToken,
        Func<Task<HttpResponseMessage>> sendPaused, Func<Task<HttpResponseMessage>> sendWinner)
    {
        var paused = ReadPausedResponseAsync();
        await Task.WhenAll(paused, RunWinnerAsync());
        return await paused;

        async Task<(HttpStatusCode Status, string Body)> ReadPausedResponseAsync()
        {
            using var response = await sendPaused();
            return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
        }

        async Task RunWinnerAsync()
        {
            try
            {
                await pause.Entered.Task.WaitAsync(cancellationToken);
                using var response = await sendWinner();
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            }
            finally
            {
                pause.Release.TrySetResult();
            }
        }
    }

    private static async Task<WebApplication> StartAsync(
        DevelopmentJwtTestTokens tokens, NotificationCounter? notifications, SaveChangesInterceptor? interceptor = null)
    {
        var configured = Environment.GetEnvironmentVariable(AuthorizationIdempotencySqlServerTests.ConnectionVariable);
        var connection = new SqlConnectionStringBuilder(configured!);
        Assert.Equal("HealthAuthorizationIdempotencyTests", connection.InitialCatalog);
        Assert.Empty(connection.AttachDBFilename);

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        tokens.Configure(builder);
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.SetMinimumLevel(LogLevel.Critical);
        builder.Services.AddApplication().AddInfrastructure(connection.ConnectionString);
        builder.Services.AddSignalR();
        if (notifications is null)
        {
            builder.Services.AddSingleton<IAuthorizationNotificationService, AuthorizationNotificationService>();
        }
        else
        {
            builder.Services.AddSingleton<IAuthorizationNotificationService>(notifications);
        }
        if (interceptor is not null)
        {
            builder.Services.ConfigureDbContext<HealthAuthorizationDbContext>(options => options.AddInterceptors(interceptor));
        }
        builder.Services.AddControllers().AddApplicationPart(typeof(AuthorizationsController).Assembly);
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<ApiExceptionHandler>();
        builder.AddApiSecurity();

        var app = builder.Build();
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HealthAuthorizationDbContext>();
            await context.Database.OpenConnectionAsync();
            await using var check = context.Database.GetDbConnection().CreateCommand();
            check.CommandText = "SELECT DB_NAME()";
            Assert.Equal("HealthAuthorizationIdempotencyTests", await check.ExecuteScalarAsync());
            Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        }
        app.UseExceptionHandler();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHub<AuthorizationHub>(AuthorizationHub.Route);
        await app.StartAsync();
        return app;
    }

    private static HttpClient CreateClient(WebApplication app, DevelopmentJwtTestTokens tokens, Guid user)
    {
        var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.Create("Administrator", user.ToString()));
        return client;
    }

    private static async Task<Guid> CreateAffiliateAsync(HttpClient client)
    {
        var input = new AffiliateInput(Guid.NewGuid().ToString("N"), "Ana", "Prueba", new DateOnly(1990, 1, 1));
        using var response = await client.PostAsJsonAsync("/api/affiliates", input);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Guid.Parse(response.Headers.Location!.Segments.Last());
    }

    private static async Task<HttpResponseMessage> CreateAuthorizationAsync(HttpClient client, Guid key, AuthorizationInput input)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/authorizations");
        request.Headers.Add("Idempotency-Key", key.ToString());
        request.Content = JsonContent.Create(input);
        return await client.SendAsync(request);
    }

    private static async Task<List<Authorization>> ReadAuthorizationsAsync(WebApplication app, Guid key)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<HealthAuthorizationDbContext>();
        return await context.Authorizations.IgnoreQueryFilters().AsNoTracking().Include(row => row.Details)
            .Where(row => row.IdempotencyKey == key).ToListAsync();
    }

    private static async Task<byte[]> ReadVersionAsync<TEntity>(WebApplication app, Guid id)
        where TEntity : HealthAuthorization.Domain.Common.Entities.BaseEntity
    {
        await using var scope = app.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<HealthAuthorizationDbContext>();
        return await context.Set<TEntity>().Where(row => row.Id == id)
            .Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync();
    }

    private class PausedWrite : SaveChangesInterceptor
    {
        private readonly Func<DbContext, bool> _matches;

        public PausedWrite(Func<DbContext, bool> matches)
        {
            _matches = matches;
        }

        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context is not null && _matches(eventData.Context) && Entered.TrySetResult())
            {
                await Release.Task.WaitAsync(cancellationToken);
            }
            return result;
        }
    }

    private class InvalidDetailOnFirstInsert : SaveChangesInterceptor
    {
        public Guid[] DetailIds { get; private set; } = [];
        public int? SqlErrorNumber { get; private set; }
        public string? SqlErrorMessage { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (DetailIds.Length == 0 && eventData.Context is not null)
            {
                var details = eventData.Context.ChangeTracker.Entries<AuthorizationDetail>()
                    .Where(entry => entry.State == EntityState.Added).Select(entry => entry.Entity).ToArray();
                if (details.Length == 2)
                {
                    DetailIds = details.Select(detail => detail.Id).ToArray();
                    details[1].AffiliateAmount = -1m;
                }
            }
            return ValueTask.FromResult(result);
        }

        public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            if (eventData.Exception.InnerException is SqlException error)
            {
                SqlErrorNumber = error.Number;
                SqlErrorMessage = error.Message;
            }
            return Task.CompletedTask;
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

public class SqlServerBusinessFactAttribute : FactAttribute
{
    public SqlServerBusinessFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(AuthorizationIdempotencySqlServerTests.ConnectionVariable)))
        {
            Skip = "Configure la base independiente de pruebas de SQL Server.";
        }
    }
}

public class SqlServerBusinessTheoryAttribute : TheoryAttribute
{
    public SqlServerBusinessTheoryAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(AuthorizationIdempotencySqlServerTests.ConnectionVariable)))
        {
            Skip = "Configure la base independiente de pruebas de SQL Server.";
        }
    }
}
