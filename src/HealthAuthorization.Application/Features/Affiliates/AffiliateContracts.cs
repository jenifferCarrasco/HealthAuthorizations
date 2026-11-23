using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;
using HealthAuthorization.Domain.Enums;

namespace HealthAuthorization.Application.Features.Affiliates;

public sealed record AffiliateInput(
    string AffiliateNumber, string FirstName, string LastName, DateOnly BirthDate,
    string? Identification = null, DocumentType DocumentType = DocumentType.None, Guid? ContractorId = null);

public sealed record AffiliateDto(
    Guid Id, string AffiliateNumber, string FirstName, string LastName, DateOnly BirthDate,
    string? Identification, DocumentType DocumentType, Guid? ContractorId);

public interface IAffiliateRepository : IRepositoryAsync<Affiliate>
{
    Task<bool> NumberExistsAsync(string number, Guid? excludedId, CancellationToken cancellationToken);
    Task<bool> IdentificationExistsAsync(string identification, Guid? excludedId, CancellationToken cancellationToken);
    Task<AffiliateDto?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<Page<AffiliateDto>> ListAsync(string? search, int pageNumber, int pageSize, CancellationToken cancellationToken);
}
