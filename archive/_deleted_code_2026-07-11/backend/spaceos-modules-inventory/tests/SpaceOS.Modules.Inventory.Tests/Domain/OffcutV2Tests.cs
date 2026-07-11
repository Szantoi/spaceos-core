using FluentAssertions;
using SpaceOS.Modules.Inventory.Domain.Aggregates;
using SpaceOS.Modules.Inventory.Domain.Enums;
using Xunit;

namespace SpaceOS.Modules.Inventory.Tests.Domain;

public class OffcutV2Tests
{
    private static readonly Guid TenantId  = Guid.NewGuid();
    private static readonly Guid CatalogId = Guid.NewGuid();

    private static Offcut MakeOffcut(decimal w = 500, decimal h = 400, decimal t = 18)
        => Offcut.Register(TenantId, CatalogId, "MDF18mm", w, h, t, 0m, 0m, null, null);

    // ── VolumeM3 ──────────────────────────────────────────────────────────────

    [Fact]
    public void Register_ComputesVolumeM3_Correctly()
    {
        var o = MakeOffcut(500, 400, 18);
        // 500 * 400 * 18 = 3_600_000 mm³ → / 1_000_000_000 = 0.0036 m³
        o.VolumeM3.Should().BeApproximately(0.0036m, 0.000001m);
    }

    [Fact]
    public void Register_ZeroThickness_VolumeIsZero()
    {
        var o = Offcut.Register(TenantId, CatalogId, "MDF18mm", 500, 400, 0m, 0m, 0m, null, null);
        o.VolumeM3.Should().Be(0m);
    }

    // ── Status transitions ────────────────────────────────────────────────────

    [Fact]
    public void Reserve_FromAvailable_ChangesStatusToReserved()
    {
        var o = MakeOffcut();
        o.Reserve();
        o.Status.Should().Be(OffcutStatus.Reserved);
    }

    [Fact]
    public void Reserve_FromReserved_Throws()
    {
        var o = MakeOffcut();
        o.Reserve();
        o.Invoking(x => x.Reserve()).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CancelReservation_FromReserved_RestoresAvailable()
    {
        var o = MakeOffcut();
        o.Reserve();
        o.CancelReservation();
        o.Status.Should().Be(OffcutStatus.Available);
    }

    [Fact]
    public void MarkUsed_WithJobId_SetsUsedAtAndJobId()
    {
        var jobId = Guid.NewGuid();
        var o = MakeOffcut();
        o.MarkUsed(jobId);
        o.Status.Should().Be(OffcutStatus.Used);
        o.UsedInJobId.Should().Be(jobId);
        o.UsedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkUsed_FromReserved_Succeeds()
    {
        var o = MakeOffcut();
        o.Reserve();
        o.MarkUsed(Guid.NewGuid());
        o.Status.Should().Be(OffcutStatus.Used);
    }

    [Fact]
    public void MarkUsed_FromUsed_Throws()
    {
        var o = MakeOffcut();
        o.MarkUsed(Guid.NewGuid());
        o.Invoking(x => x.MarkUsed(Guid.NewGuid())).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Scrap_FromAvailable_Succeeds()
    {
        var o = MakeOffcut();
        o.Scrap();
        o.Status.Should().Be(OffcutStatus.Scrapped);
    }

    [Fact]
    public void Scrap_FromUsed_Throws()
    {
        var o = MakeOffcut();
        o.MarkUsed(Guid.NewGuid());
        o.Invoking(x => x.Scrap()).Should().Throw<InvalidOperationException>();
    }

    // ── Guard validations ─────────────────────────────────────────────────────

    [Fact]
    public void Register_EmptyTenantId_Throws()
    {
        var act = () => Offcut.Register(Guid.Empty, CatalogId, "X", 100, 100, 18, 0, 0, null, null);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Register_NegativeWidth_Throws()
    {
        var act = () => Offcut.Register(TenantId, CatalogId, "X", -1, 100, 18, 0, 0, null, null);
        act.Should().Throw<ArgumentException>();
    }

    // ── Legacy overloads ──────────────────────────────────────────────────────

    [Fact]
    public void LegacyRegister_CreatesAvailableOffcut()
    {
        var o = Offcut.Register(TenantId, CatalogId, 300, 200, null);
        o.Status.Should().Be(OffcutStatus.Available);
        o.ThicknessMm.Should().Be(0m);
    }
}

public class OffcutReservationTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OffcutId = Guid.NewGuid();
    private static readonly Guid JobId    = Guid.NewGuid();

    [Fact]
    public void Create_SetsStatusPending_AndExpiresIn7Days()
    {
        var before = DateTime.UtcNow;
        var r = OffcutReservation.Create(OffcutId, JobId, TenantId);
        r.Status.Should().Be(OffcutReservationStatus.Pending);
        r.ExpiresAt.Should().BeCloseTo(before.AddDays(7), TimeSpan.FromSeconds(5));
        r.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void Approve_FromPending_ChangesStatusToApproved()
    {
        var r = OffcutReservation.Create(OffcutId, JobId, TenantId);
        r.Approve();
        r.Status.Should().Be(OffcutReservationStatus.Approved);
    }

    [Fact]
    public void Approve_FromApproved_Throws()
    {
        var r = OffcutReservation.Create(OffcutId, JobId, TenantId);
        r.Approve();
        r.Invoking(x => x.Approve()).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_FromPending_Succeeds()
    {
        var r = OffcutReservation.Create(OffcutId, JobId, TenantId);
        r.Cancel();
        r.Status.Should().Be(OffcutReservationStatus.Cancelled);
    }

    [Fact]
    public void Cancel_AlreadyCancelled_Throws()
    {
        var r = OffcutReservation.Create(OffcutId, JobId, TenantId);
        r.Cancel();
        r.Invoking(x => x.Cancel()).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Create_EmptyOffcutId_Throws()
        => Assert.Throws<ArgumentException>(() => OffcutReservation.Create(Guid.Empty, JobId, TenantId));

    [Fact]
    public void Create_EmptyJobId_Throws()
        => Assert.Throws<ArgumentException>(() => OffcutReservation.Create(OffcutId, Guid.Empty, TenantId));
}
