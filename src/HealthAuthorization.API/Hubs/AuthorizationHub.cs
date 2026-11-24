using HealthAuthorization.API.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HealthAuthorization.API.Hubs;

[Authorize(Policy = SecurityConfiguration.BusinessAccess)]
public class AuthorizationHub : Hub
{
    public const string Route = "/hubs/authorizations";
}
