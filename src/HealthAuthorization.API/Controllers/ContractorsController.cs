using HealthAuthorization.API.Contracts;
using HealthAuthorization.Application.Features.Contractors;
using HealthAuthorization.Application.Features.Contractors.Commands;
using HealthAuthorization.Application.Features.Contractors.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HealthAuthorization.API.Controllers;

[ApiController]
[Route("api/contractors")]
public class ContractorsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ContractorsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] GetContractorsQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var query = new GetContractorByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ContractorInput input, CancellationToken cancellationToken)
    {
        var command = new CreateContractorCommand(input);
        var id = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id, [FromBody] ContractorInput input, CancellationToken cancellationToken)
    {
        var command = new UpdateContractorCommand(id, input);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteContractorCommand(id);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(
        [FromRoute] Guid id, [FromBody] ChangeStatusRequest input, CancellationToken cancellationToken)
    {
        var command = new ChangeContractorStatusCommand(id, input.IsActive);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
