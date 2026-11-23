using HealthAuthorization.API.Contracts;
using HealthAuthorization.Application.Features.Affiliates;
using HealthAuthorization.Application.Features.Affiliates.Commands;
using HealthAuthorization.Application.Features.Affiliates.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HealthAuthorization.API.Controllers;

[ApiController]
[Route("api/affiliates")]
public class AffiliatesController : ControllerBase
{
    private readonly IMediator _mediator;

    public AffiliatesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] GetAffiliatesQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var query = new GetAffiliateByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AffiliateInput input, CancellationToken cancellationToken)
    {
        var command = new CreateAffiliateCommand(input);
        var id = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id, [FromBody] AffiliateInput input, CancellationToken cancellationToken)
    {
        var command = new UpdateAffiliateCommand(id, input);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteAffiliateCommand(id);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(
        [FromRoute] Guid id, [FromBody] ChangeStatusRequest input, CancellationToken cancellationToken)
    {
        var command = new ChangeAffiliateStatusCommand(id, input.IsActive);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
