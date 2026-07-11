using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.WorkStations.Commands;

/// <summary>
/// Command to reassign an existing workstation to a different facility.
/// </summary>
/// <param name="WorkStationId">The workstation identifier.</param>
/// <param name="NewFacilityId">The target facility identifier.</param>
public record AssignWorkStationToFacilityCommand(Guid WorkStationId, Guid NewFacilityId) : IRequest<Result>;
