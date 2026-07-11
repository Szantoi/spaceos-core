using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SpaceOS.Modules.Joinery.Application.WorkOrders.DTOs;
using SpaceOS.Modules.Joinery.Domain.Entities;
using SpaceOS.Modules.Joinery.Infrastructure.Persistence;

namespace SpaceOS.Modules.Joinery.Tests.Api;

/// <summary>
/// HTTP integration tests for PATCH /api/v1/work-orders/{id}/assembly-sequence endpoint.
/// Tests drag-and-drop reordering with optimistic locking.
/// </summary>
[Collection("Integration")]
public sealed class WorkOrderAssemblySequenceApiTests : IClassFixture<JoineryWebFactory>
{
    private readonly JoineryWebFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public WorkOrderAssemblySequenceApiTests(JoineryWebFactory factory)
    {
        _factory = factory;
    }

    private HttpClient Client(string? tenantId = null, string tenantType = "Manufacturer") =>
        _factory.CreateAuthenticatedClient(tenantId ?? Guid.NewGuid().ToString(), tenantType);

    /// <summary>
    /// Seeds a test work order with 3 operations for testing.
    /// Returns (workOrderId, operations list, tenantId).
    /// </summary>
    private (Guid workOrderId, List<WorkOrderOperation> operations, Guid tenantId) SeedWorkOrderWithOperations()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JoineryDbContext>();

        var tenantId = Guid.NewGuid();
        var workOrderId = Guid.NewGuid();
        var configId = Guid.NewGuid();

        // Create a work order
        var workOrder = WorkOrder.Create(
            tenantId,
            configId,
            quantity: 10,
            deliveryDate: DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            customerRef: "TEST-WO-001",
            notes: "Test work order for assembly sequence",
            bomItemsJson: "[]",
            totalMaterialCost: 50000m,
            estimatedLabor: 10000m,
            totalCost: 60000m,
            scheduledStart: DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            estimatedCompletion: DateOnly.FromDateTime(DateTime.Today.AddDays(28)),
            createdBy: Guid.NewGuid());

        typeof(WorkOrder).GetProperty("Id")!.SetValue(workOrder, workOrderId);
        db.WorkOrders.Add(workOrder);

        // Create 3 operations
        var op1 = WorkOrderOperation.Create(
            tenantId,
            workOrderId,
            sequence: 1,
            description: "Cut wood panels",
            estimatedDuration: TimeSpan.FromMinutes(30),
            operationType: "cutting");

        var op2 = WorkOrderOperation.Create(
            tenantId,
            workOrderId,
            sequence: 2,
            description: "Sand surfaces",
            estimatedDuration: TimeSpan.FromMinutes(20),
            operationType: "sanding");

        var op3 = WorkOrderOperation.Create(
            tenantId,
            workOrderId,
            sequence: 3,
            description: "Paint finish",
            estimatedDuration: TimeSpan.FromMinutes(45),
            operationType: "painting");

        db.Set<WorkOrderOperation>().AddRange(op1, op2, op3);
        db.SaveChanges();

