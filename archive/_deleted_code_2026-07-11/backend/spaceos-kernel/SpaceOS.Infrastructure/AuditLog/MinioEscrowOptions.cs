// SpaceOS.Infrastructure/AuditLog/MinioEscrowOptions.cs

namespace SpaceOS.Infrastructure.AuditLog;

/// <summary>
/// Configuration options for the MinIO WORM audit escrow writer.
/// Bound from the <c>MinioEscrow</c> configuration section.
/// </summary>
public sealed class MinioEscrowOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "MinioEscrow";

    /// <summary>Gets or sets the MinIO server endpoint (e.g. <c>http://127.0.0.1:9000</c>).</summary>
    public string Endpoint { get; set; } = "http://127.0.0.1:9000";

    /// <summary>Gets or sets the MinIO access key (username).</summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MinIO secret key (password).
    /// Loaded from <c>MinioEscrow__SecretKey</c> environment variable in production.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the target bucket name for WORM audit objects.</summary>
    public string BucketName { get; set; } = "spaceos-audit-worm";

    /// <summary>
    /// Gets or sets whether the escrow writer is active.
    /// Defaults to <see langword="false"/> — must be explicitly enabled with valid credentials.
    /// Set to <see langword="false"/> to disable MinIO writes without removing configuration.
    /// </summary>
    public bool Enabled { get; set; } = false;
}
