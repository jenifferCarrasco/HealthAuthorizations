using HealthAuthorization.API.Contracts;
using HealthAuthorization.Application.Features.Providers;
using HealthAuthorization.Application.Features.Providers.Commands;
using HealthAuthorization.Application.Features.Providers.Queries;
using HealthAuthorization.API.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthAuthorization.API.Controllers;

[ApiController]
[Authorize(Policy = SecurityConfiguration.BusinessAccess)]
[Route("api/providers")]
public class ProvidersController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProvidersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] GetProvidersQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var query = new GetProviderByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = SecurityConfiguration.Administrator)]
    public async Task<IActionResult> Create([FromBody] ProviderInput input, CancellationToken cancellationToken)
    {
        var command = new CreateProviderCommand(input);
        var id = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = SecurityConfiguration.Administrator)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id, [FromBody] ProviderInput input, CancellationToken cancellationToken)
    {
        var command = new UpdateProviderCommand(id, input);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = SecurityConfiguration.Administrator)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteProviderCommand(id);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = SecurityConfiguration.Administrator)]
    public async Task<IActionResult> ChangeStatus(
        [FromRoute] Guid id, [FromBody] ChangeStatusRequest input, CancellationToken cancellationToken)
    {
        var command = new ChangeProviderStatusCommand(id, input.IsActive);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
