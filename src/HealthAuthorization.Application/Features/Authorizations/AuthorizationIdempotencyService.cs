using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;

namespace HealthAuthorization.Application.Features.Authorizations;

public class AuthorizationIdempotencyService
{
    private readonly IRepositoryAsync<Authorization> _authorizations;
    private readonly ICurrentUser _currentUser;

    public AuthorizationIdempotencyService(IRepositoryAsync<Authorization> authorizations, ICurrentUser currentUser)
    {
        _authorizations = authorizations;
        _currentUser = currentUser;
    }

    internal Authorization Prepare(AuthorizationInput input, Guid key)
    {
        var userId = _currentUser.UserId;
        if (userId is null || userId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("La creación de autorizaciones requiere un identificador de usuario válido.");
        }

        return new Authorization
        {
            IdempotencyScopeId = userId,
            IdempotencyKey = key,
            IdempotencyRequestHash = AuthorizationRequestHash.Calculate(input)
        };
    }

    internal async Task<Guid?> FindReplayAsync(Authorization candidate, CancellationToken cancellationToken)
    {
        var existing = await FindExistingAsync(candidate.IdempotencyScopeId, candidate.IdempotencyKey, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        return ResolveExisting(existing, candidate.IdempotencyRequestHash!);
    }

    private Task<ExistingAuthorization?> FindExistingAsync(Guid? scopeId, Guid? key, CancellationToken cancellationToken)
    {
        return _authorizations.FindAsync(
            authorization => authorization.IdempotencyScopeId == scopeId && authorization.IdempotencyKey == key,
            authorization => new ExistingAuthorization
            {
                Id = authorization.Id,
                RequestHash = authorization.IdempotencyRequestHash!
            },
            cancellationToken);
    }

    private static Guid ResolveExisting(ExistingAuthorization existing, byte[] requestHash)
    {
        if (!existing.RequestHash.SequenceEqual(requestHash))
        {
            throw new ConflictException("La clave de idempotencia ya se utilizó con datos diferentes.");
        }

        return existing.Id;
    }

    private class ExistingAuthorization
    {
        public Guid Id { get; set; }
        public byte[] RequestHash { get; set; } = null!;
    }
}
