using HealthAuthorization.Application.Common;
using HealthAuthorization.Application.Features.Contractors;
using HealthAuthorization.Application.Features.Intermediaries;
using HealthAuthorization.Application.Features.Providers;
using HealthAuthorization.Application.Features.ProviderTypes;
using HealthAuthorization.Application.Features.ServiceTypes;
using HealthAuthorization.Domain.Entities;
using Mapster;

namespace HealthAuthorization.Application.Mapping;

internal static class CatalogMappings
{
    internal static TypeAdapterConfig Create()
    {
        var configuration = new TypeAdapterConfig { RequireExplicitMapping = true };

        configuration.NewConfig<ContractorInput, Contractor>()
            .IgnoreNonMapped(true)
            .Map(entity => entity.Name, input => input.Name.Trim())
            .Map(entity => entity.Identification, input => input.Identification.Trim());

        configuration.NewConfig<IntermediaryInput, Intermediary>()
            .IgnoreNonMapped(true)
            .Map(entity => entity.Name, input => input.Name.Trim())
            .Map(entity => entity.Identification, input => input.Identification.Trim());

        configuration.NewConfig<ProviderInput, Provider>()
            .IgnoreNonMapped(true)
            .Map(entity => entity.Name, input => input.Name.Trim())
            .Map(entity => entity.Identification, input => input.Identification.Trim())
            .Map(entity => entity.ProviderTypeId, input => input.ProviderTypeId);

        configuration.NewConfig<ProviderTypeInput, ProviderType>()
            .IgnoreNonMapped(true)
            .Map(entity => entity.Name, input => input.Name.Trim())
            .Map(entity => entity.Description, input => InputLimits.NormalizeOptional(input.Description));

        configuration.NewConfig<ServiceTypeInput, ServiceType>()
            .IgnoreNonMapped(true)
            .Map(entity => entity.Name, input => input.Name.Trim())
            .Map(entity => entity.Description, input => InputLimits.NormalizeOptional(input.Description));

        return configuration;
    }
}
