// SpaceOS.Kernel.Application/FlowEpics/Commands/ArchiveFlowEpicCommand.cs
using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.FlowEpics.Commands;

/// <summary>Archives a <see cref="Domain.Entities.FlowEpic"/> by setting its <c>IsArchived</c> flag.</summary>
/// <param name="Id">The unique identifier of the flow epic to archive.</param>
public sealed record ArchiveFlowEpicCommand(Guid Id) : IRequest<Result>;
