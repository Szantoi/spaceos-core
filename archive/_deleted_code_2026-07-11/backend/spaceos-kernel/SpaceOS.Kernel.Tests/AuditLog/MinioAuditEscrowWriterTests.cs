// SpaceOS.Kernel.Tests/AuditLog/MinioAuditEscrowWriterTests.cs

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SpaceOS.Infrastructure.AuditLog;
using SpaceOS.Kernel.Domain.AuditLog;
using SpaceOS.Kernel.Domain.Primitives;
using Xunit;

namespace SpaceOS.Kernel.Tests.AuditLog;

/// <summary>
/// Unit tests for <see cref="MinioAuditEscrowWriter"/>.
/// Verifies object-key format, idempotency, metadata, fire-and-forget safety,
/// and constructor guards.
/// </summary>
public sealed class MinioAuditEscrowWriterTests
{
    private static readonly Guid SampleTenantId    = Guid.Parse("79d71e39-5571-43d8-9f07-5f7631afa5e7");
    private static readonly Guid SampleAggregateId = Guid.NewGuid();
    private const string         SampleStateHash   = "a591a6d40bf420404a011733cfb7b190d62c65bf0bcda32b57b277d9ad9f146e";
    private const string         SampleBucketName  = "spaceos-audit-worm";

    private static MinioEscrowOptions EnabledOptions => new()
    {
        BucketName = SampleBucketName,
        Enabled    = true,
        Endpoint   = "http://127.0.0.1:9000",
        AccessKey  = "test",
        SecretKey  = "test1234",
    };

    private static AuditEvent MakeAuditEvent(DateTimeOffset? occurredAt = null)
    {
        var ae = AuditEvent.Create(
            tenantId:    SampleTenantId,
            eventType:   "TenantCreatedEvent",
            aggregateId: SampleAggregateId,
            payload:     """{"tenantId":"test"}""",
            stateHash:   SampleStateHash);

        // OccurredAt is set by AuditEvent.Create to UtcNow — we test BuildObjectKey directly for date assertions.
        return ae;
    }

    private static MinioAuditEscrowWriter MakeWriter(
        IMinioStorage storage,
        MinioEscrowOptions? options = null)
    {
        return new MinioAuditEscrowWriter(
            storage,
            Options.Create(options ?? EnabledOptions),
            NullLogger<MinioAuditEscrowWriter>.Instance);
    }

    // -------------------------------------------------------------------------
    // BuildObjectKey tests
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildObjectKey_ReturnsCorrectFormat()
    {
        // Arrange
        var ae = AuditEvent.Create(
            tenantId:    SampleTenantId,
            eventType:   "TenantCreatedEvent",
            aggregateId: SampleAggregateId,
            payload:     "{}",
            stateHash:   SampleStateHash);

        // Act
        var key = MinioAuditEscrowWriter.BuildObjectKey(ae);

        // Assert — {tenantId}/{year}/{month:D2}/{eventId}.json
        var parts = key.Split('/');
        Assert.Equal(4, parts.Length);
        Assert.Equal(SampleTenantId.ToString(), parts[0]);
        Assert.Equal(ae.OccurredAt.Year.ToString(), parts[1]);
        Assert.Equal(ae.OccurredAt.Month.ToString("D2"), parts[2]);
        Assert.Equal($"{ae.Id}.json", parts[3]);
    }

    [Fact]
    public void BuildObjectKey_Month_PadsTwoDigits()
    {
        // Create an event and forcefully use a month that would be single-digit.
        // We can only test indirectly via an event created at a specific month.
        // Using the known format: the month segment must always be exactly 2 chars.
        var ae = MakeAuditEvent();
        var key = MinioAuditEscrowWriter.BuildObjectKey(ae);
        var monthPart = key.Split('/')[2];
        Assert.Equal(2, monthPart.Length);
        Assert.True(int.TryParse(monthPart, out var month) && month is >= 1 and <= 12);
    }

    // -------------------------------------------------------------------------
    // WriteAsync — disabled
    // -------------------------------------------------------------------------

