using FluentAssertions;
using SpaceOS.Nesting.Algorithms.Models;
using SpaceOS.Nesting.Algorithms.Strategies;
using Xunit;

namespace SpaceOS.Nesting.Algorithms.Tests;

/// <summary>
/// Tests for NESTING-001: WastePiece model + WastePieces in PanelAssignment invariant.
/// </summary>
public class WastePieceTests
{
    private static AvailablePanel Panel(decimal w, decimal h, bool isOffcut = false)
        => new(Guid.NewGuid().ToString(), "MDF18mm", w, h, isOffcut);

    private static NestingPart Part(decimal w, decimal h, bool canRotate = false, int qty = 1)
        => new(Guid.NewGuid().ToString(), $"{w}x{h}", w, h, canRotate, qty);

    // ── 1. Guillotine: WastePieces not empty after partial fill ─────────────

    [Fact]
    public async Task GuillotineStrategy_WastePieces_NotEmpty()
    {
        // Panel 1000x1000, one small part 400x300 — plenty of leftover space
        var panels = new[] { Panel(1000m, 1000m) };
        var parts  = new[] { Part(400m, 300m) };
        var input  = new NestingInput(parts, panels, SawBladeGapMm: 0);

        var sut    = new GuillotineNestingStrategy();
        var result = await sut.ComputeAsync(input);

        result.Assignments.Should().HaveCount(1);
        var assignment = result.Assignments[0];
        assignment.WastePieces.Should().NotBeEmpty(
            "a 400×300 part on a 1000×1000 panel leaves significant waste rectangles");
        assignment.WastePieces.Should().AllSatisfy(w =>
        {
            w.WidthMm.Should().BeGreaterThan(0);
            w.HeightMm.Should().BeGreaterThan(0);
        });
    }

    // ── 2. WasteAreaMm2 ≈ WastePieces.Sum(w => w.AreaMm2) (±1 mm² tolerance) ──

    [Fact]
    public async Task WastePiece_AreaSum_EqualsWasteAreaMm2_Guillotine()
    {
        // Panel 1000x800, two parts: 600x500 and 300x200
        var panels = new[] { Panel(1000m, 800m) };
        var parts  = new[] { Part(600m, 500m), Part(300m, 200m) };
        var input  = new NestingInput(parts, panels, SawBladeGapMm: 0);

        var sut    = new GuillotineNestingStrategy();
        var result = await sut.ComputeAsync(input);

        result.Assignments.Should().HaveCount(1);
        var a = result.Assignments[0];

        var wasteFromPieces = a.WastePieces.Sum(w => w.AreaMm2);
        var tolerance = 1m; // ±1 mm²

        wasteFromPieces.Should().BeApproximately(a.WasteAreaMm2, tolerance,
            $"WastePieces area sum ({wasteFromPieces}) should equal WasteAreaMm2 ({a.WasteAreaMm2}) within ±{tolerance} mm²");
    }

    // ── 3. FFDH: row-end waste captured ──────────────────────────────────────

    [Fact]
    public async Task FfdhStrategy_RowEndWaste_Captured()
    {
        // Panel 1000x500, two parts 300x400 placed in the same row
        // Row is 400mm tall, panel 1000mm wide — parts occupy 300+300=600mm
        // Row-end waste: (600, 0, 400, 400) — 400mm wide strip on the right
        var panels = new[] { Panel(1000m, 500m) };
        var parts  = new[] { Part(300m, 400m, qty: 1), Part(300m, 400m, qty: 1) };
        var input  = new NestingInput(parts, panels, SawBladeGapMm: 0);

        var sut    = new FfdhNestingStrategy();
        var result = await sut.ComputeAsync(input);

        result.Assignments.Should().HaveCount(1);
        var a = result.Assignments[0];

        // At least one row-end waste piece should be present
        a.WastePieces.Should().NotBeEmpty("two 300×400 parts on a 1000mm wide panel leave 400mm row-end waste");
        a.WastePieces.Should().AllSatisfy(w =>
        {
            w.WidthMm.Should().BeGreaterThan(0);
            w.HeightMm.Should().BeGreaterThan(0);
        });
    }
}
