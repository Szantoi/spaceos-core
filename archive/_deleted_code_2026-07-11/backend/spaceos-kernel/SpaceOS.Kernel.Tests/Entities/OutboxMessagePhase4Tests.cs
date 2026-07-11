// SpaceOS.Kernel.Tests/Entities/OutboxMessagePhase4Tests.cs

using SpaceOS.Kernel.Domain.Outbox;
using Xunit;

namespace SpaceOS.Kernel.Tests.Entities;

/// <summary>
/// Unit tests for Phase 4 extension fields on <see cref="OutboxMessage"/>.
/// </summary>
public sealed class OutboxMessagePhase4Tests
{
    private static readonly Guid SomeTenantId = Guid.NewGuid();

    // ── 1. New fields are set on the simple factory ──────────────────────────

    [Fact]
    public void Create_SimpleOverload_StatusIsPending()
    {
        var message = OutboxMessage.Create("EscrowTrigger", "{}", SomeTenantId);

        Assert.Equal(OutboxStatus.Pending, message.Status);
    }

    [Fact]
    public void Create_SimpleOverload_AttemptsIsZero()
    {
        var message = OutboxMessage.Create("EscrowTrigger", "{}", SomeTenantId);

        Assert.Equal(0, message.Attempts);
    }

    [Fact]
    public void Create_SimpleOverload_BatchFieldsAreNull()
    {
        var message = OutboxMessage.Create("EscrowTrigger", "{}", SomeTenantId);

        Assert.Null(message.BatchId);
        Assert.Null(message.BatchSequenceNumber);
        Assert.Null(message.AggregateId);
        Assert.Null(message.AggregateType);
        Assert.Null(message.EventType);
        Assert.Null(message.LastError);
    }

    // ── 2. Batch overload sets all Phase 4 fields ────────────────────────────

    [Fact]
    public void Create_BatchOverload_SetsBatchFields()
    {
        var batchId       = Guid.NewGuid();
        var aggregateId   = Guid.NewGuid();

        var message = OutboxMessage.Create(
            "CuttingSheetSubmitted",
            "{\"id\":\"x\"}",
            SomeTenantId,
            batchId,
            batchSequenceNumber: 3,
            aggregateId,
            "CuttingSheet",
            "CuttingSheetSubmitted");

        Assert.Equal(batchId,       message.BatchId);
        Assert.Equal(3,             message.BatchSequenceNumber);
        Assert.Equal(aggregateId,   message.AggregateId);
        Assert.Equal("CuttingSheet",            message.AggregateType);
        Assert.Equal("CuttingSheetSubmitted",   message.EventType);
    }

    // ── 3. MarkProcessed sets Status = Processed ─────────────────────────────

    [Fact]
    public void MarkProcessed_SetsStatusToProcessed()
    {
        var message = OutboxMessage.Create("EscrowTrigger", "{}", SomeTenantId);

        message.MarkProcessed(DateTimeOffset.UtcNow);

        Assert.Equal(OutboxStatus.Processed, message.Status);
    }

    // ── 4. MarkFailed increments Attempts and sets LastError ──────────────────

    [Fact]
    public void MarkFailed_IncrementsAttemptsAndSetsLastError()
    {
        var message = OutboxMessage.Create("EscrowTrigger", "{}", SomeTenantId);

        message.MarkFailed("Connection timeout");

        Assert.Equal(1, message.Attempts);
        Assert.Equal("Connection timeout", message.LastError);
        Assert.Equal(OutboxStatus.Failed, message.Status);
    }

    [Fact]
    public void MarkFailed_CalledTwice_AttemptsIsTwo()
    {
        var message = OutboxMessage.Create("EscrowTrigger", "{}", SomeTenantId);

        message.MarkFailed("first error");
        message.MarkFailed("second error");

        Assert.Equal(2, message.Attempts);
        Assert.Equal("second error", message.LastError);
    }

    // ── 5. ResetToPending clears LastError and restores Status ───────────────

    [Fact]
    public void ResetToPending_AfterFailed_ClearsLastErrorAndSetsPending()
    {
        var message = OutboxMessage.Create("EscrowTrigger", "{}", SomeTenantId);
        message.MarkFailed("some error");

        message.ResetToPending();

        Assert.Equal(OutboxStatus.Pending, message.Status);
        Assert.Null(message.LastError);
    }
}
