using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Features.Authorizations.Notifications;
using HealthAuthorization.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HealthAuthorization.Application.Features.Authorizations.Commands;

public class CreateAuthorizationCommand : IRequest<Guid>
{
    public AuthorizationInput Input { get; set; }

    public CreateAuthorizationCommand(AuthorizationInput Input)
    {
        this.Input = Input;
    }
}

public class CreateAuthorizationCommandHandler : IRequestHandler<CreateAuthorizationCommand, Guid>
{
    private readonly IRepositoryAsync<Authorization> _authorizations;
    private readonly AuthorizationWriter _writer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthorizationNotificationService _notifications;
    private readonly TimeProvider _clock;
    private readonly ILogger<CreateAuthorizationCommandHandler> _logger;

    public CreateAuthorizationCommandHandler(IRepositoryAsync<Authorization> authorizations, AuthorizationWriter writer,
        IUnitOfWork unitOfWork, IAuthorizationNotificationService notifications, TimeProvider clock,
        ILogger<CreateAuthorizationCommandHandler> logger)
    {
        _authorizations = authorizations;
        _writer = writer;
        _unitOfWork = unitOfWork;
        _notifications = notifications;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateAuthorizationCommand request, CancellationToken cancellationToken)
    {
        var authorization = new Authorization();
        await _writer.ApplyAsync(authorization, request.Input, cancellationToken);
        _authorizations.Add(authorization);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var notification = new AuthorizationCreatedNotification
            {
                AuthorizationId = authorization.Id,
                OccurredAt = _clock.GetUtcNow()
            };
            await _notifications.NotifyCreatedAsync(notification, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "No se pudo notificar la creación de la autorización {AuthorizationId}.",
                authorization.Id);
        }

        return authorization.Id;
    }
}
