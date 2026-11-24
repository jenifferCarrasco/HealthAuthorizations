namespace HealthAuthorization.Application.Features.Authorizations.Notifications;

public class AuthorizationCreatedNotification
{
    public Guid AuthorizationId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
