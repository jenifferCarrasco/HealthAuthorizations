using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.Affiliates.Queries;

public class GetAffiliatesQuery : IRequest<Page<AffiliateDto>>
{
    public string? Search { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public bool? IsActive { get; set; }

    public GetAffiliatesQuery(string? Search = null, int PageNumber = 1, int PageSize = 20, bool? IsActive = null)
    {
        this.Search = Search;
        this.PageNumber = PageNumber;
        this.PageSize = PageSize;
        this.IsActive = IsActive;
    }
}

public class GetAffiliatesQueryHandler : IRequestHandler<GetAffiliatesQuery, Page<AffiliateDto>>
{
    private readonly IRepositoryAsync<Affiliate> _repository;

    public GetAffiliatesQueryHandler(IRepositoryAsync<Affiliate> repository)
    {
        _repository = repository;
    }

    public Task<Page<AffiliateDto>> Handle(GetAffiliatesQuery request, CancellationToken cancellationToken)
    {
        var search = InputLimits.NormalizeOptional(request.Search);
        return _repository.ListAsync(entity =>
            (request.IsActive == null || entity.IsActive == request.IsActive) &&
            (search == null || entity.AffiliateNumber.StartsWith(search) || entity.FirstName.StartsWith(search) || entity.LastName.StartsWith(search) ||
                (entity.Identification != null && entity.Identification.StartsWith(search))),
            query => query.OrderBy(entity => entity.AffiliateNumber).ThenBy(entity => entity.Id),
            AffiliateProjection.ToDto, request.PageNumber, request.PageSize, cancellationToken);
    }
}
