using HealthAuthorization.Application;
using HealthAuthorization.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("HealthAuthorization");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Configure la cadena de conexión 'HealthAuthorization'.");
}

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);

var app = builder.Build();

app.Run();
