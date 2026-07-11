// SpaceOS.Kernel.Application/Sync/Commands/ReceiveSignal/ReceiveSyncSignalCommand.cs
using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.Sync.Commands.ReceiveSignal;

/// <summary>
/// Appends a new state-change signal to the tenant's sync-signal hash chain.
/// Idempotent: a duplicate <paramref name="ClientSignalId"/> returns <see cref="Result.Success"/> without re-inserting.
/// </summary>
/// <param name="TenantId">The identifier of the tenant emitting the signal.</param>
/// <param name="EpicId">The flow epic this signal targets.</param>
/// <param name="NewState">The target workflow phase encoded as a string.</param>
/// <param name="ClientSignalId">Client-generated UUID used for idempotent delivery.</param>
/// <param name="PayloadJson">The JSON-serialised payload accompanying the signal.</param>
public sealed record ReceiveSyncSignalCommand(
    Guid TenantId,
    Guid EpicId,
    string NewState,
    Guid ClientSignalId,
    string PayloadJson)
    : IRequest<Result>;
