// SpaceOS.Kernel.Tests/Application/GetDashboardStatsQueryHandlerTests.cs
using Moq;
using SpaceOS.Kernel.Application.Dashboard;
using SpaceOS.Kernel.Application.Dashboard.Queries;
using SpaceOS.Kernel.Domain.Dashboard;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

/// <summary>Unit tests for <see cref="GetDashboardStatsQueryHandler"/>.</summary>
public sealed class GetDashboardStatsQueryHandlerTests
{
    private readonly Mock<IDashboardStatsQuery> _dashboardStatsQueryMock = new();
    private readonly GetDashboardStatsQueryHandler _handler;

    /// <summary>Initialises the handler under test with a mocked <see cref="IDashboardStatsQuery"/>.</summary>
    public GetDashboardStatsQueryHandlerTests() =>
        _handler = new GetDashboardStatsQueryHandler(_dashboardStatsQueryMock.Object);

    [Fact]
    public async Task Handle_ValidStats_ReturnsMappedDashboardStatsDto()
    {
        // Arrange
        var stats = new DashboardStats(
            TenantCount: 3,
            FacilityCount: 7,
            WorkStationCount: 42,
            ActiveWorkStationCount: 15,
            FlowEpicCount: 8,
            AuditEventCount: 200);

        _dashboardStatsQueryMock
            .Setup(q => q.QueryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        // Act
        var result = await _handler.Handle(new GetDashboardStatsQuery(), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.TenantCount);
        Assert.Equal(7, result.Value.FacilityCount);
        Assert.Equal(42, result.Value.WorkStationCount);
        Assert.Equal(15, result.Value.ActiveWorkStationCount);
        Assert.Equal(8, result.Value.FlowEpicCount);
        Assert.Equal(200, result.Value.AuditEventCount);
    }

    [Fact]
    public async Task Handle_AllZeroCounts_ReturnsDtoWithAllZeros()
    {
        // Arrange
        var stats = new DashboardStats(0, 0, 0, 0, 0, 0);

        _dashboardStatsQueryMock
            .Setup(q => q.QueryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        // Act
        var result = await _handler.Handle(new GetDashboardStatsQuery(), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.TenantCount);
        Assert.Equal(0, result.Value.FacilityCount);
        Assert.Equal(0, result.Value.WorkStationCount);
        Assert.Equal(0, result.Value.ActiveWorkStationCount);
        Assert.Equal(0, result.Value.FlowEpicCount);
        Assert.Equal(0, result.Value.AuditEventCount);
    }

    [Fact]
    public async Task Handle_ForwardsCancellationToken_ToQueryAsync()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var ct = cts.Token;
        CancellationToken captured = CancellationToken.None;

        _dashboardStatsQueryMock
            .Setup(q => q.QueryAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(token => captured = token)
            .ReturnsAsync(new DashboardStats(0, 0, 0, 0, 0, 0));

        // Act
        await _handler.Handle(new GetDashboardStatsQuery(), ct);

        // Assert
        Assert.Equal(ct, captured);
    }

    [Fact]
    public async Task Handle_CallsQueryAsync_ExactlyOnce()
    {
        // Arrange
        _dashboardStatsQueryMock
            .Setup(q => q.QueryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardStats(1, 2, 3, 4, 5, 6));

        // Act
        await _handler.Handle(new GetDashboardStatsQuery(), CancellationToken.None);

        // Assert
        _dashboardStatsQueryMock.Verify(
            q => q.QueryAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
