using FluentAssertions;
using SpaceOS.Modules.Joinery.Domain.Core;

namespace SpaceOS.Modules.Joinery.Tests.Domain;

/// <summary>
/// Domain tests for GyartasilapBatch aggregate — creation and status transitions.
/// </summary>
public class GyartasilapBatchTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly IReadOnlyList<Guid> ValidIds =
        new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

    [Fact]
    public void Create_WithValidInputs_ReturnsPendingBatch()
    {
        var result = GyartasilapBatch.Create(OrderId, TenantId, ValidIds);

        result.IsSuccess.Should().BeTrue();
        var batch = result.Value;
        batch.OrderId.Should().Be(OrderId);
        batch.TenantId.Should().Be(TenantId);
        batch.Status.Should().Be(BatchStatus.Pending);
        batch.GyartasilapIds.Should().BeEquivalentTo(ValidIds);
        batch.ZipStoragePath.Should().BeNull();
        batch.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void Create_WithEmptyIds_ReturnsInvalidResult()
    {
        var result = GyartasilapBatch.Create(OrderId, TenantId, new List<Guid>());

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "GyartasilapIds");
    }

    [Fact]
    public void Create_WithNullIds_ReturnsInvalidResult()
    {
        var result = GyartasilapBatch.Create(OrderId, TenantId, null!);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "GyartasilapIds");
    }

    [Fact]
    public void Create_WithEmptyOrderId_ReturnsInvalidResult()
    {
        var result = GyartasilapBatch.Create(Guid.Empty, TenantId, ValidIds);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "OrderId");
    }

    [Fact]
    public void MarkGenerating_FromPending_SetsGeneratingStatus()
    {
        var batch = GyartasilapBatch.Create(OrderId, TenantId, ValidIds).Value;

        var result = batch.MarkGenerating();

        result.IsSuccess.Should().BeTrue();
        batch.Status.Should().Be(BatchStatus.Generating);
    }

    [Fact]
    public void MarkReady_FromGenerating_SetsReadyStatusAndPath()
    {
        const string zipPath = "tenant/batch/batch.zip";
        var batch = GyartasilapBatch.Create(OrderId, TenantId, ValidIds).Value;
        batch.MarkGenerating();

        var result = batch.MarkReady(zipPath);

        result.IsSuccess.Should().BeTrue();
        batch.Status.Should().Be(BatchStatus.Ready);
        batch.ZipStoragePath.Should().Be(zipPath);
        batch.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkReady_WhenAlreadyReady_ReturnsFail()
    {
        var batch = GyartasilapBatch.Create(OrderId, TenantId, ValidIds).Value;
        batch.MarkGenerating();
        batch.MarkReady("some/path.zip");

        var result = batch.MarkReady("another/path.zip");

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "Status");
    }

    [Fact]
    public void MarkFailed_FromGenerating_SetsFailedStatus()
    {
        var batch = GyartasilapBatch.Create(OrderId, TenantId, ValidIds).Value;
        batch.MarkGenerating();

        var result = batch.MarkFailed();

        result.IsSuccess.Should().BeTrue();
        batch.Status.Should().Be(BatchStatus.Failed);
        batch.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkFailed_WhenAlreadyFailed_ReturnsFail()
    {
        var batch = GyartasilapBatch.Create(OrderId, TenantId, ValidIds).Value;
        batch.MarkFailed();

        var result = batch.MarkFailed();

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "Status");
    }

    [Fact]
    public void MarkReady_WithEmptyPath_ReturnsFail()
    {
        var batch = GyartasilapBatch.Create(OrderId, TenantId, ValidIds).Value;
        batch.MarkGenerating();

        var result = batch.MarkReady(string.Empty);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "ZipStoragePath");
    }

    [Fact]
    public void Create_SetsCreatedAtToUtcNow()
    {
        var before = DateTimeOffset.UtcNow;
        var batch = GyartasilapBatch.Create(OrderId, TenantId, ValidIds).Value;
        var after = DateTimeOffset.UtcNow;

        batch.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }
}
