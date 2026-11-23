using HealthAuthorization.API.Contracts;
using HealthAuthorization.Application.Features.Authorizations;
using HealthAuthorization.Application.Features.Authorizations.Commands;
using HealthAuthorization.Application.Features.Authorizations.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HealthAuthorization.API.Controllers;

[ApiController]
[Route("api/authorizations")]
public class AuthorizationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthorizationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] GetAuthorizationsQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var query = new GetAuthorizationByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AuthorizationInput input, CancellationToken cancellationToken)
    {
        var command = new CreateAuthorizationCommand(input);
        var id = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id, [FromBody] AuthorizationInput input, CancellationToken cancellationToken)
    {
        var command = new UpdateAuthorizationCommand(id, input);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(
        [FromRoute] Guid id, [FromBody] ChangeStatusRequest input, CancellationToken cancellationToken)
    {
        var command = new ChangeAuthorizationStatusCommand(id, input.IsActive);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
