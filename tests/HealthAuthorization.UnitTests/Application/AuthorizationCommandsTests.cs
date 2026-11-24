using FluentValidation;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Application.Features.Authorizations;
using HealthAuthorization.Application.Features.Authorizations.Commands;
using HealthAuthorization.Application.Features.Authorizations.Notifications;
using HealthAuthorization.Application.Features.Authorizations.Validators;
using HealthAuthorization.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace HealthAuthorization.UnitTests.Application;

public sealed class AuthorizationCommandsTests
{
    private readonly Affiliate _affiliate = new();
    private readonly Provider _provider = new();
    private readonly ServiceType _serviceType = new();
    private readonly UnitOfWorkStub _unit = new();
    private readonly NotificationServiceStub _notifications = new();
    private readonly FixedClock _clock = new();

    [Fact]
    public async Task ReplayPreservesTheOriginalHashAfterUpdateAndDoesNotNotifyAgain()
    {
        var repository = new MemoryRepository<Authorization>();
        var create = CreateHandler(repository);
        var original = new CreateAuthorizationCommand(Input(Service(100m)), Guid.NewGuid());
        var id = await create.Handle(original, CancellationToken.None);
        var stored = Assert.Single(repository.Items);
        var hash = Assert.IsType<byte[]>(stored.IdempotencyRequestHash).ToArray();

        var update = new UpdateAuthorizationCommandHandler(repository, CreateWriter(), _unit);
        await update.Handle(new UpdateAuthorizationCommand(id, Input(Service(200m, stored.Details.Single().Id))),
            CancellationToken.None);

        Assert.Equal(id, await create.Handle(original, CancellationToken.None));
        Assert.Equal(hash, stored.IdempotencyRequestHash);
        Assert.Equal(200m, stored.TotalClaimedAmount);
        Assert.Equal(2, _unit.SaveCount);
        Assert.Single(_notifications.Created);
    }

