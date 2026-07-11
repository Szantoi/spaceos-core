using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.WorkStations.Commands;

/// <summary>
/// Command to update an existing workstation's display name.
/// </summary>
/// <param name="WorkStationId">The workstation identifier.</param>
/// <param name="NewName">The new display name.</param>
public record UpdateWorkStationNameCommand(Guid WorkStationId, string NewName) : IRequest<Result>;
