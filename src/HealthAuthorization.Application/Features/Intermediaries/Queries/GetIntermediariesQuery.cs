using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Application.Common.Models;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.Intermediaries.Queries;

public class GetIntermediariesQuery : PaginationFilter, IRequest<Page<IntermediaryDto>>
{
    public bool? IsActive { get; set; }
}

public class GetIntermediariesQueryHandler : IRequestHandler<GetIntermediariesQuery, Page<IntermediaryDto>>
{
    private readonly IRepositoryAsync<Intermediary> _repository;

    public GetIntermediariesQueryHandler(IRepositoryAsync<Intermediary> repository)
    {
        _repository = repository;
    }

    public Task<Page<IntermediaryDto>> Handle(GetIntermediariesQuery request, CancellationToken cancellationToken)
    {
        var search = InputLimits.NormalizeOptional(request.Search);
        return _repository.ListAsync(entity =>
            (request.IsActive == null || entity.IsActive == request.IsActive) &&
            (search == null || entity.Name.StartsWith(search) || entity.Identification.StartsWith(search)),
            query => query.OrderBy(entity => entity.Name).ThenBy(entity => entity.Id),
            IntermediaryProjection.ToDto, request.PageNumber, request.PageSize, cancellationToken);
    }
}
