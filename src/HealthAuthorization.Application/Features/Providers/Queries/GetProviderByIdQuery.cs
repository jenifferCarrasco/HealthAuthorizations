using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.Providers.Queries;

public class GetProviderByIdQuery : IRequest<ProviderDto>
{
    public Guid Id { get; set; }

    public GetProviderByIdQuery(Guid Id)
    {
        this.Id = Id;
    }
}

public class GetProviderByIdQueryHandler : IRequestHandler<GetProviderByIdQuery, ProviderDto>
{
    private readonly IRepositoryAsync<Provider> _repository;

    public GetProviderByIdQueryHandler(IRepositoryAsync<Provider> repository)
    {
        _repository = repository;
    }

    public async Task<ProviderDto> Handle(GetProviderByIdQuery request, CancellationToken cancellationToken)
    {
        return await _repository.FindAsync(request.Id, ProviderProjection.ToDto, cancellationToken)
            ?? throw new NotFoundException("El prestador no existe.");
    }
}
