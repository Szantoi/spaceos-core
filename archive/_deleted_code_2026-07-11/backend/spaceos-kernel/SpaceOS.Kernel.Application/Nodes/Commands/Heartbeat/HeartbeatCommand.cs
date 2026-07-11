// SpaceOS.Kernel.Application/Nodes/Commands/Heartbeat/HeartbeatCommand.cs
using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.Nodes.Commands.Heartbeat;

/// <summary>
/// Records a heartbeat for the node registered under the given tenant,
/// updating <c>LastHeartbeatAt</c> on the <see cref="SpaceOS.Kernel.Domain.Federation.NodeManifest"/>.
/// </summary>
/// <param name="TenantId">The identifier of the tenant whose node is heartbeating.</param>
public sealed record HeartbeatCommand(Guid TenantId) : IRequest<Result>;
