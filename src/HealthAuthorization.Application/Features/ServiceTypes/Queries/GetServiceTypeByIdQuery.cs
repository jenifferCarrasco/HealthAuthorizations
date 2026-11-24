using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.ServiceTypes.Queries;

public class GetServiceTypeByIdQuery : IRequest<ServiceTypeDto>
{
    public Guid Id { get; set; }

    public GetServiceTypeByIdQuery(Guid Id)
    {
        this.Id = Id;
    }
}

public class GetServiceTypeByIdQueryHandler : IRequestHandler<GetServiceTypeByIdQuery, ServiceTypeDto>
{
    private readonly IRepositoryAsync<ServiceType> _repository;

    public GetServiceTypeByIdQueryHandler(IRepositoryAsync<ServiceType> repository)
    {
        _repository = repository;
    }

    public async Task<ServiceTypeDto> Handle(GetServiceTypeByIdQuery request, CancellationToken cancellationToken)
    {
        return await _repository.FindAsync(request.Id, ServiceTypeProjection.ToDto, cancellationToken)
            ?? throw new NotFoundException("El tipo de servicio no existe.");
    }
}
