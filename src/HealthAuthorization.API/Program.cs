using HealthAuthorization.API.Errors;
using HealthAuthorization.API.Hubs;
using HealthAuthorization.API.Notifications;
using HealthAuthorization.API.Security;
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
builder.AddApiSecurity();
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddApiOpenApi();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IAuthorizationNotificationService, AuthorizationNotificationService>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("../openapi/v1.json", "HealthAuthorization API v1");
    });
}

app.MapControllers();
app.MapHub<AuthorizationHub>(AuthorizationHub.Route, options =>
{
    options.CloseOnAuthenticationExpiration = true;
});

app.Run();