        return (workOrderId, new List<WorkOrderOperation> { op1, op2, op3 }, tenantId);
    }

    // ─── Test Cases ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAssemblySequence_ValidRequest_Returns200()
    {
        // Arrange
        var (workOrderId, operations, tenantId) = SeedWorkOrderWithOperations();
        var client = Client(tenantId.ToString());

        var request = new UpdateAssemblySequenceRequest(
            Operations: new List<OperationSequenceUpdate>
            {
                new(operations[2].Id, 1),  // Paint → sequence 1
                new(operations[1].Id, 2),  // Sand → sequence 2
                new(operations[0].Id, 3)   // Cut → sequence 3 (reversed)
            },
            Timestamp: DateTime.UtcNow);

        // Act
        var resp = await client.PatchAsJsonAsync($"/api/v1/work-orders/{workOrderId}/assembly-sequence", request);

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<UpdateAssemblySequenceResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.UpdatedOperations.Should().HaveCount(3);
        body.UpdatedOperations[0].Sequence.Should().Be(1);
        body.UpdatedOperations[0].Description.Should().Be("Paint finish");
        body.UpdatedOperations[1].Sequence.Should().Be(2);
        body.UpdatedOperations[1].Description.Should().Be("Sand surfaces");
        body.UpdatedOperations[2].Sequence.Should().Be(3);
        body.UpdatedOperations[2].Description.Should().Be("Cut wood panels");
        body.EstimatedDurationChange.Should().Be("+0min");
        body.TotalDuration.Should().Be(TimeSpan.FromMinutes(95));
    }

    [Fact]
    public async Task UpdateAssemblySequence_ConcurrentModification_Returns409()
    {
        // Arrange
        var (workOrderId, operations, tenantId) = SeedWorkOrderWithOperations();
        var client = Client(tenantId.ToString());

        // Use an old timestamp (5 minutes ago) to simulate concurrent modification
        var staleTimestamp = DateTime.UtcNow.AddMinutes(-5);

        var request = new UpdateAssemblySequenceRequest(
            Operations: new List<OperationSequenceUpdate>
            {
                new(operations[0].Id, 1),
                new(operations[1].Id, 2),
                new(operations[2].Id, 3)
            },
            Timestamp: staleTimestamp);

        // Act
        var resp = await client.PatchAsJsonAsync($"/api/v1/work-orders/{workOrderId}/assembly-sequence", request);

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("error").GetString().Should().Be("CONCURRENT_MODIFICATION");
        body.GetProperty("message").GetString().Should().Contain("modified by another user");
    }

    [Fact]
    public async Task UpdateAssemblySequence_GapInSequence_Returns400()
    {
        // Arrange
        var (workOrderId, operations, tenantId) = SeedWorkOrderWithOperations();
        var client = Client(tenantId.ToString());

        var request = new UpdateAssemblySequenceRequest(
            Operations: new List<OperationSequenceUpdate>
            {
                new(operations[0].Id, 1),
                new(operations[1].Id, 2),
                new(operations[2].Id, 4)   // Gap! Should be 3, not 4
            },
            Timestamp: DateTime.UtcNow);

        // Act
        var resp = await client.PatchAsJsonAsync($"/api/v1/work-orders/{workOrderId}/assembly-sequence", request);

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("error").GetString().Should().Be("VALIDATION_FAILED");
        var details = body.GetProperty("details");
        details.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task UpdateAssemblySequence_UnknownOperationId_Returns400()
    {
        // Arrange
        var (workOrderId, operations, tenantId) = SeedWorkOrderWithOperations();
        var client = Client(tenantId.ToString());

        var unknownId = Guid.NewGuid();

        var request = new UpdateAssemblySequenceRequest(
            Operations: new List<OperationSequenceUpdate>
            {
                new(operations[0].Id, 1),
                new(operations[1].Id, 2),
                new(unknownId, 3)   // Unknown operation ID
            },
            Timestamp: DateTime.UtcNow);

        // Act
        var resp = await client.PatchAsJsonAsync($"/api/v1/work-orders/{workOrderId}/assembly-sequence", request);

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("error").GetString().Should().Be("VALIDATION_FAILED");
        body.GetProperty("message").GetString().Should().Contain("Unknown operation IDs");
    }

    [Fact]
    public async Task UpdateAssemblySequence_WorkOrderNotFound_Returns404()
    {
        // Arrange
        var client = Client();
        var nonExistentId = Guid.NewGuid();

        var request = new UpdateAssemblySequenceRequest(
            Operations: new List<OperationSequenceUpdate>
            {
                new(Guid.NewGuid(), 1)
            },
            Timestamp: DateTime.UtcNow);

        // Act
        var resp = await client.PatchAsJsonAsync($"/api/v1/work-orders/{nonExistentId}/assembly-sequence", request);

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateAssemblySequence_CalculatesDurationChange()
    {
        // Arrange
        var (workOrderId, operations, tenantId) = SeedWorkOrderWithOperations();
        var client = Client(tenantId.ToString());

        var request = new UpdateAssemblySequenceRequest(
            Operations: new List<OperationSequenceUpdate>
            {
                new(operations[0].Id, 1),
                new(operations[1].Id, 2),
                new(operations[2].Id, 3)
            },
            Timestamp: DateTime.UtcNow);

        // Act
        var resp = await client.PatchAsJsonAsync($"/api/v1/work-orders/{workOrderId}/assembly-sequence", request);

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<UpdateAssemblySequenceResponse>(JsonOptions);
        body.Should().NotBeNull();

        // Phase 1: duration change is a stub "+0min"
        // Phase 2 will implement dependency-based calculation
        body!.EstimatedDurationChange.Should().Be("+0min");
        body.TotalDuration.Should().Be(TimeSpan.FromMinutes(95));
    }
}
