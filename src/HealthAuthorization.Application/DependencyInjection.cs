using FluentValidation;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Application.Features.Authorizations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HealthAuthorization.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<AuthorizationWriter>();
        services.TryAddScoped<IAuthorizationReactivationPolicy, AuthorizationReactivationDisabled>();
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        return services;
    }
}
