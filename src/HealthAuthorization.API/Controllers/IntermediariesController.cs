using HealthAuthorization.API.Contracts;
using HealthAuthorization.Application.Features.Intermediaries;
using HealthAuthorization.Application.Features.Intermediaries.Commands;
using HealthAuthorization.Application.Features.Intermediaries.Queries;
using HealthAuthorization.API.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthAuthorization.API.Controllers;

[ApiController]
[Authorize(Policy = SecurityConfiguration.BusinessAccess)]
[Route("api/intermediaries")]
public class IntermediariesController : ControllerBase
{
    private readonly IMediator _mediator;

    public IntermediariesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] GetIntermediariesQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var query = new GetIntermediaryByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = SecurityConfiguration.Administrator)]
    public async Task<IActionResult> Create([FromBody] IntermediaryInput input, CancellationToken cancellationToken)
    {
        var command = new CreateIntermediaryCommand(input);
        var id = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = SecurityConfiguration.Administrator)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id, [FromBody] IntermediaryInput input, CancellationToken cancellationToken)
    {
        var command = new UpdateIntermediaryCommand(id, input);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = SecurityConfiguration.Administrator)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteIntermediaryCommand(id);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = SecurityConfiguration.Administrator)]
    public async Task<IActionResult> ChangeStatus(
        [FromRoute] Guid id, [FromBody] ChangeStatusRequest input, CancellationToken cancellationToken)
    {
        var command = new ChangeIntermediaryStatusCommand(id, input.IsActive);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
