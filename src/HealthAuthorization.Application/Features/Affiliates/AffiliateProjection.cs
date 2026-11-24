using System.Linq.Expressions;
using HealthAuthorization.Domain.Entities;

namespace HealthAuthorization.Application.Features.Affiliates;

internal static class AffiliateProjection
{
    internal static readonly Expression<Func<Affiliate, AffiliateDto>> ToDto =
        entity => new AffiliateDto(entity.Id, entity.AffiliateNumber, entity.FirstName, entity.LastName, entity.BirthDate, entity.Identification, entity.DocumentType, entity.ContractorId, entity.IsActive);
}
