// SpaceOS.Kernel.Api/Endpoints/SnapshotEndpoints.cs

using MediatR;
using SpaceOS.Kernel.Api.Extensions;
using SpaceOS.Kernel.Application.Snapshots;
using SpaceOS.Kernel.Application.Snapshots.Queries;

namespace SpaceOS.Kernel.Api.Endpoints;

/// <summary>Registers Snapshot Minimal API endpoints.</summary>
public static class SnapshotEndpoints
{
    /// <summary>Maps all snapshot endpoints to the provided route builder.</summary>
    /// <param name="app">The endpoint route builder to register routes on.</param>
    /// <returns>The same <see cref="IEndpointRouteBuilder"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapSnapshotEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/snapshots").WithTags("Snapshots");

        group.MapGet("/{aggregateId:guid}", async (
            Guid aggregateId,
            DateTimeOffset? at,
            IMediator mediator = default!,
            CancellationToken ct = default) =>
        {
            if (!at.HasValue)
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        ["at"] = ["The 'at' query parameter is required."]
                    });

            var result = await mediator.Send(
                new GetSnapshotAtQuery(aggregateId, at.Value), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("GetSnapshotAt")
        .WithSummary("Get aggregate snapshot at a point in time")
        .WithDescription("Returns the most recent snapshot for the given aggregate taken at or before the specified timestamp.")
        .Produces<SnapshotDto>(200)
        .ProducesValidationProblem(422)
        .ProducesProblem(429)
        .RequireAuthorization()
        .RequireRateLimiting("fixed");

        group.MapGet("/{aggregateId:guid}/versions", async (
            Guid aggregateId,
            IMediator mediator = default!,
            CancellationToken ct = default) =>
        {
            var result = await mediator.Send(
                new GetSnapshotVersionsQuery(aggregateId), ct)
                .ConfigureAwait(false);
            return result.ToApiResult();
        })
        .WithName("GetSnapshotVersions")
        .WithSummary("List all snapshot versions for an aggregate")
        .WithDescription("Returns all snapshots recorded for the given aggregate, ordered by version ascending.")
        .Produces<IReadOnlyList<SnapshotDto>>(200)
        .ProducesProblem(429)
        .RequireAuthorization()
        .RequireRateLimiting("fixed");

        return app;
    }
}
