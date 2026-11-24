using System.Security.Claims;
using HealthAuthorization.Application.Abstractions;

namespace HealthAuthorization.API.Security;

internal class HttpCurrentUser : ICurrentUser
{
    internal const string UserIdClaim = "healthauthorization_user_id";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var principal = _httpContextAccessor.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            return GetUserId(principal)
                ?? throw new UnauthorizedAccessException("La identidad autenticada no contiene un identificador válido.");
        }
    }

    internal static Guid? GetUserId(ClaimsPrincipal? principal)
    {
        var ids = principal?.FindAll(UserIdClaim).Take(2).ToArray();
        if (ids?.Length == 1 && Guid.TryParse(ids[0].Value, out var id) && id != Guid.Empty)
        {
            return id;
        }

        return null;
    }
}
