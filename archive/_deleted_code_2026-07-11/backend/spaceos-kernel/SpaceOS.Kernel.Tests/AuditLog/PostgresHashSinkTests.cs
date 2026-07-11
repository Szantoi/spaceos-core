// SpaceOS.Kernel.Tests/AuditLog/PostgresHashSinkTests.cs

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SpaceOS.Infrastructure.AuditLog;
using SpaceOS.Infrastructure.Persistence;
using Xunit;

namespace SpaceOS.Kernel.Tests.AuditLog;

/// <summary>
/// Unit tests for <see cref="PostgresHashSink"/>.
/// Verifies that sink failures are caught and not rethrown,
/// and that constructor guards are enforced.
/// </summary>
public sealed class PostgresHashSinkTests
{
    // -------------------------------------------------------------------------
    // WriteAsync_WhenFactoryThrows_DoesNotRethrow
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a factory exception (e.g., DB unavailable) is swallowed —
    /// the primary audit write path must not be interrupted by a sink outage.
    /// </summary>
    [Fact]
    public async Task WriteAsync_WhenFactoryThrows_DoesNotRethrow()
    {
        // Arrange — factory throws simulating DB unavailability
        var factory = new Mock<IDbContextFactory<HashSinkDbContext>>();
        factory
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Connection refused"));

        var sink = new PostgresHashSink(factory.Object, NullLogger<PostgresHashSink>.Instance);

        // Act + Assert — must not throw
        var exception = await Record.ExceptionAsync(() =>
            sink.WriteAsync(
                Guid.NewGuid(),
                "TestEvent",
                new string('a', 64),
                new string('b', 64),
                DateTimeOffset.UtcNow,
                TestContext.Current.CancellationToken));

        Assert.Null(exception);
    }

    // -------------------------------------------------------------------------
    // ReadHashesAsync_WhenFactoryThrows_ReturnsEmptyList
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a factory exception during read returns an empty list rather than
    /// throwing — a sink outage must not break the verify-chain query.
    /// </summary>
    [Fact]
    public async Task ReadHashesAsync_WhenFactoryThrows_ReturnsEmptyList()
    {
        // Arrange
        var factory = new Mock<IDbContextFactory<HashSinkDbContext>>();
        factory
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Sink DB unreachable"));

        var sink = new PostgresHashSink(factory.Object, NullLogger<PostgresHashSink>.Instance);

        // Act
        var result = await sink.ReadHashesAsync(
            Guid.NewGuid(),
            from: null,
            to: null,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result);
    }

    // -------------------------------------------------------------------------
    // Constructor_NullFactory_ThrowsArgumentNullException
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="PostgresHashSink"/> enforces a non-null factory at construction.
    /// </summary>
    [Fact]
    public void Constructor_NullFactory_ThrowsArgumentNullException()
    {
        // Act + Assert
        Assert.Throws<ArgumentNullException>(() =>
            new PostgresHashSink(null!, NullLogger<PostgresHashSink>.Instance));
    }

    // -------------------------------------------------------------------------
    // Constructor_NullLogger_ThrowsArgumentNullException
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="PostgresHashSink"/> enforces a non-null logger at construction.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var factory = new Mock<IDbContextFactory<HashSinkDbContext>>();

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() =>
            new PostgresHashSink(factory.Object, null!));
    }
}
