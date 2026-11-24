using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.Intermediaries.Queries;

public class GetIntermediaryByIdQuery : IRequest<IntermediaryDto>
{
    public Guid Id { get; set; }

    public GetIntermediaryByIdQuery(Guid Id)
    {
        this.Id = Id;
    }
}

public class GetIntermediaryByIdQueryHandler : IRequestHandler<GetIntermediaryByIdQuery, IntermediaryDto>
{
    private readonly IRepositoryAsync<Intermediary> _repository;

    public GetIntermediaryByIdQueryHandler(IRepositoryAsync<Intermediary> repository)
    {
        _repository = repository;
    }

    public async Task<IntermediaryDto> Handle(GetIntermediaryByIdQuery request, CancellationToken cancellationToken)
    {
        return await _repository.FindAsync(request.Id, IntermediaryProjection.ToDto, cancellationToken)
            ?? throw new NotFoundException("El intermediario no existe.");
    }
}
