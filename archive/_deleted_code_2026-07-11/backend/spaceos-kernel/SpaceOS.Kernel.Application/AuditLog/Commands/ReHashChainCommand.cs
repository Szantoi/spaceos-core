// SpaceOS.Kernel.Application/AuditLog/Commands/ReHashChainCommand.cs

using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Domain.AuditLog;

namespace SpaceOS.Kernel.Application.AuditLog.Commands;

/// <summary>
/// Command that performs a dry-run analysis of re-hashing a tenant's audit event chain
/// with a new cryptographic algorithm.
/// Returns how many records would be affected without modifying any data.
/// The actual chain migration is a destructive, out-of-band operation that must be
/// reviewed and executed manually in a future sprint.
/// </summary>
/// <param name="TenantId">The tenant whose audit chain to analyse.</param>
/// <param name="TargetAlgorithm">The target hash algorithm for the migration.</param>
public sealed record ReHashChainCommand(
    Guid TenantId,
    HashAlgorithmType TargetAlgorithm) : IRequest<Result<ReHashResultDto>>;

/// <summary>Dry-run result returned by <see cref="ReHashChainCommand"/>.</summary>
/// <param name="RecordsAffected">Number of audit records that would need to be re-hashed.</param>
/// <param name="NewAlgorithm">The algorithm that would be applied.</param>
public sealed record ReHashResultDto(int RecordsAffected, HashAlgorithmType NewAlgorithm);
