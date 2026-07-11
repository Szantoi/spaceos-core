// SpaceOS.Kernel.Tests/Infrastructure/ConnectionStringOptionsTests.cs

using System.ComponentModel.DataAnnotations;
using SpaceOS.Infrastructure.Persistence;
using Xunit;

namespace SpaceOS.Kernel.Tests.Infrastructure;

/// <summary>
/// Unit tests for <see cref="ConnectionStringOptions"/> data-annotation validation.
/// Verifies that the <c>[Required]</c> constraint on <c>DefaultConnection</c> catches
/// misconfigured startup before the first database call.
/// </summary>
public sealed class ConnectionStringOptionsTests
{
    // -------------------------------------------------------------------------
    // Validate_DefaultConnectionPresent_PassesValidation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a fully populated <see cref="ConnectionStringOptions"/> passes validation.
    /// </summary>
    [Fact]
    public void Validate_DefaultConnectionPresent_PassesValidation()
    {
        // Arrange
        var options = new ConnectionStringOptions
        {
            DefaultConnection = "Host=localhost;Database=spaceos",
            AuditWriter       = "Host=localhost;Database=spaceos;Username=spaceos_audit_writer",
            AuditSink         = "Host=localhost;Database=spaceos_audit_sink;Username=spaceos_sink_writer",
        };

        // Act
        var results = ValidateOptions(options);

        // Assert
        Assert.Empty(results);
    }

    // -------------------------------------------------------------------------
    // Validate_MissingDefaultConnection_FailsValidation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that omitting <c>DefaultConnection</c> produces a validation error.
    /// </summary>
    [Fact]
    public void Validate_MissingDefaultConnection_FailsValidation()
    {
        // Arrange
        var options = new ConnectionStringOptions
        {
            DefaultConnection = string.Empty,
        };

        // Act
        var results = ValidateOptions(options);

        // Assert
        Assert.Contains(results, r =>
            r.MemberNames.Contains(nameof(ConnectionStringOptions.DefaultConnection)));
    }

    // -------------------------------------------------------------------------
    // Validate_NullDefaultConnection_FailsValidation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a null <c>DefaultConnection</c> (unbound from config) also fails validation.
    /// </summary>
    [Fact]
    public void Validate_NullDefaultConnection_FailsValidation()
    {
        // Arrange — simulate a missing config key binding to null
        var options = new ConnectionStringOptions
        {
            DefaultConnection = null!,
        };

        // Act
        var results = ValidateOptions(options);

        // Assert
        Assert.Contains(results, r =>
            r.MemberNames.Contains(nameof(ConnectionStringOptions.DefaultConnection)));
    }

    // -------------------------------------------------------------------------
    // Validate_AuditWriterAndAuditSinkOptional_PassesValidation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <c>AuditWriter</c> and <c>AuditSink</c> are optional —
    /// they are absent in Development where SQLite is used.
    /// </summary>
    [Fact]
    public void Validate_AuditWriterAndAuditSinkOptional_PassesValidation()
    {
        // Arrange — only DefaultConnection set; AuditWriter and AuditSink null
        var options = new ConnectionStringOptions
        {
            DefaultConnection = "Data Source=SpaceOS.dev.db",
            AuditWriter       = null,
            AuditSink         = null,
        };

        // Act
        var results = ValidateOptions(options);

        // Assert — no validation errors
        Assert.Empty(results);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static IReadOnlyList<ValidationResult> ValidateOptions(ConnectionStringOptions options)
    {
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, context, results, validateAllProperties: true);
        return results.AsReadOnly();
    }
}
