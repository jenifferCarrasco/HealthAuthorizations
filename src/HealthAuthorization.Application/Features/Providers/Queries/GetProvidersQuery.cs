using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Application.Common.Models;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.Providers.Queries;

public class GetProvidersQuery : PaginationFilter, IRequest<Page<ProviderDto>>
{
    public bool? IsActive { get; set; }
    public Guid? ProviderTypeId { get; set; }
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
