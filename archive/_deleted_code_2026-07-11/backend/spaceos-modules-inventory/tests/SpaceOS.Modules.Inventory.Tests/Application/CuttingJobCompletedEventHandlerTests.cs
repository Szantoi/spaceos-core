using FluentAssertions;
using Moq;
using SpaceOS.Modules.Inventory.Application.Events;
using SpaceOS.Modules.Inventory.Domain.Aggregates;
using SpaceOS.Modules.Inventory.Domain.Interfaces;
using Xunit;

namespace SpaceOS.Modules.Inventory.Tests.Application;

public class CuttingJobCompletedEventHandlerTests
{
    private static readonly Guid TenantId    = Guid.NewGuid();
    private static readonly Guid CatalogId   = Guid.NewGuid();
    private static readonly Guid JobId       = Guid.NewGuid();

    private readonly Mock<IInventoryRepository> _repoMock = new();
    private readonly CuttingJobCompletedEventHandler _handler;

    public CuttingJobCompletedEventHandlerTests()
    {
        _handler = new CuttingJobCompletedEventHandler(_repoMock.Object);
    }

    private static CuttingJobCompletedEvent MakeEvent(
        decimal wastePercent = 0.15m,
        decimal w = 1000m,
        decimal h = 800m,
        decimal t = 18m)
        => new(JobId, CatalogId, "MDF18mm", w, h, t, wastePercent, TenantId);

    // ── Offcut creation ───────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithDefaultWaste_CreatesOffcutAndSaves()
    {
        Offcut? captured = null;
        _repoMock
            .Setup(r => r.AddOffcutAsync(It.IsAny<Offcut>(), default))
            .Callback<Offcut, CancellationToken>((o, _) => captured = o)
            .Returns(Task.CompletedTask);

        await _handler.Handle(MakeEvent(), default);

        captured.Should().NotBeNull();
        _repoMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_OffcutHasCuttingJobId()
    {
        Offcut? captured = null;
        _repoMock
            .Setup(r => r.AddOffcutAsync(It.IsAny<Offcut>(), default))
            .Callback<Offcut, CancellationToken>((o, _) => captured = o)
            .Returns(Task.CompletedTask);

        await _handler.Handle(MakeEvent(), default);

        captured!.CuttingJobId.Should().Be(JobId);
        captured.TenantId.Should().Be(TenantId);
        captured.MaterialCatalogId.Should().Be(CatalogId);
    }

    // ── Volume calculation ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WasteVolume_IsCorrectFractionOfTotal()
    {
        // 1000 * 800 * 18 / 1e9 = 0.0144 m³, 15% = 0.00216
        Offcut? captured = null;
        _repoMock
            .Setup(r => r.AddOffcutAsync(It.IsAny<Offcut>(), default))
            .Callback<Offcut, CancellationToken>((o, _) => captured = o)
            .Returns(Task.CompletedTask);

        await _handler.Handle(MakeEvent(wastePercent: 0.15m, w: 1000m, h: 800m, t: 18m), default);

        var expectedWaste = Offcut.ComputeVolume(1000m, 800m, 18m) * 0.15m;
        captured!.VolumeM3.Should().Be(expectedWaste);
    }

    [Fact]
    public async Task Handle_StubDimensions_AreHalfOfJobSheet()
    {
        Offcut? captured = null;
        _repoMock
            .Setup(r => r.AddOffcutAsync(It.IsAny<Offcut>(), default))
            .Callback<Offcut, CancellationToken>((o, _) => captured = o)
            .Returns(Task.CompletedTask);

        await _handler.Handle(MakeEvent(w: 1000m, h: 800m), default);

        captured!.WidthMm.Should().Be(500m);
        captured.HeightMm.Should().Be(400m);
    }

    // ── Zero-waste guard ──────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ZeroWastePercent_NoOffcutCreated()
    {
        await _handler.Handle(MakeEvent(wastePercent: 0m), default);

        _repoMock.Verify(r => r.AddOffcutAsync(It.IsAny<Offcut>(), default), Times.Never);
        _repoMock.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task Handle_NegativeWastePercent_NoOffcutCreated()
    {
        await _handler.Handle(MakeEvent(wastePercent: -0.1m), default);

        _repoMock.Verify(r => r.AddOffcutAsync(It.IsAny<Offcut>(), default), Times.Never);
    }
}
