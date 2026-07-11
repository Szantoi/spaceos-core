// SpaceOS.Kernel.Application/SpaceLayers/Commands/ArchiveSpaceLayerCommand.cs
using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.SpaceLayers.Commands;

/// <summary>Archives a <see cref="Domain.Entities.SpaceLayer"/> by setting its <c>IsArchived</c> flag.</summary>
/// <param name="Id">The unique identifier of the space layer to archive.</param>
public sealed record ArchiveSpaceLayerCommand(Guid Id) : IRequest<Result>;
