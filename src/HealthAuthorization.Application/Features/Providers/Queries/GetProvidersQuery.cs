using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.Providers.Queries;

public class GetProvidersQuery : IRequest<Page<ProviderDto>>
{
    public string? Search { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public bool? IsActive { get; set; }
    public Guid? ProviderTypeId { get; set; }

    public GetProvidersQuery(string? Search = null, int PageNumber = 1, int PageSize = 20, bool? IsActive = null, Guid? ProviderTypeId = null)
    {
        this.Search = Search;
        this.PageNumber = PageNumber;
        this.PageSize = PageSize;
        this.IsActive = IsActive;
        this.ProviderTypeId = ProviderTypeId;
    }
}

public class GetProvidersQueryHandler : IRequestHandler<GetProvidersQuery, Page<ProviderDto>>
{
    private readonly IRepositoryAsync<Provider> _repository;

    public GetProvidersQueryHandler(IRepositoryAsync<Provider> repository)
    {
        _repository = repository;
    }

    public Task<Page<ProviderDto>> Handle(GetProvidersQuery request, CancellationToken cancellationToken)
    {
        var search = InputLimits.NormalizeOptional(request.Search);
        return _repository.ListAsync(entity =>
            (request.IsActive == null || entity.IsActive == request.IsActive) &&
            (request.ProviderTypeId == null || entity.ProviderTypeId == request.ProviderTypeId) &&
            (search == null || entity.Name.StartsWith(search) || entity.Identification.StartsWith(search)),
            query => query.OrderBy(entity => entity.Name).ThenBy(entity => entity.Id),
            ProviderProjection.ToDto, request.PageNumber, request.PageSize, cancellationToken);
    }
}
