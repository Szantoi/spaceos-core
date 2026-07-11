// SpaceOS.Kernel.Application/Handshakes/Queries/GetAllowedHostsQueryHandler.cs

using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Domain.Auth;
using SpaceOS.Kernel.Domain.DTOs;
using SpaceOS.Kernel.Domain.Repositories;

namespace SpaceOS.Kernel.Application.Handshakes.Queries;

/// <summary>
/// Handles <see cref="GetAllowedHostsQuery"/>: returns the B2B allowed host tenants
/// for the current authenticated tenant (capped at 20 — SEC-P3CP-08).
/// Returns <see cref="Result.Unauthorized"/> when no tenant context is available.
/// </summary>
internal sealed class GetAllowedHostsQueryHandler
    : IRequestHandler<GetAllowedHostsQuery, Result<IReadOnlyList<AllowedHostDto>>>
{
    private readonly ITenantHandshakeAllowlistRepository _allowlistRepo;
    private readonly ITenantResolver _tenantResolver;

    /// <summary>Initialises a new <see cref="GetAllowedHostsQueryHandler"/>.</summary>
    public GetAllowedHostsQueryHandler(
        ITenantHandshakeAllowlistRepository allowlistRepo,
        ITenantResolver tenantResolver)
    {
        ArgumentNullException.ThrowIfNull(allowlistRepo);
        ArgumentNullException.ThrowIfNull(tenantResolver);
        _allowlistRepo  = allowlistRepo;
        _tenantResolver = tenantResolver;
    }

    /// <summary>Executes the query and returns the allowed hosts list.</summary>
    /// <param name="request">The query (no parameters required).</param>
    /// <param name="ct">A token that can be used to cancel the operation.</param>
    public async Task<Result<IReadOnlyList<AllowedHostDto>>> Handle(
        GetAllowedHostsQuery request, CancellationToken ct)
    {
        var tenantId = _tenantResolver.TryResolve();
        if (tenantId is null)
            return Result<IReadOnlyList<AllowedHostDto>>.Unauthorized();

        var hosts = await _allowlistRepo
            .GetAllowedHostsAsync(tenantId.Value.Value, ct)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<AllowedHostDto>>.Success(hosts);
    }
}
