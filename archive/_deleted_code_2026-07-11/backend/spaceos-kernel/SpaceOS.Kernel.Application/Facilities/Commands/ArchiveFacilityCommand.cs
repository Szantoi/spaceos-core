// SpaceOS.Kernel.Application/Facilities/Commands/ArchiveFacilityCommand.cs
using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.Facilities.Commands;

/// <summary>Archives a <see cref="Domain.Entities.Facility"/> by setting its <c>IsArchived</c> flag.</summary>
/// <param name="Id">The unique identifier of the facility to archive.</param>
public sealed record ArchiveFacilityCommand(Guid Id) : IRequest<Result>;
