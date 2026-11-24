namespace HealthAuthorization.Application.Features.Authorizations.Notifications;

public interface IAuthorizationNotificationService
{
    Task NotifyCreatedAsync(AuthorizationCreatedNotification notification, CancellationToken cancellationToken);
    Task NotifyStatusChangedAsync(AuthorizationStatusChangedNotification notification, CancellationToken cancellationToken);
}
