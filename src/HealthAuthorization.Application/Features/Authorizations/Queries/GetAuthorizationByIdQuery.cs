using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.Authorizations.Queries;

public class GetAuthorizationByIdQuery : IRequest<AuthorizationDto>
{
    public Guid Id { get; set; }

    public GetAuthorizationByIdQuery(Guid Id)
    {
        this.Id = Id;
    }
}

public class GetAuthorizationByIdQueryHandler : IRequestHandler<GetAuthorizationByIdQuery, AuthorizationDto>
{
    private readonly IRepositoryAsync<Authorization> _repository;

    public GetAuthorizationByIdQueryHandler(IRepositoryAsync<Authorization> repository)
    {
        _repository = repository;
    }

    public async Task<AuthorizationDto> Handle(GetAuthorizationByIdQuery request, CancellationToken cancellationToken)
    {
        var result = await _repository.FindAsync(request.Id, entity => new AuthorizationDto(
            new AuthorizationSummaryDto(entity.Id, entity.AffiliateId, entity.ContractorId, entity.IntermediaryId,
                entity.IsActive, entity.CreatedDate, entity.Details.Where(detail => !detail.IsDelete).Sum(detail => detail.ClaimedAmount),
                entity.Details.Where(detail => !detail.IsDelete).Sum(detail => detail.ArsAmount), entity.Details.Where(detail => !detail.IsDelete).Sum(detail => detail.AffiliateAmount)),
            entity.Details.Where(detail => !detail.IsDelete).OrderBy(detail => detail.Id).Select(detail => new AuthorizationDetailDto(
                detail.Id, detail.ProviderId, detail.ServiceTypeId, detail.ClaimedAmount,
                detail.ArsAmount, detail.AffiliateAmount)).ToList()), cancellationToken);
        return result ?? throw new NotFoundException("La autorización no existe.");
    }
}
