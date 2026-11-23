using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Entities;
using MediatR;

namespace HealthAuthorization.Application.Features.Affiliates.Queries;

public class GetAffiliateByIdQuery : IRequest<AffiliateDto>
{
    public Guid Id { get; set; }

    public GetAffiliateByIdQuery(Guid Id)
    {
        this.Id = Id;
    }
}

public class GetAffiliateByIdQueryHandler : IRequestHandler<GetAffiliateByIdQuery, AffiliateDto>
{
    private readonly IRepositoryAsync<Affiliate> _repository;

    public GetAffiliateByIdQueryHandler(IRepositoryAsync<Affiliate> repository)
    {
        _repository = repository;
    }

    public async Task<AffiliateDto> Handle(GetAffiliateByIdQuery request, CancellationToken cancellationToken)
    {
        return await _repository.FindAsync(request.Id, AffiliateProjection.ToDto, cancellationToken)
            ?? throw new NotFoundException("El afiliado no existe.");
    }
}
