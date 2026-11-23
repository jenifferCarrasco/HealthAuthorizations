using HealthAuthorization.Domain.Entities;

namespace HealthAuthorization.Application.Features.Authorizations;

public interface IAuthorizationReactivationPolicy
{
    bool CanReactivate(Authorization authorization);
}

internal class AuthorizationReactivationDisabled : IAuthorizationReactivationPolicy
{
    public bool CanReactivate(Authorization authorization) => false;
}
