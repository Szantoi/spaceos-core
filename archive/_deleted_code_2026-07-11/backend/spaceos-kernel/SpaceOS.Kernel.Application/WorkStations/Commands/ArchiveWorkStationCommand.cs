// SpaceOS.Kernel.Application/WorkStations/Commands/ArchiveWorkStationCommand.cs
using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.WorkStations.Commands;

/// <summary>Archives a <see cref="Domain.Entities.WorkStation"/> by setting its <c>IsArchived</c> flag.</summary>
/// <param name="Id">The unique identifier of the workstation to archive.</param>
public sealed record ArchiveWorkStationCommand(Guid Id) : IRequest<Result>;
