using HealthAuthorization.API.Hubs;
using HealthAuthorization.API.Notifications;
using HealthAuthorization.Application;
using HealthAuthorization.Application.Features.Authorizations.Notifications;
using HealthAuthorization.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("HealthAuthorization");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Configure la cadena de conexión 'HealthAuthorization'.");
}

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddSignalR();
builder.Services.AddSingleton<IAuthorizationNotificationService, AuthorizationNotificationService>();

var app = builder.Build();

app.MapHub<AuthorizationHub>("/hubs/authorizations");

app.Run();
