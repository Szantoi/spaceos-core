using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.FlowEpics.Commands.DelegateFlowEpic;

/// <summary>
/// Command to delegate a <see cref="SpaceOS.Kernel.Domain.Entities.FlowEpic"/> to a guest tenant,
/// creating a B2B handshake and raising a domain event.
/// </summary>
/// <param name="EpicId">The identifier of the epic to delegate.</param>
/// <param name="GuestTenantId">The identifier of the guest tenant receiving the delegation.</param>
public record DelegateFlowEpicCommand(Guid EpicId, Guid GuestTenantId) : IRequest<Result>;
