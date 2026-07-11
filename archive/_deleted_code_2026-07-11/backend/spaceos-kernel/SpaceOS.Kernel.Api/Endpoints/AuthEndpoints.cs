// SpaceOS.Kernel.Api/Endpoints/AuthEndpoints.cs

using MediatR;
using Microsoft.AspNetCore.Mvc;
using SpaceOS.Kernel.Api.Extensions;
using SpaceOS.Kernel.Application.Auth.Commands;

namespace SpaceOS.Kernel.Api.Endpoints;

/// <summary>Registers authentication-related Minimal API endpoints (logout).</summary>
/// <remarks>
/// Refresh token rotation is no longer handled here — Keycloak manages token lifecycle.
/// The JWKS endpoint has been removed; JWT validation keys are fetched automatically from
/// the Keycloak authority's <c>/.well-known/openid-configuration</c> discovery document.
/// </remarks>
public static class AuthEndpoints
{
    /// <summary>Maps all auth endpoints to the provided route builder.</summary>
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        // --- POST /api/auth/logout ---
        app.MapPost("/api/auth/logout", async (
            [FromBody] RefreshRequest request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator
                .Send(new RevokeTokenCommand(request.RefreshToken), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .AllowAnonymous()
        .RequireRateLimiting("fixed")
        .WithTags("Auth")
        .WithName("RevokeToken")
        .WithSummary("Revoke a refresh token (logout).")
        .WithDescription(
            "Revokes the presented refresh token. Idempotent: if the token does not exist " +
            "or is already revoked, 200 OK is returned (BE-P15-11).")
        .Produces(200)
        .ProducesValidationProblem(422)
        .ProducesProblem(429);

        return app;
    }
}

/// <summary>Request body for refresh and logout endpoints.</summary>
/// <param name="RefreshToken">The raw 43-character Base64Url opaque refresh token.</param>
internal sealed record RefreshRequest(string RefreshToken);
