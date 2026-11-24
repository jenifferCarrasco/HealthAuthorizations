using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Application.Common.Models;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.Contractors.Queries;

public class GetContractorsQuery : SearchablePaginationFilter, IRequest<Page<ContractorDto>>
{
    public bool? IsActive { get; set; }
}

public class GetContractorsQueryHandler : IRequestHandler<GetContractorsQuery, Page<ContractorDto>>
{
    private readonly IRepositoryAsync<Contractor> _repository;

    public GetContractorsQueryHandler(IRepositoryAsync<Contractor> repository)
    {
        _repository = repository;
    }

    public Task<Page<ContractorDto>> Handle(GetContractorsQuery request, CancellationToken cancellationToken)
    {
        var search = InputLimits.NormalizeOptional(request.Search);
        return _repository.ListAsync(entity =>
            (request.IsActive == null || entity.IsActive == request.IsActive) &&
            (search == null || entity.Name.StartsWith(search) || entity.Identification.StartsWith(search)),
            query => query.OrderBy(entity => entity.Name).ThenBy(entity => entity.Id),
            ContractorProjection.ToDto, request.PageNumber, request.PageSize, cancellationToken);
    }
}
