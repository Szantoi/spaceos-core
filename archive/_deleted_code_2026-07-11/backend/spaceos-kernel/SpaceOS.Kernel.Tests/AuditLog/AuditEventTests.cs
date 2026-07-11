// SpaceOS.Kernel.Tests/AuditLog/AuditEventTests.cs

using SpaceOS.Kernel.Domain.AuditLog;
using Xunit;

namespace SpaceOS.Kernel.Tests.AuditLog;

public sealed class AuditEventTests
{
    private static readonly Guid   SampleTenantId    = Guid.NewGuid();
    private static readonly Guid   SampleAggregateId = Guid.NewGuid();
    private const           string SampleEventType   = "TenantCreatedEvent";
    private const           string SamplePayload     = """{"tenantId":"abc"}""";
    private const           string SampleStateHash   = "a591a6d40bf420404a011733cfb7b190d62c65bf0bcda32b57b277d9ad9f146e";

    [Fact]
    public void AuditEvent_Create_SetsAllProperties()
    {
        // Arrange & Act
        var before = DateTimeOffset.UtcNow;
        var auditEvent = AuditEvent.Create(
            tenantId:    SampleTenantId,
            eventType:   SampleEventType,
            aggregateId: SampleAggregateId,
            payload:     SamplePayload,
            stateHash:   SampleStateHash);
        var after = DateTimeOffset.UtcNow;

        // Assert
        Assert.NotEqual(Guid.Empty, auditEvent.Id);
        Assert.Equal(SampleTenantId,    auditEvent.TenantId);
        Assert.Equal(SampleEventType,   auditEvent.EventType);
        Assert.Equal(SampleAggregateId, auditEvent.AggregateId);
        Assert.Equal(SamplePayload,     auditEvent.Payload);
        Assert.Equal(SampleStateHash,   auditEvent.StateHash);
        Assert.InRange(auditEvent.OccurredAt, before, after);
    }

    [Fact]
    public void AuditEvent_Create_RaisesAuditEventCreatedEvent()
    {
        // Arrange & Act
        var auditEvent = AuditEvent.Create(
            tenantId:    SampleTenantId,
            eventType:   SampleEventType,
            aggregateId: SampleAggregateId,
            payload:     SamplePayload,
            stateHash:   SampleStateHash);

        // Assert
        var events = auditEvent.PopDomainEvents();
        Assert.Single(events);
        var domainEvent = Assert.IsType<AuditEventCreatedEvent>(events[0]);
        Assert.Equal(auditEvent.Id, domainEvent.AuditEventId);
    }
}
