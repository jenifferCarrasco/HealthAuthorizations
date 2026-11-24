using System.Linq.Expressions;
using HealthAuthorization.Domain.Entities;

namespace HealthAuthorization.Application.Features.Contractors;

internal static class ContractorProjection
{
    internal static readonly Expression<Func<Contractor, ContractorDto>> ToDto =
        entity => new ContractorDto(entity.Id, entity.Name, entity.Identification, entity.IsActive);
}
