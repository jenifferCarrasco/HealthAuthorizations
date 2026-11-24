using HealthAuthorization.Application;
using HealthAuthorization.Application.Features.Contractors;
using HealthAuthorization.Application.Features.Intermediaries;
using HealthAuthorization.Application.Features.Providers;
using HealthAuthorization.Application.Features.ProviderTypes;
using HealthAuthorization.Application.Features.ServiceTypes;
using HealthAuthorization.Domain.Entities;
using HealthAuthorization.Infrastructure;
using HealthAuthorization.Infrastructure.Persistence;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthAuthorization.IntegrationTests.Persistence;

public class CatalogMappingTests
{
    [Fact]
    public void RegisteredMapperUsesTheExplicitConfiguration()
    {
        using var services = CreateServices();
        var configuration = services.GetRequiredService<TypeAdapterConfig>();
        var mapper = services.GetRequiredService<IMapper>();

        configuration.Compile();
        Assert.Same(configuration, mapper.Config);
        Assert.True(configuration.RequireExplicitMapping);
    }

    [Fact]
    public void CreatesCatalogEntitiesWithNormalizedEditableFields()
    {
        using var services = CreateServices();
        var mapper = services.GetRequiredService<IMapper>();
        var providerTypeId = Guid.NewGuid();

        var contractor = mapper.Map<Contractor>(new ContractorInput(" Empresa ", " RNC-1 "));
        var intermediary = mapper.Map<Intermediary>(new IntermediaryInput(" Corredor ", " ID-2 "));
        var provider = mapper.Map<Provider>(new ProviderInput(" Prestador ", " ID-3 ", providerTypeId));
        var providerType = mapper.Map<ProviderType>(new ProviderTypeInput(" Clínica ", "  "));
        var serviceType = mapper.Map<ServiceType>(new ServiceTypeInput(" Consulta ", " Primera cita "));

        Assert.Equal(("Empresa", "RNC-1"), (contractor.Name, contractor.Identification));
        Assert.Equal(("Corredor", "ID-2"), (intermediary.Name, intermediary.Identification));
        Assert.Equal(("Prestador", "ID-3", providerTypeId), (provider.Name, provider.Identification, provider.ProviderTypeId));
        Assert.Equal("Clínica", providerType.Name);
        Assert.Null(providerType.Description);
        Assert.Equal(("Consulta", "Primera cita"), (serviceType.Name, serviceType.Description));
        Assert.NotEqual(Guid.Empty, contractor.Id);
        Assert.True(contractor.IsActive);
        Assert.False(contractor.IsDelete);
    }

    [Fact]
    public void UpdatesTheTrackedEntityWithoutChangingAuditStatusOrNavigation()
    {
        using var services = CreateServices();
        using var scope = services.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        var context = scope.ServiceProvider.GetRequiredService<HealthAuthorizationDbContext>();
        var type = new ProviderType();
        var created = new DateTimeOffset(2026, 11, 23, 12, 0, 0, TimeSpan.Zero);
        var updated = created.AddHours(1);
        var creator = Guid.NewGuid();
        var updater = Guid.NewGuid();
        var existing = new Provider
        {
            Name = "Anterior", Identification = "ID-ANTERIOR", ProviderTypeId = type.Id, ProviderType = type,
            IsActive = false, IsDelete = true, CreatedDate = created, CreatedBy = creator,
            LastUpdatedDate = updated, LastUpdatedBy = updater
        };
        var id = existing.Id;
        var entry = context.Attach(existing);

        var mapped = mapper.Map(new ProviderInput(" Nuevo ", " ID-NUEVO ", type.Id), existing);
        context.ChangeTracker.DetectChanges();

        Assert.Same(existing, mapped);
        Assert.Equal(id, mapped.Id);
        Assert.Same(type, mapped.ProviderType);
        Assert.Equal(type.Id, mapped.ProviderTypeId);
        Assert.Equal(("Nuevo", "ID-NUEVO"), (mapped.Name, mapped.Identification));
        Assert.Equal((created, (Guid?)creator, (DateTimeOffset?)updated, (Guid?)updater),
            (mapped.CreatedDate, mapped.CreatedBy, mapped.LastUpdatedDate, mapped.LastUpdatedBy));
        Assert.False(mapped.IsActive);
        Assert.True(mapped.IsDelete);
        Assert.Equal(EntityState.Modified, entry.State);
        Assert.True(entry.Property(nameof(Provider.IsDelete)).Metadata.IsConcurrencyToken);
        Assert.Equal(new[] { nameof(Provider.Identification), nameof(Provider.Name) },
            entry.Properties.Where(property => property.IsModified).Select(property => property.Metadata.Name).Order().ToArray());
    }

    [Fact]
    public void AnEmptyDescriptionClearsTheExistingValue()
    {
        using var services = CreateServices();
        var mapper = services.GetRequiredService<IMapper>();
        var existing = new ServiceType { Description = "Anterior" };

        Assert.Same(existing, mapper.Map(new ServiceTypeInput(" Consulta ", " "), existing));
        Assert.Null(existing.Description);
    }

    private static ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddApplication().AddInfrastructure(
            "Server=localhost;Database=HealthAuthorizationModelOnly;Integrated Security=true;Encrypt=true");
        return services.BuildServiceProvider();
    }
}
