using Ardalis.Result;
using FluentAssertions;
using Moq;
using SpaceOS.Modules.Inventory.Application.Commands.ApproveOffcutReservation;
using SpaceOS.Modules.Inventory.Application.Commands.ReserveOffcut;
using SpaceOS.Modules.Inventory.Application.Commands.UseOffcutInJob;
using SpaceOS.Modules.Inventory.Domain.Aggregates;
using SpaceOS.Modules.Inventory.Domain.Enums;
using SpaceOS.Modules.Inventory.Domain.Interfaces;
using Xunit;

namespace SpaceOS.Modules.Inventory.Tests.Application;

public class ReuseCommandsTests
{
    private static readonly Guid TenantId  = Guid.NewGuid();
    private static readonly Guid CatalogId = Guid.NewGuid();
    private static readonly Guid JobId     = Guid.NewGuid();

    private static Offcut MakeAvailableOffcut()
        => Offcut.Register(TenantId, CatalogId, "MDF18mm", 500, 400, 18, 0m, 0m, null, null);

    // ── ReserveOffcutCommand ──────────────────────────────────────────────────

    [Fact]
    public async Task Reserve_AvailableOffcut_ReturnsReservationId()
    {
        var offcut = MakeAvailableOffcut();
        var repo   = new Mock<IInventoryRepository>();
        repo.Setup(r => r.GetOffcutByIdAsync(offcut.Id, default)).ReturnsAsync(offcut);

        var result = await new ReserveOffcutCommandHandler(repo.Object)
            .Handle(new ReserveOffcutCommand(offcut.Id, JobId, TenantId), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReservationId.Should().NotBeEmpty();
        result.Value.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        repo.Verify(r => r.AddOffcutReservationAsync(It.IsAny<OffcutReservation>(), default), Times.Once);
        repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Reserve_OffcutNotFound_ReturnsNotFound()
    {
        var repo = new Mock<IInventoryRepository>();
        repo.Setup(r => r.GetOffcutByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Offcut?)null);

        var result = await new ReserveOffcutCommandHandler(repo.Object)
            .Handle(new ReserveOffcutCommand(Guid.NewGuid(), JobId, TenantId), default);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Reserve_NotAvailableOffcut_ReturnsConflict()
    {
        var offcut = MakeAvailableOffcut();
        offcut.Reserve(); // drive it to Reserved via Approve flow — use Scrap to make it non-Available
        // Actually: use MarkUsed to make it Used
        offcut.MarkUsed(JobId);

        var repo = new Mock<IInventoryRepository>();
        repo.Setup(r => r.GetOffcutByIdAsync(offcut.Id, default)).ReturnsAsync(offcut);

        var result = await new ReserveOffcutCommandHandler(repo.Object)
            .Handle(new ReserveOffcutCommand(offcut.Id, JobId, TenantId), default);

        result.Status.Should().Be(ResultStatus.Conflict);
    }

    // ── ApproveOffcutReservationCommand ───────────────────────────────────────

    [Fact]
    public async Task Approve_PendingReservation_ApprovesAndReservesOffcut()
    {
        var offcut      = MakeAvailableOffcut();
        var reservation = OffcutReservation.Create(offcut.Id, JobId, TenantId);

        var repo = new Mock<IInventoryRepository>();
        repo.Setup(r => r.GetOffcutReservationByIdAsync(reservation.Id, default)).ReturnsAsync(reservation);
        repo.Setup(r => r.GetOffcutByIdAsync(offcut.Id, default)).ReturnsAsync(offcut);

        var result = await new ApproveOffcutReservationCommandHandler(repo.Object)
            .Handle(new ApproveOffcutReservationCommand(reservation.Id), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Approved");
        reservation.Status.Should().Be(OffcutReservationStatus.Approved);
        offcut.Status.Should().Be(OffcutStatus.Reserved);
    }

    [Fact]
    public async Task Approve_ReservationNotFound_ReturnsNotFound()
    {
        var repo = new Mock<IInventoryRepository>();
        repo.Setup(r => r.GetOffcutReservationByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((OffcutReservation?)null);

        var result = await new ApproveOffcutReservationCommandHandler(repo.Object)
            .Handle(new ApproveOffcutReservationCommand(Guid.NewGuid()), default);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Approve_ExpiredReservation_ReturnsError()
    {
        var offcut      = MakeAvailableOffcut();
        var reservation = OffcutReservation.Create(offcut.Id, JobId, TenantId);
        // Simulate expiry by using a recently-created reservation with a past ExpiresAt
        // We test via the IsExpired property — create a type with past date via reflection
        var field = typeof(OffcutReservation)
            .GetProperty("ExpiresAt", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        // ExpiresAt is private set; use the backing via the private setter reflection
        typeof(OffcutReservation)
            .GetProperty("ExpiresAt")!
            .SetValue(reservation, DateTime.UtcNow.AddDays(-1));

        var repo = new Mock<IInventoryRepository>();
        repo.Setup(r => r.GetOffcutReservationByIdAsync(reservation.Id, default)).ReturnsAsync(reservation);

        var result = await new ApproveOffcutReservationCommandHandler(repo.Object)
            .Handle(new ApproveOffcutReservationCommand(reservation.Id), default);

        result.Status.Should().Be(ResultStatus.Error);
    }

    // ── UseOffcutInJobCommand ─────────────────────────────────────────────────

    [Fact]
    public async Task Use_ReservedOffcut_MarksUsed()
    {
        var offcut = MakeAvailableOffcut();
        offcut.Reserve();

        var repo = new Mock<IInventoryRepository>();
        repo.Setup(r => r.GetOffcutByIdAsync(offcut.Id, default)).ReturnsAsync(offcut);

        var result = await new UseOffcutInJobCommandHandler(repo.Object)
            .Handle(new UseOffcutInJobCommand(offcut.Id, JobId), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Used");
        result.Value.UsedInJobId.Should().Be(JobId);
        offcut.Status.Should().Be(OffcutStatus.Used);
    }

    [Fact]
    public async Task Use_OffcutNotFound_ReturnsNotFound()
    {
        var repo = new Mock<IInventoryRepository>();
        repo.Setup(r => r.GetOffcutByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Offcut?)null);

        var result = await new UseOffcutInJobCommandHandler(repo.Object)
            .Handle(new UseOffcutInJobCommand(Guid.NewGuid(), JobId), default);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Use_NotReservedOffcut_ReturnsConflict()
    {
        var offcut = MakeAvailableOffcut(); // Available, not Reserved

        var repo = new Mock<IInventoryRepository>();
        repo.Setup(r => r.GetOffcutByIdAsync(offcut.Id, default)).ReturnsAsync(offcut);

        var result = await new UseOffcutInJobCommandHandler(repo.Object)
            .Handle(new UseOffcutInJobCommand(offcut.Id, JobId), default);

        result.Status.Should().Be(ResultStatus.Conflict);
    }

    // ── Full lifecycle ────────────────────────────────────────────────────────

    [Fact]
    public async Task FullLifecycle_Reserve_Approve_Use_Succeeds()
    {
        var offcut = MakeAvailableOffcut();
        OffcutReservation? storedReservation = null;

        var repo = new Mock<IInventoryRepository>();
        repo.Setup(r => r.GetOffcutByIdAsync(offcut.Id, default)).ReturnsAsync(offcut);
        repo.Setup(r => r.AddOffcutReservationAsync(It.IsAny<OffcutReservation>(), default))
            .Callback<OffcutReservation, CancellationToken>((r, _) => storedReservation = r)
            .Returns(Task.CompletedTask);
        repo.Setup(r => r.GetOffcutReservationByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(() => storedReservation);

        // 1. Reserve
        var reserveResult = await new ReserveOffcutCommandHandler(repo.Object)
            .Handle(new ReserveOffcutCommand(offcut.Id, JobId, TenantId), default);
        reserveResult.IsSuccess.Should().BeTrue();

        // 2. Approve
        var approveResult = await new ApproveOffcutReservationCommandHandler(repo.Object)
            .Handle(new ApproveOffcutReservationCommand(storedReservation!.Id), default);
        approveResult.IsSuccess.Should().BeTrue();
        offcut.Status.Should().Be(OffcutStatus.Reserved);

        // 3. Use
        var useResult = await new UseOffcutInJobCommandHandler(repo.Object)
            .Handle(new UseOffcutInJobCommand(offcut.Id, JobId), default);
        useResult.IsSuccess.Should().BeTrue();
        offcut.Status.Should().Be(OffcutStatus.Used);
    }
}
