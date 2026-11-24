using System.Linq.Expressions;
using HealthAuthorization.Domain.Entities;

namespace HealthAuthorization.Application.Features.ServiceTypes;

internal static class ServiceTypeProjection
{
    internal static readonly Expression<Func<ServiceType, ServiceTypeDto>> ToDto =
        entity => new ServiceTypeDto(entity.Id, entity.Name, entity.Description, entity.IsActive);
}
