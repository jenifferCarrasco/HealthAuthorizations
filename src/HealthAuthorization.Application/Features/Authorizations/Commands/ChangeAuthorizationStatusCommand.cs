using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Application.Features.Authorizations.Notifications;
using HealthAuthorization.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HealthAuthorization.Application.Features.Authorizations.Commands;

public class ChangeAuthorizationStatusCommand : IRequest
{
    public Guid Id { get; set; }
    public bool IsActive { get; set; }

    public ChangeAuthorizationStatusCommand(Guid Id, bool IsActive)
    {
        this.Id = Id;
        this.IsActive = IsActive;
    }
}

public class ChangeAuthorizationStatusCommandHandler : IRequestHandler<ChangeAuthorizationStatusCommand>
{
    private readonly IRepositoryAsync<Authorization> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthorizationReactivationPolicy _reactivationPolicy;
    private readonly IAuthorizationNotificationService _notifications;
    private readonly TimeProvider _clock;
    private readonly ILogger<ChangeAuthorizationStatusCommandHandler> _logger;

    public ChangeAuthorizationStatusCommandHandler(IRepositoryAsync<Authorization> repository, IUnitOfWork unitOfWork,
        IAuthorizationReactivationPolicy reactivationPolicy, IAuthorizationNotificationService notifications,
        TimeProvider clock, ILogger<ChangeAuthorizationStatusCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _reactivationPolicy = reactivationPolicy;
        _notifications = notifications;
        _clock = clock;
        _logger = logger;
    }

    public async Task Handle(ChangeAuthorizationStatusCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("La autorización no existe.");
        if (entity.IsActive == request.IsActive)
        {
            return;
        }

        if (request.IsActive && !_reactivationPolicy.CanReactivate(entity))
        {
            throw new ConflictException("La reactivación de la autorización no está permitida.");
        }

        entity.IsActive = request.IsActive;
        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var notification = new AuthorizationStatusChangedNotification
            {
                AuthorizationId = entity.Id,
                IsActive = entity.IsActive,
                OccurredAt = _clock.GetUtcNow()
            };
            await _notifications.NotifyStatusChangedAsync(notification, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "No se pudo notificar el cambio de estado de la autorización {AuthorizationId}.",
                entity.Id);
        }
    }
}
