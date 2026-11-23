using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.Intermediaries.Queries;

public class GetIntermediariesQuery : IRequest<Page<IntermediaryDto>>
{
    public string? Search { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public bool? IsActive { get; set; }

    public GetIntermediariesQuery(string? Search = null, int PageNumber = 1, int PageSize = 20, bool? IsActive = null)
    {
        this.Search = Search;
        this.PageNumber = PageNumber;
        this.PageSize = PageSize;
        this.IsActive = IsActive;
    }
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
