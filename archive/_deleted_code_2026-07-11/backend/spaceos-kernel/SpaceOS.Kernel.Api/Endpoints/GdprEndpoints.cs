// SpaceOS.Kernel.Api/Endpoints/GdprEndpoints.cs

using MediatR;
using SpaceOS.Kernel.Api.Extensions;
using SpaceOS.Kernel.Application.UserProfiles.Commands;

namespace SpaceOS.Kernel.Api.Endpoints;

/// <summary>Registers GDPR Minimal API endpoints.</summary>
public static class GdprEndpoints
{
    /// <summary>Maps all GDPR endpoints to the provided route builder.</summary>
    /// <param name="app">The endpoint route builder to register routes on.</param>
    /// <returns>The same <see cref="IEndpointRouteBuilder"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapGdprEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/gdpr").WithTags("GDPR");

        group.MapPost("/erase-user", async (
            EraseUserRequest request,
            IMediator mediator = default!,
            CancellationToken ct = default) =>
        {
            var result = await mediator
                .Send(new EraseUserCommand(request.ExternalUserId, request.TenantId), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("EraseUser")
        .WithSummary("GDPR: erase user PII from the audit log")
        .WithDescription(
            "Replaces the stored JWT sub claim for the specified user with '[ERASED]', " +
            "satisfying a GDPR right-to-erasure request. " +
            "The pseudonym GUID is preserved so existing audit log references remain valid. " +
            "Returns 204 on success, 404 when no profile exists for the given identity.")
        .Produces(204)
        .ProducesValidationProblem(422)
        .ProducesProblem(404)
        .ProducesProblem(429)
        .RequireAuthorization("AdminPolicy")
        .RequireRateLimiting("fixed");

        return app;
    }
}

/// <summary>Request body for the <c>POST /api/gdpr/erase-user</c> endpoint.</summary>
/// <param name="ExternalUserId">The JWT <c>sub</c> claim of the user to erase.</param>
/// <param name="TenantId">The tenant the user belongs to.</param>
public sealed record EraseUserRequest(string ExternalUserId, Guid TenantId);
