using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Application.Common.Models;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.ProviderTypes.Queries;

public class GetProviderTypesQuery : SearchablePaginationFilter, IRequest<Page<ProviderTypeDto>>
{
    public bool? IsActive { get; set; }
}

public class GetProviderTypesQueryHandler : IRequestHandler<GetProviderTypesQuery, Page<ProviderTypeDto>>
{
    private readonly IRepositoryAsync<ProviderType> _repository;

    public GetProviderTypesQueryHandler(IRepositoryAsync<ProviderType> repository)
    {
        _repository = repository;
    }

    public Task<Page<ProviderTypeDto>> Handle(GetProviderTypesQuery request, CancellationToken cancellationToken)
    {
        var search = InputLimits.NormalizeOptional(request.Search);
        return _repository.ListAsync(entity =>
            (request.IsActive == null || entity.IsActive == request.IsActive) &&
            (search == null || entity.Name.StartsWith(search)),
            query => query.OrderBy(entity => entity.Name).ThenBy(entity => entity.Id),
            ProviderTypeProjection.ToDto, request.PageNumber, request.PageSize, cancellationToken);
    }
}
