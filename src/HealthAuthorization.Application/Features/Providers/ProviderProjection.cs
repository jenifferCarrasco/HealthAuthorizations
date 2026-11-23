using System.Linq.Expressions;
using HealthAuthorization.Domain.Entities;

namespace HealthAuthorization.Application.Features.Providers;

internal static class ProviderProjection
{
    internal static readonly Expression<Func<Provider, ProviderDto>> ToDto =
        entity => new ProviderDto(entity.Id, entity.Name, entity.Identification, entity.ProviderTypeId, entity.IsActive);
}
