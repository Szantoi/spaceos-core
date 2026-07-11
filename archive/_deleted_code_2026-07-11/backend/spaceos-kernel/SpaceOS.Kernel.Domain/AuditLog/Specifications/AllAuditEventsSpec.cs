// SpaceOS.Kernel.Domain/AuditLog/Specifications/AllAuditEventsSpec.cs

using Ardalis.Specification;

namespace SpaceOS.Kernel.Domain.AuditLog.Specifications;

/// <summary>Returns all <see cref="AuditEvent"/> records with no filter applied.</summary>
public sealed class AllAuditEventsSpec : Specification<AuditEvent>;
