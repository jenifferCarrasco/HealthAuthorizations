using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.ProviderTypes.Queries;

public class GetProviderTypeByIdQuery : IRequest<ProviderTypeDto>
{
    public Guid Id { get; set; }

    public GetProviderTypeByIdQuery(Guid Id)
    {
        this.Id = Id;
    }
}

public class GetProviderTypeByIdQueryHandler : IRequestHandler<GetProviderTypeByIdQuery, ProviderTypeDto>
{
    private readonly IRepositoryAsync<ProviderType> _repository;

    public GetProviderTypeByIdQueryHandler(IRepositoryAsync<ProviderType> repository)
    {
        _repository = repository;
    }

    public async Task<ProviderTypeDto> Handle(GetProviderTypeByIdQuery request, CancellationToken cancellationToken)
    {
        return await _repository.FindAsync(request.Id, ProviderTypeProjection.ToDto, cancellationToken)
            ?? throw new NotFoundException("El tipo de prestador no existe.");
    }
}
