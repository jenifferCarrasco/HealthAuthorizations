using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.Contractors.Queries;

public class GetContractorByIdQuery : IRequest<ContractorDto>
{
    public Guid Id { get; set; }

    public GetContractorByIdQuery(Guid Id)
    {
        this.Id = Id;
    }
}

public class GetContractorByIdQueryHandler : IRequestHandler<GetContractorByIdQuery, ContractorDto>
{
    private readonly IRepositoryAsync<Contractor> _repository;

    public GetContractorByIdQueryHandler(IRepositoryAsync<Contractor> repository)
    {
        _repository = repository;
    }

    public async Task<ContractorDto> Handle(GetContractorByIdQuery request, CancellationToken cancellationToken)
    {
        return await _repository.FindAsync(request.Id, ContractorProjection.ToDto, cancellationToken)
            ?? throw new NotFoundException("El contratante no existe.");
    }
}
