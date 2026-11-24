using HealthAuthorization.API.Hubs;
using HealthAuthorization.API.Notifications;
using HealthAuthorization.Application.Features.Authorizations.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace HealthAuthorization.IntegrationTests.Api;

public class AuthorizationNotificationServiceTests
{
    [Theory]
    [InlineData(false, "AuthorizationCreated")]
    [InlineData(true, "AuthorizationStatusChanged")]
    public async Task SendsTheExpectedEventAndPayloadToAllClients(bool changeStatus, string expectedEvent)
    {
        var client = new ClientProxyStub();
        var service = new AuthorizationNotificationService(new HubContextStub(client));
        using var cancellation = new CancellationTokenSource();
        var occurredAt = new DateTimeOffset(2026, 11, 23, 12, 0, 0, TimeSpan.Zero);
        var authorizationId = Guid.NewGuid();
        object notification;

        if (changeStatus)
        {
            var status = new AuthorizationStatusChangedNotification
            {
                AuthorizationId = authorizationId, OccurredAt = occurredAt, IsActive = false
            };
            notification = status;
            await service.NotifyStatusChangedAsync(status, cancellation.Token);
        }
        else
        {
            var created = new AuthorizationCreatedNotification
            {
                AuthorizationId = authorizationId, OccurredAt = occurredAt
            };
            notification = created;
            await service.NotifyCreatedAsync(created, cancellation.Token);
        }

        Assert.Equal(expectedEvent, client.Method);
        Assert.Same(notification, Assert.Single(client.Arguments));
        Assert.Equal(cancellation.Token, client.CancellationToken);
    }

    private class ClientProxyStub : IClientProxy
    {
        public string? Method { get; private set; }
        public object?[] Arguments { get; private set; } = Array.Empty<object?>();
        public CancellationToken CancellationToken { get; private set; }

        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            Method = method;
            Arguments = args;
            CancellationToken = cancellationToken;
            return Task.CompletedTask;
        }
    }

    private class HubContextStub : IHubContext<AuthorizationHub>
    {
        public HubContextStub(IClientProxy client)
        {
            Clients = new HubClientsStub(client);
        }

        public IHubClients Clients { get; }
        public IGroupManager Groups => throw new NotSupportedException();
    }

    private class HubClientsStub : IHubClients
    {
        public HubClientsStub(IClientProxy client)
        {
            All = client;
        }

        public IClientProxy All { get; }
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
        public IClientProxy Client(string connectionId) => throw new NotSupportedException();
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => throw new NotSupportedException();
        public IClientProxy Group(string groupName) => throw new NotSupportedException();
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) =>
            throw new NotSupportedException();
        public IClientProxy Groups(IReadOnlyList<string> groupNames) => throw new NotSupportedException();
        public IClientProxy User(string userId) => throw new NotSupportedException();
        public IClientProxy Users(IReadOnlyList<string> userIds) => throw new NotSupportedException();
    }
}
