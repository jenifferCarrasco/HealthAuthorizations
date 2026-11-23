using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;

namespace HealthAuthorization.Application.Features.Affiliates;

internal static class AffiliateWriter
{
    internal static async Task ApplyAsync(
        Affiliate affiliate, AffiliateInput input, Guid? excludedId, IAffiliateRepository affiliates,
        IRepositoryAsync<Contractor> contractors, CancellationToken cancellationToken)
    {
        var number = input.AffiliateNumber.Trim();
        var identification = InputLimits.NormalizeOptional(input.Identification);
        if (input.ContractorId is Guid contractorId &&
            !await contractors.ExistsAsync(contractorId, cancellationToken))
        {
            throw new NotFoundException("El contratante no existe.");
        }

        if (await affiliates.NumberExistsAsync(number, excludedId, cancellationToken))
        {
            throw new ConflictException("El número de afiliado ya existe.");
        }

        if (identification is not null &&
            await affiliates.IdentificationExistsAsync(identification, excludedId, cancellationToken))
        {
            throw new ConflictException("La identificación ya existe.");
        }

        affiliate.AffiliateNumber = number;
        affiliate.FirstName = input.FirstName.Trim();
        affiliate.LastName = input.LastName.Trim();
        affiliate.BirthDate = input.BirthDate;
        affiliate.Identification = identification;
        affiliate.DocumentType = input.DocumentType;
        affiliate.ContractorId = input.ContractorId;
    }
}
