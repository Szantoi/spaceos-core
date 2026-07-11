// SpaceOS.Kernel.Application/Spaces/Queries/DTOs/GetSpatialTimelineEventsQuery.cs

using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.Spaces.Queries;

/// <summary>
/// Query to retrieve the full timeline of spatial state changes for a physical space.
/// Returns all FlowTask state change events joined with their spatial elements.
/// </summary>
/// <param name="PhysicalSpaceId">The physical space to query.</param>
public sealed record GetSpatialTimelineEventsQuery(
    Guid PhysicalSpaceId) : IRequest<Result<List<SpatialTimelineEventDto>>>;