    [Fact]
    public async Task ReplayNormalizesOrderAndDecimalScaleAndPreservesRepeatedServices()
    {
        var repository = new MemoryRepository<Authorization>();
        var handler = CreateHandler(repository);
        var key = Guid.NewGuid();
        var first = new CreateAuthorizationCommand(Input(Service(100m), Service(25m), Service(25m)), key);
        var reordered = new CreateAuthorizationCommand(Input(Service(25.00m), Service(100.00m), Service(25m)), key);
        var id = await handler.Handle(first, CancellationToken.None);

        Assert.Equal(id, await handler.Handle(reordered, CancellationToken.None));
        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new CreateAuthorizationCommand(Input(Service(25m), Service(100m)), key), CancellationToken.None));
        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new CreateAuthorizationCommand(Input(Service(25m), Service(100.01m), Service(25m)), key),
                CancellationToken.None));
        Assert.Single(repository.Items);
        Assert.Equal(1, _unit.SaveCount);
        Assert.Single(_notifications.Created);
    }

    [Fact]
    public async Task DifferentKeysAndScopesCreateIndependentAuthorizations()
    {
        var repository = new MemoryRepository<Authorization>();
        var local = CreateHandler(repository);
        var authenticated = CreateHandler(repository, Guid.NewGuid());
        var command = new CreateAuthorizationCommand(Input(Service(100m)), Guid.NewGuid());
        var original = await local.Handle(command, CancellationToken.None);
        var otherKey = await local.Handle(new CreateAuthorizationCommand(command.Input, Guid.NewGuid()), CancellationToken.None);
        var otherScope = await authenticated.Handle(command, CancellationToken.None);

        Assert.Equal(3, new HashSet<Guid> { original, otherKey, otherScope }.Count);
        Assert.Equal(otherScope, await authenticated.Handle(command, CancellationToken.None));
        Assert.Equal(original, await local.Handle(command, CancellationToken.None));
        Assert.Equal(3, _unit.SaveCount);
        Assert.Equal(3, _notifications.Created.Count);
    }

    [Fact]
    public async Task FailedValidationBeforeSavingLeavesTheKeyAvailable()
    {
        var repository = new MemoryRepository<Authorization>();
        var handler = CreateHandler(repository);
        var key = Guid.NewGuid();
        var invalid = new CreateAuthorizationCommand(Input(new AuthorizationServiceInput(
            Guid.NewGuid(), _serviceType.Id, 100m)), key);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(invalid, CancellationToken.None));
        Assert.Empty(repository.Items);
        Assert.Equal(0, _unit.SaveCount);
        Assert.Empty(_notifications.Created);

        await handler.Handle(new CreateAuthorizationCommand(Input(Service(100m)), key), CancellationToken.None);
        Assert.Single(repository.Items);
        Assert.Equal(1, _unit.SaveCount);
        Assert.Single(_notifications.Created);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task CreatesAuthorizationWithConsistentTotalsInOneSave(int count)
    {
        var repository = new MemoryRepository<Authorization>();
        var handler = new CreateAuthorizationCommandHandler(repository, CreateWriter(), _unit, _notifications, _clock,
            NullLogger<CreateAuthorizationCommandHandler>.Instance, new AuthorizationIdempotencyService(repository, new CurrentUserStub()));
        var command = new CreateAuthorizationCommand(Input(Enumerable.Repeat(Service(100m), count).ToArray()), Guid.NewGuid());

        Assert.True(new CreateAuthorizationCommandValidator().Validate(command).IsValid);
        _notifications.BeforePublish = () => Assert.Equal(1, _unit.SaveCount);
        var id = await handler.Handle(command, CancellationToken.None);

        var authorization = Assert.Single(repository.Items);
        Assert.Equal(id, authorization.Id);
        Assert.Equal(count, authorization.Details.Count);
        Assert.Equal(100m * count, authorization.TotalClaimedAmount);
        Assert.Equal(70m * count, authorization.TotalArsAmount);
        Assert.Equal(30m * count, authorization.TotalAffiliateAmount);
        Assert.All(authorization.Details, item => Assert.Equal(id, item.AuthorizationId));
        Assert.Equal(1, _unit.SaveCount);
        var notification = Assert.Single(_notifications.Created);
        Assert.Equal(id, notification.AuthorizationId);
        Assert.Equal(_clock.GetUtcNow(), notification.OccurredAt);
        Assert.Equal(TimeSpan.Zero, notification.OccurredAt.Offset);
    }

    [Fact]
    public void RejectsCreationWithoutServicesAndRemovalOfTheLastService()
    {
        var input = Input();
        Assert.False(new CreateAuthorizationCommandValidator().Validate(new CreateAuthorizationCommand(input, Guid.NewGuid())).IsValid);
        Assert.False(new UpdateAuthorizationCommandValidator().Validate(new UpdateAuthorizationCommand(Guid.NewGuid(), input)).IsValid);
    }

    [Fact]
    public async Task DirectHandlerCallsCannotSaveAnAuthorizationWithoutServices()
    {
        var detail = new AuthorizationDetail { ClaimedAmount = 100m, ArsAmount = 70m, AffiliateAmount = 30m };
        var existing = new Authorization { Details = { detail } };
        var repository = new MemoryRepository<Authorization>(existing);
        var writer = CreateWriter();
        var create = new CreateAuthorizationCommandHandler(repository, writer, _unit, _notifications, _clock,
            NullLogger<CreateAuthorizationCommandHandler>.Instance, new AuthorizationIdempotencyService(repository, new CurrentUserStub()));
        var update = new UpdateAuthorizationCommandHandler(repository, writer, _unit);

        await Assert.ThrowsAsync<ValidationException>(() =>
            create.Handle(new CreateAuthorizationCommand(Input(), Guid.NewGuid()), CancellationToken.None));
        await Assert.ThrowsAsync<ValidationException>(() =>
            update.Handle(new UpdateAuthorizationCommand(existing.Id, Input()), CancellationToken.None));

        Assert.Same(existing, Assert.Single(repository.Items));
        Assert.Same(detail, Assert.Single(existing.Details));
        Assert.False(detail.IsDelete);
        Assert.Equal(0, repository.Updates);
        Assert.Equal(0, _unit.SaveCount);
    }

    [Theory]
    [InlineData(-10)]
    [InlineData(1001)]
    public void RejectsInvalidAmounts(int thousandths)
    {
        var command = new CreateAuthorizationCommand(Input(Service(thousandths / 1000m)), Guid.NewGuid());
        Assert.Contains(new CreateAuthorizationCommandValidator().Validate(command).Errors,
            error => error.PropertyName.Contains(nameof(AuthorizationServiceInput.ClaimedAmount)));
    }

    [Fact]
    public async Task RejectsUpdateWhenInactive()
    {
        var existing = new Authorization { IsActive = false };
        var repository = new MemoryRepository<Authorization>(existing);
        var handler = new UpdateAuthorizationCommandHandler(repository, CreateWriter(), _unit);

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new UpdateAuthorizationCommand(existing.Id, Input(Service(100m))), CancellationToken.None));
        Assert.Equal(0, _unit.SaveCount);
        Assert.Equal(0, repository.Updates);
    }

    [Fact]
    public async Task DeactivationPreservesDetailsAndCanBeRepeated()
    {
        var detail = new AuthorizationDetail { ClaimedAmount = 100m, ArsAmount = 70m, AffiliateAmount = 30m };
        var existing = new Authorization { AffiliateId = _affiliate.Id, Details = { detail } };
        var repository = new MemoryRepository<Authorization>(existing);
        var handler = CreateStatusHandler(repository, false);
        var command = new ChangeAuthorizationStatusCommand(existing.Id, false);
        _notifications.BeforePublish = () => Assert.Equal(1, _unit.SaveCount);

        await handler.Handle(command, CancellationToken.None);
        await handler.Handle(command, CancellationToken.None);

        Assert.False(existing.IsActive);
        Assert.Equal(_affiliate.Id, existing.AffiliateId);
        Assert.Same(detail, Assert.Single(existing.Details));
        Assert.False(detail.IsDelete);
        Assert.Equal(100m, existing.TotalClaimedAmount);
        Assert.Equal(1, _unit.SaveCount);
        var notification = Assert.Single(_notifications.StatusChanged);
        Assert.Equal(existing.Id, notification.AuthorizationId);
        Assert.False(notification.IsActive);
        Assert.Equal(_clock.GetUtcNow(), notification.OccurredAt);
    }

    [Fact]
    public async Task InvalidUpdatesLeaveExistingDetailsAndParticipantsUnchanged()
    {
        var detail = new AuthorizationDetail { ClaimedAmount = 100m, ArsAmount = 70m, AffiliateAmount = 30m };
        var existing = new Authorization { Details = { detail } };
        var repository = new MemoryRepository<Authorization>(existing);
        var handler = new UpdateAuthorizationCommandHandler(repository, CreateWriter(), _unit);
        var input = Input(Service(150m, detail.Id), Service(100m, Guid.NewGuid()));

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new UpdateAuthorizationCommand(existing.Id, input), CancellationToken.None));

        Assert.Same(detail, Assert.Single(existing.Details));
        Assert.Equal(Guid.Empty, existing.AffiliateId);
        Assert.Equal(100m, existing.TotalClaimedAmount);
        Assert.False(detail.IsDelete);
        Assert.Equal(0, _unit.SaveCount);
        Assert.Equal(0, repository.Updates);
    }

    [Fact]
    public async Task UpdateRetainsExistingIdsAndSoftDeletesOmittedDetails()
    {
        var retained = new AuthorizationDetail { ClaimedAmount = 100m };
        var omitted = new AuthorizationDetail { ClaimedAmount = 30m };
        var existing = new Authorization { Details = { retained, omitted } };
        var repository = new MemoryRepository<Authorization>(existing);
        var handler = new UpdateAuthorizationCommandHandler(repository, CreateWriter(), _unit);

        await handler.Handle(new UpdateAuthorizationCommand(existing.Id,
            Input(Service(0.05m, retained.Id), Service(10m))), CancellationToken.None);

        Assert.True(omitted.IsDelete);
        Assert.Same(retained, existing.Details.Single(detail => detail.Id == retained.Id));
        Assert.Equal(10.05m, existing.TotalClaimedAmount);
        Assert.Equal(7.04m, existing.TotalArsAmount);
        Assert.Equal(3.01m, existing.TotalAffiliateAmount);
        Assert.Equal(1, _unit.SaveCount);
    }

    [Fact]
    public async Task UpdateCanRetainADeletedCatalogReference()
    {
        _provider.IsDelete = true;
        var detail = new AuthorizationDetail { ProviderId = _provider.Id, ServiceTypeId = _serviceType.Id };
        var existing = new Authorization { AffiliateId = _affiliate.Id, Details = { detail } };
        var repository = new MemoryRepository<Authorization>(existing);
        var handler = new UpdateAuthorizationCommandHandler(repository, CreateWriter(), _unit);

        await handler.Handle(new UpdateAuthorizationCommand(existing.Id, Input(Service(25m, detail.Id))),
            CancellationToken.None);

        Assert.Equal(_provider.Id, detail.ProviderId);
        Assert.Equal(25m, existing.TotalClaimedAmount);
        Assert.Equal(1, _unit.SaveCount);
    }

    [Fact]
    public async Task RejectsUnknownProviderWithoutAddingOrSaving()
    {
        var repository = new MemoryRepository<Authorization>();
        var handler = new CreateAuthorizationCommandHandler(repository, CreateWriter(), _unit, _notifications, _clock,
            NullLogger<CreateAuthorizationCommandHandler>.Instance, new AuthorizationIdempotencyService(repository, new CurrentUserStub()));
        var input = Input(new AuthorizationServiceInput(Guid.NewGuid(), _serviceType.Id, 100m));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new CreateAuthorizationCommand(input, Guid.NewGuid()), CancellationToken.None));

        Assert.Empty(repository.Items);
        Assert.Equal(0, _unit.SaveCount);
        Assert.Empty(_notifications.Created);
    }

    private AuthorizationInput Input(params AuthorizationServiceInput[] services) => new(_affiliate.Id, services);
    private AuthorizationServiceInput Service(decimal amount, Guid? detailId = null) =>
        new(_provider.Id, _serviceType.Id, amount, detailId);

    private AuthorizationWriter CreateWriter() =>
        new(new MemoryRepository<Affiliate>(_affiliate), new MemoryRepository<Contractor>(),
            new MemoryRepository<Intermediary>(), new MemoryRepository<Provider>(_provider),
            new MemoryRepository<ServiceType>(_serviceType));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReactivationUsesBusinessPolicyAndPreservesDetails(bool allowed)
    {
        var authorization = new Authorization { IsActive = false };
        authorization.Details.Add(new AuthorizationDetail { ClaimedAmount = 100m });
        var repository = new MemoryRepository<Authorization>(authorization);
        var handler = CreateStatusHandler(repository, allowed);
        var command = new ChangeAuthorizationStatusCommand(authorization.Id, true);

        if (allowed)
        {
            await handler.Handle(command, CancellationToken.None);
        }
        else
        {
            await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(command, CancellationToken.None));
        }

        Assert.Equal(allowed, authorization.IsActive);
        Assert.Equal(100m, authorization.TotalClaimedAmount);
        Assert.Equal(allowed ? 1 : 0, _unit.SaveCount);
        Assert.Equal(allowed ? 1 : 0, _notifications.StatusChanged.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedSaveDoesNotPublishNotifications(bool changeStatus)
    {
        var failure = new InvalidOperationException("Falló el guardado.");
        _unit.SaveError = failure;
        var existing = new Authorization();
        var repository = new MemoryRepository<Authorization>(existing);

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => ExecuteOperationAsync(changeStatus, repository, existing));

        Assert.Same(failure, thrown);
        Assert.Empty(_notifications.Created);
        Assert.Empty(_notifications.StatusChanged);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedNotificationPreservesSuccessfulSave(bool changeStatus)
    {
        var existing = new Authorization();
        var repository = new MemoryRepository<Authorization>(existing);
        _notifications.BeforePublish = () => Assert.Equal(1, _unit.SaveCount);
        _notifications.SendError = new InvalidOperationException("Falló el envío.");

        var id = await ExecuteOperationAsync(changeStatus, repository, existing);

        Assert.Equal(1, _unit.SaveCount);
        Assert.Contains(repository.Items, item => item.Id == id);
        if (changeStatus)
        {
            Assert.False(existing.IsActive);
            Assert.Equal(id, Assert.Single(_notifications.StatusChanged).AuthorizationId);
        }
        else
        {
            Assert.Equal(id, Assert.Single(_notifications.Created).AuthorizationId);
        }
    }

    private async Task<Guid> ExecuteOperationAsync(bool changeStatus, MemoryRepository<Authorization> repository,
        Authorization existing)
    {
        if (changeStatus)
        {
            await CreateStatusHandler(repository, false)
                .Handle(new ChangeAuthorizationStatusCommand(existing.Id, false), CancellationToken.None);
            return existing.Id;
        }

        var handler = new CreateAuthorizationCommandHandler(repository, CreateWriter(), _unit, _notifications, _clock,
            NullLogger<CreateAuthorizationCommandHandler>.Instance, new AuthorizationIdempotencyService(repository, new CurrentUserStub()));
        return await handler.Handle(new CreateAuthorizationCommand(Input(Service(100m)), Guid.NewGuid()), CancellationToken.None);
    }

    private ChangeAuthorizationStatusCommandHandler CreateStatusHandler(MemoryRepository<Authorization> repository,
        bool canReactivate)
    {
        return new ChangeAuthorizationStatusCommandHandler(repository, _unit, new TestReactivationPolicy(canReactivate),
            _notifications, _clock, NullLogger<ChangeAuthorizationStatusCommandHandler>.Instance);
    }

    private CreateAuthorizationCommandHandler CreateHandler(MemoryRepository<Authorization> repository, Guid? userId = null)
    {
        return new CreateAuthorizationCommandHandler(repository, CreateWriter(), _unit, _notifications, _clock,
            NullLogger<CreateAuthorizationCommandHandler>.Instance, new AuthorizationIdempotencyService(repository, new CurrentUserStub { UserId = userId }));
    }

    private class CurrentUserStub : HealthAuthorization.Application.Abstractions.ICurrentUser
    {
        public Guid? UserId { get; set; }
    }

    private class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return new DateTimeOffset(2026, 11, 23, 12, 0, 0, TimeSpan.Zero);
        }
    }

    private class NotificationServiceStub : IAuthorizationNotificationService
    {
        public List<AuthorizationCreatedNotification> Created { get; } = new List<AuthorizationCreatedNotification>();
        public List<AuthorizationStatusChangedNotification> StatusChanged { get; } = new List<AuthorizationStatusChangedNotification>();
        public Action? BeforePublish { get; set; }
        public Exception? SendError { get; set; }

        public Task NotifyCreatedAsync(AuthorizationCreatedNotification notification, CancellationToken cancellationToken)
        {
            BeforePublish?.Invoke();
            Created.Add(notification);
            return SendError == null ? Task.CompletedTask : Task.FromException(SendError);
        }

        public Task NotifyStatusChangedAsync(AuthorizationStatusChangedNotification notification,
            CancellationToken cancellationToken)
        {
            BeforePublish?.Invoke();
            StatusChanged.Add(notification);
            return SendError == null ? Task.CompletedTask : Task.FromException(SendError);
        }
    }

    private sealed class TestReactivationPolicy : IAuthorizationReactivationPolicy
    {
        private readonly bool _allowed;

        public TestReactivationPolicy(bool allowed) => _allowed = allowed;

        public bool CanReactivate(Authorization authorization) => _allowed;
    }
}
