// SpaceOS.Kernel.Application/Spaces/Commands/DTOs/RegisterSpatialElementCommand.cs

using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Domain.Enums;

namespace SpaceOS.Kernel.Application.Spaces.Commands;

/// <summary>
/// Command to register a new <see cref="SpaceOS.Kernel.Domain.Entities.SpatialElement"/> within a physical space.
/// The element is inserted into the BVH tree via <see cref="SpaceOS.Kernel.Application.Spaces.Services.IBvhTreeService"/>.
/// Returns the new element identifier on success.
/// </summary>
/// <param name="PhysicalSpaceId">The physical space to insert into.</param>
/// <param name="FlowEpicId">The FlowEpic this element belongs to.</param>
/// <param name="TradeType">The construction trade type.</param>
/// <param name="ElementType">The driver-specific element classification.</param>
/// <param name="MinX">Minimum X coordinate in millimetres.</param>
/// <param name="MinY">Minimum Y coordinate in millimetres.</param>
/// <param name="MinZ">Minimum Z coordinate in millimetres.</param>
/// <param name="MaxX">Maximum X coordinate in millimetres.</param>
/// <param name="MaxY">Maximum Y coordinate in millimetres.</param>
/// <param name="MaxZ">Maximum Z coordinate in millimetres.</param>
public sealed record RegisterSpatialElementCommand(
    Guid PhysicalSpaceId,
    Guid FlowEpicId,
    TradeType TradeType,
    string ElementType,
    int MinX, int MinY, int MinZ,
    int MaxX, int MaxY, int MaxZ) : IRequest<Result<Guid>>;
