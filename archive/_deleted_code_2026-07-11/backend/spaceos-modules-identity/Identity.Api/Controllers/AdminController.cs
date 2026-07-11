// Identity.Api/Controllers/AdminController.cs

using Ardalis.Result.AspNetCore;
using Identity.Application.Users.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Api.Controllers;

[ApiController]
[Route("identity/admin")]
[Authorize(Policy = "SuperAdmin")]
public sealed class AdminController : IdentityControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator) => _mediator = mediator;

    [HttpPost("tenants/{tenantId:guid}/sync-from-keycloak")]
    public async Task<IActionResult> SyncFromKeycloak(Guid tenantId, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new SyncTenantUsersFromKeycloakCommand(tenantId), ct).ConfigureAwait(false);
        return Respond(result);
    }
}