    [Fact]
    public async Task WriteAsync_WhenDisabled_SkipsStorageCompletely()
    {
        // Arrange
        var storage = new Mock<IMinioStorage>(MockBehavior.Strict); // strict → fails on any call
        var options = EnabledOptions;
        options.Enabled = false;
        var writer = MakeWriter(storage.Object, options);

        // Act — must not throw even though storage is strict and never set up
        await writer.WriteAsync(MakeAuditEvent(), TestContext.Current.CancellationToken);

        // Assert — no calls at all
        storage.VerifyNoOtherCalls();
    }

    // -------------------------------------------------------------------------
    // WriteAsync — idempotency
    // -------------------------------------------------------------------------

    [Fact]
    public async Task WriteAsync_WhenObjectAlreadyExists_SkipsPutObject()
    {
        // Arrange
        var storage = new Mock<IMinioStorage>();
        storage
            .Setup(s => s.ObjectExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var writer = MakeWriter(storage.Object);

        // Act
        await writer.WriteAsync(MakeAuditEvent(), TestContext.Current.CancellationToken);

        // Assert — PutObjectAsync never called
        storage.Verify(
            s => s.PutObjectAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(),
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // -------------------------------------------------------------------------
    // WriteAsync — happy path
    // -------------------------------------------------------------------------

    [Fact]
    public async Task WriteAsync_WhenObjectDoesNotExist_CallsPutObjectOnce()
    {
        // Arrange
        var storage = new Mock<IMinioStorage>();
        storage
            .Setup(s => s.ObjectExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var writer = MakeWriter(storage.Object);

        // Act
        await writer.WriteAsync(MakeAuditEvent(), TestContext.Current.CancellationToken);

        // Assert
        storage.Verify(
            s => s.PutObjectAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(),
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task WriteAsync_CorrectBucketName_PassedToPutObject()
    {
        // Arrange
        var storage = new Mock<IMinioStorage>();
        storage
            .Setup(s => s.ObjectExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        string? capturedBucket = null;
        storage
            .Setup(s => s.PutObjectAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(),
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, Stream, long, string, Dictionary<string, string>, CancellationToken>(
                (bucket, _, _, _, _, _, _) => capturedBucket = bucket)
            .Returns(Task.CompletedTask);
        var writer = MakeWriter(storage.Object);

        // Act
        await writer.WriteAsync(MakeAuditEvent(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(SampleBucketName, capturedBucket);
    }

    [Fact]
    public async Task WriteAsync_CorrectObjectKey_PassedToPutObject()
    {
        // Arrange
        var ae = MakeAuditEvent();
        var storage = new Mock<IMinioStorage>();
        storage
            .Setup(s => s.ObjectExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        string? capturedKey = null;
        storage
            .Setup(s => s.PutObjectAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(),
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, Stream, long, string, Dictionary<string, string>, CancellationToken>(
                (_, key, _, _, _, _, _) => capturedKey = key)
            .Returns(Task.CompletedTask);
        var writer = MakeWriter(storage.Object);

        // Act
        await writer.WriteAsync(ae, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(MinioAuditEscrowWriter.BuildObjectKey(ae), capturedKey);
    }

    [Fact]
    public async Task WriteAsync_ContentType_IsApplicationJson()
    {
        // Arrange
        var storage = new Mock<IMinioStorage>();
        storage
            .Setup(s => s.ObjectExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        string? capturedContentType = null;
        storage
            .Setup(s => s.PutObjectAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(),
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, Stream, long, string, Dictionary<string, string>, CancellationToken>(
                (_, _, _, _, ct, _, _) => capturedContentType = ct)
            .Returns(Task.CompletedTask);
        var writer = MakeWriter(storage.Object);

        // Act
        await writer.WriteAsync(MakeAuditEvent(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("application/json", capturedContentType);
    }

    // -------------------------------------------------------------------------
    // WriteAsync — metadata
    // -------------------------------------------------------------------------

    [Fact]
    public async Task WriteAsync_MetadataContainsStateHash()
    {
        // Arrange
        var ae = MakeAuditEvent();
        var storage = new Mock<IMinioStorage>();
        storage
            .Setup(s => s.ObjectExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        Dictionary<string, string>? capturedMeta = null;
        storage
            .Setup(s => s.PutObjectAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(),
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, Stream, long, string, Dictionary<string, string>, CancellationToken>(
                (_, _, _, _, _, meta, _) => capturedMeta = meta)
            .Returns(Task.CompletedTask);
        var writer = MakeWriter(storage.Object);

        // Act
        await writer.WriteAsync(ae, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(capturedMeta);
        Assert.True(capturedMeta.TryGetValue("x-amz-meta-audit-chain-hash", out var hash));
        Assert.Equal(ae.StateHash, hash);
    }

    [Fact]
    public async Task WriteAsync_MetadataContainsTenantId()
    {
        // Arrange
        var ae = MakeAuditEvent();
        var storage = new Mock<IMinioStorage>();
        storage
            .Setup(s => s.ObjectExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        Dictionary<string, string>? capturedMeta = null;
        storage
            .Setup(s => s.PutObjectAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(),
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, Stream, long, string, Dictionary<string, string>, CancellationToken>(
                (_, _, _, _, _, meta, _) => capturedMeta = meta)
            .Returns(Task.CompletedTask);
        var writer = MakeWriter(storage.Object);

        // Act
        await writer.WriteAsync(ae, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(capturedMeta);
        Assert.True(capturedMeta.TryGetValue("x-amz-meta-tenant-id", out var tenantId));
        Assert.Equal(ae.TenantId.ToString(), tenantId);
    }

    [Fact]
    public async Task WriteAsync_MetadataContainsCreatedAt_Iso8601()
    {
        // Arrange
        var ae = MakeAuditEvent();
        var storage = new Mock<IMinioStorage>();
        storage
            .Setup(s => s.ObjectExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        Dictionary<string, string>? capturedMeta = null;
        storage
            .Setup(s => s.PutObjectAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(),
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, Stream, long, string, Dictionary<string, string>, CancellationToken>(
                (_, _, _, _, _, meta, _) => capturedMeta = meta)
            .Returns(Task.CompletedTask);
        var writer = MakeWriter(storage.Object);

        // Act
        await writer.WriteAsync(ae, TestContext.Current.CancellationToken);

        // Assert — value parses as DateTimeOffset and equals OccurredAt rounded-trip
        Assert.NotNull(capturedMeta);
        Assert.True(capturedMeta.TryGetValue("x-amz-meta-created-at", out var createdAt));
        Assert.True(DateTimeOffset.TryParse(createdAt, out var parsed));
        Assert.Equal(ae.OccurredAt, parsed);
    }

    // -------------------------------------------------------------------------
    // WriteAsync — fire-and-forget safety (no rethrow)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task WriteAsync_WhenPutObjectThrows_DoesNotRethrow()
    {
        // Arrange
        var storage = new Mock<IMinioStorage>();
        storage
            .Setup(s => s.ObjectExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        storage
            .Setup(s => s.PutObjectAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(),
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("MinIO unreachable"));
        var writer = MakeWriter(storage.Object);

        // Act & Assert — must not throw
        var ex = await Record.ExceptionAsync(() =>
            writer.WriteAsync(MakeAuditEvent(), TestContext.Current.CancellationToken));
        Assert.Null(ex);
    }

    [Fact]
    public async Task WriteAsync_WhenExistsCheckThrows_DoesNotRethrow()
    {
        // Arrange
        var storage = new Mock<IMinioStorage>();
        storage
            .Setup(s => s.ObjectExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("MinIO timeout"));
        var writer = MakeWriter(storage.Object);

        // Act & Assert — must not throw
        var ex = await Record.ExceptionAsync(() =>
            writer.WriteAsync(MakeAuditEvent(), TestContext.Current.CancellationToken));
        Assert.Null(ex);
    }

    // -------------------------------------------------------------------------
    // Constructor guards
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_NullStorage_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new MinioAuditEscrowWriter(
                null!,
                Options.Create(EnabledOptions),
                NullLogger<MinioAuditEscrowWriter>.Instance));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new MinioAuditEscrowWriter(
                new Mock<IMinioStorage>().Object,
                null!,
                NullLogger<MinioAuditEscrowWriter>.Instance));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new MinioAuditEscrowWriter(
                new Mock<IMinioStorage>().Object,
                Options.Create(EnabledOptions),
                null!));
    }
}
