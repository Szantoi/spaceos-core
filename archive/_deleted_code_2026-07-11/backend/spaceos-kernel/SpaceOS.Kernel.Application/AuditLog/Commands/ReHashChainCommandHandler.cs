// SpaceOS.Kernel.Application/AuditLog/Commands/ReHashChainCommandHandler.cs

using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Domain.AuditLog;

namespace SpaceOS.Kernel.Application.AuditLog.Commands;

/// <summary>
/// Handles <see cref="ReHashChainCommand"/>: walks the audit event chain for the specified
/// tenant and counts how many records would need to be re-hashed with the target algorithm.
/// This is a read-only dry-run — no records are modified.
/// The actual chain migration requires a coordinated, manually reviewed database operation.
/// </summary>
internal sealed class ReHashChainCommandHandler
    : IRequestHandler<ReHashChainCommand, Result<ReHashResultDto>>
{
    private readonly IAuditEventRepository _repository;

    /// <summary>Initialises a new <see cref="ReHashChainCommandHandler"/>.</summary>
    /// <param name="repository">The audit event repository used to read the chain.</param>
    public ReHashChainCommandHandler(IAuditEventRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    /// <summary>
    /// Counts the audit records that use an algorithm different from <see cref="ReHashChainCommand.TargetAlgorithm"/>
    /// and returns the dry-run result without modifying any data.
    /// </summary>
    public async Task<Result<ReHashResultDto>> Handle(ReHashChainCommand command, CancellationToken ct)
    {
        var events = await _repository
            .GetChainAsync(command.TenantId, from: null, to: null, ct)
            .ConfigureAwait(false);

        var affected = events.Count(e => e.HashAlgorithm != command.TargetAlgorithm);

        return Result.Success(new ReHashResultDto(affected, command.TargetAlgorithm));
    }
}
