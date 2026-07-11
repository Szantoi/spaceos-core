using MediatR;
using Ardalis.Result;
using SpaceOS.Kernel.Domain.Enums;

namespace SpaceOS.Kernel.Application.WorkStations.Commands;

/// <summary>
/// Command to update the operational status of an existing workstation.
/// </summary>
/// <param name="Id">The identifier of the workstation to update.</param>
/// <param name="NewStatus">The desired new status.</param>
public record UpdateWorkStationStatusCommand(Guid Id, WorkStationStatus NewStatus) : IRequest<Result>;
