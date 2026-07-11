// SpaceOS.Infrastructure/Persistence/ConnectionStringOptions.cs

using System.ComponentModel.DataAnnotations;

namespace SpaceOS.Infrastructure.Persistence;

/// <summary>
/// Strongly-typed representation of the <c>ConnectionStrings</c> configuration section.
/// Validated at startup via <c>ValidateOnStart()</c> so a misconfigured connection string
/// causes immediate application startup failure rather than a runtime exception on first use.
/// </summary>
/// <remarks>
/// <para>
/// <b>Production requirements:</b>
/// <list type="bullet">
///   <item><c>DefaultConnection</c> — main application database (<c>spaceos_app</c> role)</item>
///   <item><c>AuditWriter</c> — audit-event database (<c>spaceos_audit_writer</c> role, INSERT+SELECT on AuditEvents)</item>
///   <item><c>AuditSink</c> — hash sink database (<c>spaceos_sink_writer</c> role, INSERT on hash_chain_records)</item>
/// </list>
/// </para>
/// <para>
/// <b>Development:</b> <c>AuditWriter</c> and <c>AuditSink</c> may be absent; the Infrastructure
/// DI registration falls back to the main SQLite connection string in the Development environment.
/// The <c>[Required]</c> constraint on <c>DefaultConnection</c> applies in all environments.
/// </para>
/// </remarks>
internal sealed class ConnectionStringOptions
{
    /// <summary>
    /// Gets or sets the connection string for the main application database.
    /// Required in all environments.
    /// </summary>
    [Required]
    public string DefaultConnection { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the connection string for the audit-writer role on the AuditEvents table.
    /// Optional in Development — falls back to <c>DefaultConnection</c>.
    /// </summary>
    public string? AuditWriter { get; set; }

    /// <summary>
    /// Gets or sets the connection string for the <c>spaceos_audit_sink</c> database
    /// used by <c>HashSinkDbContext</c> and the <c>spaceos_sink_writer</c> PostgreSQL role.
    /// Optional in Development — the hash sink is a no-op in that environment.
    /// </summary>
    public string? AuditSink { get; set; }
}
