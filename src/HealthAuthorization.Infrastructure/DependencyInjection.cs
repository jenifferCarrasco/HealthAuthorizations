using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Infrastructure.Persistence;
using HealthAuthorization.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HealthAuthorization.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<ICurrentUser, AnonymousCurrentUser>();
        services.AddDbContext<HealthAuthorizationDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepositoryAsync<>), typeof(EfRepository<>));
        return services;
    }

    private class AnonymousCurrentUser : ICurrentUser
    {
        public Guid? UserId => null;
    }
}
