using System.Linq.Expressions;
using HealthAuthorization.Domain.Entities;

namespace HealthAuthorization.Application.Features.ProviderTypes;

internal static class ProviderTypeProjection
{
    internal static readonly Expression<Func<ProviderType, ProviderTypeDto>> ToDto =
        entity => new ProviderTypeDto(entity.Id, entity.Name, entity.Description, entity.IsActive);
}
