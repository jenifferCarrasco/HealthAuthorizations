using System.Linq.Expressions;
using HealthAuthorization.Domain.Entities;

namespace HealthAuthorization.Application.Features.Intermediaries;

internal static class IntermediaryProjection
{
    internal static readonly Expression<Func<Intermediary, IntermediaryDto>> ToDto =
        entity => new IntermediaryDto(entity.Id, entity.Name, entity.Identification, entity.IsActive);
}
