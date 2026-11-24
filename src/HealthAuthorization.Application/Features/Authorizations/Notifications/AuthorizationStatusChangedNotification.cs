namespace HealthAuthorization.Application.Features.Authorizations.Notifications;

public class AuthorizationStatusChangedNotification
{
    public Guid AuthorizationId { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
