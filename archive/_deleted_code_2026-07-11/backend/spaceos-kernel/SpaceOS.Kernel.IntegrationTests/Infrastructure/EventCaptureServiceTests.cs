// SpaceOS.Kernel.IntegrationTests/Infrastructure/EventCaptureServiceTests.cs
using System.Net.Http.Json;
using SpaceOS.Kernel.Api.Endpoints;
using SpaceOS.Kernel.Domain.Events;
using Xunit;

namespace SpaceOS.Kernel.IntegrationTests.Infrastructure;

/// <summary>
/// Verifies that <see cref="EventCaptureService"/> records domain events dispatched
/// during a real HTTP request through the test host pipeline.
/// </summary>
public sealed class EventCaptureServiceTests : ApiTestBase
{
    /// <summary>
    /// Sends a POST /api/tenants request that triggers the CreateTenantCommandHandler,
    /// which raises a <see cref="TenantCreatedEvent"/> and dispatches it via
    /// <see cref="EventCaptureService"/>. Asserts that the capture buffer contains
    /// the expected event type after the response is received.
    /// </summary>
    [Fact]
    public async Task EventCaptureService_RecordsEvent_WhenDispatched()
    {
        // Arrange
        Factory.Capture.Reset();
        var request = new CreateTenantRequest("Capture Test Tenant");

        // Act
        var response = await Client.PostAsJsonAsync("/api/tenants", request, TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        var captured = Factory.Capture.Events;
        Assert.Contains(captured, e => e is TenantCreatedEvent);
    }
}
