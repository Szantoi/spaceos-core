// SpaceOS.Kernel.Application/Spaces/Commands/DTOs/RegisterPhysicalSpaceCommand.cs

using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Domain.Enums;

namespace SpaceOS.Kernel.Application.Spaces.Commands;

/// <summary>
/// Command to register a new <see cref="SpaceOS.Kernel.Domain.Aggregates.PhysicalSpace"/> within a facility.
/// Returns the new physical space identifier on success.
/// </summary>
/// <param name="FacilityId">The facility this space belongs to.</param>
/// <param name="WidthMm">Width in millimetres.</param>
/// <param name="HeightMm">Height in millimetres.</param>
/// <param name="DepthMm">Depth in millimetres.</param>
/// <param name="OriginX">Origin X coordinate in millimetres.</param>
/// <param name="OriginY">Origin Y coordinate in millimetres.</param>
/// <param name="OriginZ">Origin Z coordinate in millimetres.</param>
/// <param name="SpaceType">The classification of the space.</param>
/// <param name="CellSizeMm">The spatial grid cell size in millimetres (default 500).</param>
public sealed record RegisterPhysicalSpaceCommand(
    Guid FacilityId,
    int WidthMm, int HeightMm, int DepthMm,
    int OriginX, int OriginY, int OriginZ,
    SpaceType SpaceType,
    int CellSizeMm = 500) : IRequest<Result<Guid>>;
