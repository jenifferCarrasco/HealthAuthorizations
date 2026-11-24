using HealthAuthorization.Application.Common;

namespace HealthAuthorization.Application.Features.Authorizations;

public class AuthorizationIdempotencyConflictException : ConflictException
{
    public AuthorizationIdempotencyConflictException(Exception innerException)
        : base("Ya existe una autorización con la misma clave de idempotencia.", innerException)
    {
    }
}
