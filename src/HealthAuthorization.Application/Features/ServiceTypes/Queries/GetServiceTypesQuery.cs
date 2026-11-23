using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Application.Common.Models;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.ServiceTypes.Queries;

public class GetServiceTypesQuery : PaginationFilter, IRequest<Page<ServiceTypeDto>>
{
    public bool? IsActive { get; set; }
}

public class GetServiceTypesQueryHandler : IRequestHandler<GetServiceTypesQuery, Page<ServiceTypeDto>>
{
    private readonly IRepositoryAsync<ServiceType> _repository;

    public GetServiceTypesQueryHandler(IRepositoryAsync<ServiceType> repository)
    {
        _repository = repository;
    }

    public Task<Page<ServiceTypeDto>> Handle(GetServiceTypesQuery request, CancellationToken cancellationToken)
    {
        var search = InputLimits.NormalizeOptional(request.Search);
        return _repository.ListAsync(entity =>
            (request.IsActive == null || entity.IsActive == request.IsActive) &&
            (search == null || entity.Name.StartsWith(search)),
            query => query.OrderBy(entity => entity.Name).ThenBy(entity => entity.Id),
            ServiceTypeProjection.ToDto, request.PageNumber, request.PageSize, cancellationToken);
    }
}
