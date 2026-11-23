using FluentValidation;
using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;
using HealthAuthorization.Domain.Policies;

namespace HealthAuthorization.Application.Features.Authorizations;

public class AuthorizationWriter
{
    private readonly IRepositoryAsync<Affiliate> _affiliates;
    private readonly IRepositoryAsync<Contractor> _contractors;
    private readonly IRepositoryAsync<Intermediary> _intermediaries;
    private readonly IRepositoryAsync<Provider> _providers;
    private readonly IRepositoryAsync<ServiceType> _serviceTypes;

    public AuthorizationWriter(IRepositoryAsync<Affiliate> affiliates, IRepositoryAsync<Contractor> contractors, IRepositoryAsync<Intermediary> intermediaries, IRepositoryAsync<Provider> providers, IRepositoryAsync<ServiceType> serviceTypes)
    {
        _affiliates = affiliates;
        _contractors = contractors;
        _intermediaries = intermediaries;
        _providers = providers;
        _serviceTypes = serviceTypes;
    }

    internal async Task ApplyAsync(Authorization authorization, AuthorizationInput input, CancellationToken cancellationToken)
    {
        if (!authorization.IsActive)
        {
            throw new ConflictException("No se puede modificar una autorización inactiva.");
        }

        if (input.Services is not { Count: > 0 })
        {
            throw new ValidationException("La autorización debe contener al menos un servicio.");
        }

        if (input.AffiliateId == Guid.Empty || (authorization.AffiliateId != input.AffiliateId &&
            !await _affiliates.ExistsAsync(input.AffiliateId, cancellationToken)))
        {
            throw new NotFoundException("El afiliado no existe.");
        }

        if (input.ContractorId is Guid contractorId && contractorId != authorization.ContractorId &&
            !await _contractors.ExistsAsync(contractorId, cancellationToken))
        {
            throw new NotFoundException("El contratante no existe.");
        }

        if (input.IntermediaryId is Guid intermediaryId && intermediaryId != authorization.IntermediaryId &&
            !await _intermediaries.ExistsAsync(intermediaryId, cancellationToken))
        {
            throw new NotFoundException("El intermediario no existe.");
        }

        var existing = authorization.Details.Where(detail => !detail.IsDelete).ToDictionary(detail => detail.Id);
        var planned = new List<(AuthorizationDetail? Existing, AuthorizationDetail Values)>();
        foreach (var service in input.Services)
        {
            AuthorizationDetail? match = null;
            if (service.DetailId is Guid detailId && !existing.Remove(detailId, out match))
            {
                throw new ValidationException("El detalle no pertenece a la autorización, está eliminado o está repetido.");
            }

            var (arsAmount, affiliateAmount) = CoveragePolicy.Calculate(service.ClaimedAmount);
            planned.Add((match, new AuthorizationDetail
            {
                AuthorizationId = authorization.Id,
                ProviderId = service.ProviderId,
                ServiceTypeId = service.ServiceTypeId,
                ClaimedAmount = service.ClaimedAmount,
                ArsAmount = arsAmount,
                AffiliateAmount = affiliateAmount
            }));
        }

        var providerIds = planned.Where(change => change.Existing?.ProviderId != change.Values.ProviderId)
            .Select(change => change.Values.ProviderId).Distinct().ToArray();
        if (!await _providers.ExistAllAsync(providerIds, cancellationToken))
        {
            throw new NotFoundException("Uno o más prestadores no existen.");
        }

        var serviceTypeIds = planned.Where(change => change.Existing?.ServiceTypeId != change.Values.ServiceTypeId)
            .Select(change => change.Values.ServiceTypeId).Distinct().ToArray();
        if (!await _serviceTypes.ExistAllAsync(serviceTypeIds, cancellationToken))
        {
            throw new NotFoundException("Uno o más tipos de servicio no existen.");
        }

        var candidate = new Authorization { Details = planned.Select(change => change.Values).ToList() };
        try
        {
            if (candidate.TotalClaimedAmount != candidate.TotalArsAmount + candidate.TotalAffiliateAmount ||
                candidate.TotalClaimedAmount > InputLimits.MaxClaimedAmount)
            {
                throw new ValidationException("El monto total de la autorización supera la precisión monetaria admitida o no es consistente.");
            }
        }
        catch (OverflowException)
        {
            throw new ValidationException("El monto total de la autorización supera la precisión monetaria admitida.");
        }

        authorization.AffiliateId = input.AffiliateId;
        authorization.ContractorId = input.ContractorId;
        authorization.IntermediaryId = input.IntermediaryId;

        foreach (var removed in existing.Values)
        {
            removed.IsDelete = true;
        }

        foreach (var (detail, values) in planned)
        {
            if (detail is null)
            {
                authorization.Details.Add(values);
                continue;
            }

            detail.ProviderId = values.ProviderId;
            detail.ServiceTypeId = values.ServiceTypeId;
            detail.ClaimedAmount = values.ClaimedAmount;
            detail.ArsAmount = values.ArsAmount;
            detail.AffiliateAmount = values.AffiliateAmount;
        }
    }
}
