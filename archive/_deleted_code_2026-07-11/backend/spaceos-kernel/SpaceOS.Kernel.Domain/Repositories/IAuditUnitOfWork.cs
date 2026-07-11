// SpaceOS.Kernel.Domain/Repositories/IAuditUnitOfWork.cs

namespace SpaceOS.Kernel.Domain.Repositories;

/// <summary>
/// Unit-of-work marker for the audit-log write path.
/// Implementations commit changes on <see cref="AuditDbContext"/> rather than the main
/// <see cref="AppDbContext"/>, so the audit connection string and DB role are fully separated.
/// </summary>
public interface IAuditUnitOfWork : IUnitOfWork
{
}
