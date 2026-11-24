using HealthAuthorization.API.Hubs;
using HealthAuthorization.Application.Features.Authorizations.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace HealthAuthorization.API.Notifications;

public class AuthorizationNotificationService : IAuthorizationNotificationService
{
    private const string AuthorizationCreatedEvent = "AuthorizationCreated";
    private const string AuthorizationStatusChangedEvent = "AuthorizationStatusChanged";

    private readonly IHubContext<AuthorizationHub> _hubContext;

    public AuthorizationNotificationService(IHubContext<AuthorizationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyCreatedAsync(AuthorizationCreatedNotification notification, CancellationToken cancellationToken)
    {
        return _hubContext.Clients.All.SendAsync(AuthorizationCreatedEvent, notification, cancellationToken);
    }

    public Task NotifyStatusChangedAsync(AuthorizationStatusChangedNotification notification, CancellationToken cancellationToken)
    {
        return _hubContext.Clients.All.SendAsync(AuthorizationStatusChangedEvent, notification, cancellationToken);
    }
}
