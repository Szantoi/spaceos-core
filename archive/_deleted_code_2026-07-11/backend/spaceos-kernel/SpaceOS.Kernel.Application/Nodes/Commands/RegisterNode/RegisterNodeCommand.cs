// SpaceOS.Kernel.Application/Nodes/Commands/RegisterNode/RegisterNodeCommand.cs
using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.Nodes.Commands.RegisterNode;

/// <summary>
/// Registers a new remote SpaceOS node for the given tenant, issues a node JWT,
/// and returns the persisted <see cref="NodeManifestDto"/> with the JWT included.
/// </summary>
/// <param name="TenantId">The identifier of the owning tenant.</param>
/// <param name="ServerUrl">The public base URL of the remote node.</param>
public sealed record RegisterNodeCommand(Guid TenantId, string ServerUrl)
    : IRequest<Result<NodeManifestDto>>;
