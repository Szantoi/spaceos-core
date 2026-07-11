// SpaceOS.Kernel.Application/AuditLog/Queries/VerifyChainQuery.cs

using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.AuditLog.Queries;

/// <summary>
/// Result DTO returned by the chain verification endpoint.
/// </summary>
/// <param name="IsValid">Whether the full chain is intact (no broken links found).</param>
/// <param name="TotalRecordsChecked">The number of audit records inspected during verification.</param>
/// <param name="FirstBrokenAt">The timestamp of the first event where the chain breaks, or <see langword="null"/> if the chain is intact.</param>
/// <param name="ExternalSinkMatch">Whether the state hashes in the primary DB match those in the external sink.</param>
/// <param name="WormStorageAvailable">
/// SEC-P3B-05: Whether the WORM proof storage backend is currently reachable.
/// A value of <see langword="false"/> means cross-validation against WORM storage was skipped —
/// the endpoint still returns HTTP 200 rather than 500.
/// </param>
/// <param name="DiagnosticMessage">Optional human-readable diagnostic message, or <see langword="null"/>.</param>
public sealed record ChainVerificationResultDto(
    bool IsValid,
    int TotalRecordsChecked,
    DateTimeOffset? FirstBrokenAt,
    bool ExternalSinkMatch,
    bool WormStorageAvailable = true,
    string? DiagnosticMessage = null);

/// <summary>
/// Verifies the integrity of the audit event chain for a tenant over the specified date range.
/// </summary>
/// <param name="TenantId">The tenant whose chain to verify.</param>
/// <param name="From">Inclusive lower bound on the audit events to check; <see langword="null"/> means from the beginning.</param>
/// <param name="To">Inclusive upper bound on the audit events to check; <see langword="null"/> means through the latest.</param>
public sealed record VerifyChainQuery(
    Guid TenantId,
    DateTimeOffset? From,
    DateTimeOffset? To) : IRequest<Result<ChainVerificationResultDto>>;
