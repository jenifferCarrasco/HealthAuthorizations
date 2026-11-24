using HealthAuthorization.API.Controllers;
using HealthAuthorization.API.Errors;
using HealthAuthorization.API.Notifications;
using HealthAuthorization.API.Security;
using HealthAuthorization.Application;
using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Features.Authorizations.Notifications;
using HealthAuthorization.Domain.Entities;
using HealthAuthorization.UnitTests.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HealthAuthorization.IntegrationTests.Api;

internal static class BusinessApiTestHost
{
    internal static async Task<WebApplication> StartAsync(
        DevelopmentJwtTestTokens tokens, UnitOfWorkStub unit, ProviderType providerType,
        MemoryRepository<Authorization>? authorizations = null, Action<IServiceCollection>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        tokens.Configure(builder);
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.SetMinimumLevel(LogLevel.Critical);
        builder.Services.AddApplication();
        builder.Services.AddSingleton<IRepositoryAsync<Affiliate>>(new MemoryRepository<Affiliate>());
        builder.Services.AddSingleton<IRepositoryAsync<Contractor>>(new MemoryRepository<Contractor>());
        builder.Services.AddSingleton<IRepositoryAsync<Intermediary>>(new MemoryRepository<Intermediary>());
        builder.Services.AddSingleton<IRepositoryAsync<Provider>>(new MemoryRepository<Provider>());
        builder.Services.AddSingleton<IRepositoryAsync<ProviderType>>(new MemoryRepository<ProviderType>(providerType));
        builder.Services.AddSingleton<IRepositoryAsync<ServiceType>>(new MemoryRepository<ServiceType>());
        builder.Services.AddSingleton<IRepositoryAsync<Authorization>>(authorizations ?? new MemoryRepository<Authorization>());
        builder.Services.AddSingleton<IUnitOfWork>(unit);
        builder.Services.AddSignalR();
        builder.Services.AddSingleton<IAuthorizationNotificationService, AuthorizationNotificationService>();
        configure?.Invoke(builder.Services);
        builder.Services.AddControllers().AddApplicationPart(typeof(ProvidersController).Assembly);
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<ApiExceptionHandler>();
        builder.AddApiSecurity();

        var app = builder.Build();
        app.UseExceptionHandler();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        await app.StartAsync();
        return app;
    }
}
