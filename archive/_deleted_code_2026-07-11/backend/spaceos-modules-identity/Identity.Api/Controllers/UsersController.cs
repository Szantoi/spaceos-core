// Identity.Api/Controllers/UsersController.cs

using Ardalis.Result.AspNetCore;
using Identity.Application.Common.DTOs;
using Identity.Application.Users.Commands;
using Identity.Application.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Api.Controllers;

[ApiController]
[Route("identity/users")]
[Authorize]
public sealed class UsersController : IdentityControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Policy = "TenantMember")]
    public async Task<IActionResult> ListUsers([FromQuery] string? role, CancellationToken ct)
    {
        // If role query parameter is provided, use GetUsersByRoleQuery
        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleResult = await _mediator.Send(new GetUsersByRoleQuery(role), ct).ConfigureAwait(false);
            return Respond(roleResult);
        }

        // Otherwise, use the original ListTenantUsersQuery
        var result = await _mediator.Send(new ListTenantUsersQuery(), ct).ConfigureAwait(false);
        return Respond(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "TenantMember")]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetUserByIdQuery(id), ct).ConfigureAwait(false);
        return Respond(result);
    }

    [HttpPost]
    [Authorize(Policy = "TenantAdmin")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new CreateUserCommand(dto.Email, dto.FirstName, dto.LastName), ct).ConfigureAwait(false);

        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetUser), new { id = result.Value.Id }, result.Value);

        return Respond(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "TenantAdmin")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserProfileDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new UpdateUserProfileCommand(id, dto.FirstName, dto.LastName), ct).ConfigureAwait(false);
        return Respond(result);
    }

    [HttpPost("{id:guid}/disable")]
    [Authorize(Policy = "TenantAdmin")]
    public async Task<IActionResult> DisableUser(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DisableUserCommand(id), ct).ConfigureAwait(false);
        return Respond(result);
    }

    [HttpPost("{id:guid}/enable")]
    [Authorize(Policy = "TenantAdmin")]
    public async Task<IActionResult> EnableUser(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new EnableUserCommand(id), ct).ConfigureAwait(false);
        return Respond(result);
    }

    [HttpPost("{id:guid}/reset-password")]
    [Authorize(Policy = "TenantAdmin")]
    public async Task<IActionResult> ResetPassword(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new ResetPasswordCommand(id), ct).ConfigureAwait(false);
        return Respond(result);
    }

    [HttpPatch("{id:guid}/operator-pin")]
    [Authorize(Policy = "TenantAdmin")]
    public async Task<IActionResult> SetOperatorPin(Guid id, [FromBody] SetOperatorPinDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new SetOperatorPinCommand(id, dto.Pin), ct).ConfigureAwait(false);
        return Respond(result);
    }

    [HttpDelete("{id:guid}/operator-pin")]
    [Authorize(Policy = "TenantAdmin")]
    public async Task<IActionResult> ClearOperatorPin(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new ClearOperatorPinCommand(id), ct).ConfigureAwait(false);
        return Respond(result);
    }
}
