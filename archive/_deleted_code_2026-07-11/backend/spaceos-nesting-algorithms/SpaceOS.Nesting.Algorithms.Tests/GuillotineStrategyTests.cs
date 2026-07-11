using System.Diagnostics;
using FluentAssertions;
using SpaceOS.Nesting.Algorithms.Models;
using SpaceOS.Nesting.Algorithms.Strategies;
using Xunit;

namespace SpaceOS.Nesting.Algorithms.Tests;

public class GuillotineStrategyTests
{
    private readonly GuillotineNestingStrategy _sut = new();

    private static AvailablePanel Panel(decimal w, decimal h, bool isOffcut = false, string? id = null)
        => new(id ?? Guid.NewGuid().ToString(), "MDF18mm", w, h, isOffcut);

    private static NestingPart Part(decimal w, decimal h, bool canRotate = true, int qty = 1)
        => new(Guid.NewGuid().ToString(), $"{w}x{h}", w, h, canRotate, qty);

    // ── Benchmark: 10 parts → yield ≥ 85% ────────────────────────────────────

    [Fact]
    public async Task Benchmark_10Parts_YieldAtLeast85Pct()
    {
        // Panel 1000x1000 = 1,000,000 mm²
        // 10 parts 100x900 = 900,000 mm² → theoretical max 90%
        // With gap=0, all 10 should fit in one row (10×100=1000 wide, 900 tall)
        var panels = new[] { Panel(1000, 1000) };
        var parts  = Enumerable.Range(0, 10).Select(_ => Part(100, 900, qty: 1)).ToArray();
        var input  = new NestingInput(parts, panels, SawBladeGapMm: 0);

        var result = await _sut.ComputeAsync(input);

        var totalPanelArea = result.Assignments.Sum(a => a.PanelWidthMm * a.PanelHeightMm);
        var totalPartArea  = result.Assignments
            .Sum(a => a.PlacedParts.Sum(p => p.WidthMm * p.HeightMm));
        var yield = totalPanelArea > 0 ? totalPartArea / totalPanelArea * 100m : 0m;

        yield.Should().BeGreaterThanOrEqualTo(85m,
            $"10-part benchmark: yield={yield:F1}%, expected ≥85%");
    }

    // ── Benchmark: 50 parts → yield ≥ 88% ────────────────────────────────────

    [Fact]
    public async Task Benchmark_50Parts_YieldAtLeast88Pct()
    {
        // Panel 1000x1000, 50 parts 100x176 → 50 × 17,600 = 880,000 mm² = 88%
        // 5 rows of 10 parts (row height=176, 5×176=880 ≤ 1000): all fit
        var panels = new[] { Panel(1000, 1000) };
        var parts  = Enumerable.Range(0, 50).Select(_ => Part(100, 176, qty: 1)).ToArray();
        var input  = new NestingInput(parts, panels, SawBladeGapMm: 0);

        var result = await _sut.ComputeAsync(input);

        var totalPanelArea = result.Assignments.Sum(a => a.PanelWidthMm * a.PanelHeightMm);
        var totalPartArea  = result.Assignments
            .Sum(a => a.PlacedParts.Sum(p => p.WidthMm * p.HeightMm));
        var yield = totalPanelArea > 0 ? totalPartArea / totalPanelArea * 100m : 0m;

        yield.Should().BeGreaterThanOrEqualTo(88m,
            $"50-part benchmark: yield={yield:F1}%, expected ≥88%");
    }

    // ── Benchmark: 100 parts → yield ≥ 90% ───────────────────────────────────

    [Fact]
    public async Task Benchmark_100Parts_YieldAtLeast90Pct()
    {
        // Panel 1000x1000, 100 parts 90x100 → 100 × 9,000 = 900,000 mm² = 90%
        // 10 columns of 10 (10×90=900, 10×100=1000): all fit
        var panels = new[] { Panel(1000, 1000) };
        var parts  = Enumerable.Range(0, 100).Select(_ => Part(90, 100, qty: 1)).ToArray();
        var input  = new NestingInput(parts, panels, SawBladeGapMm: 0);

        var result = await _sut.ComputeAsync(input);

        var totalPanelArea = result.Assignments.Sum(a => a.PanelWidthMm * a.PanelHeightMm);
        var totalPartArea  = result.Assignments
            .Sum(a => a.PlacedParts.Sum(p => p.WidthMm * p.HeightMm));
        var yield = totalPanelArea > 0 ? totalPartArea / totalPanelArea * 100m : 0m;

        yield.Should().BeGreaterThanOrEqualTo(90m,
            $"100-part benchmark: yield={yield:F1}%, expected ≥90%");
    }

    // ── Benchmark: 200 parts → compute ≤ 5s ──────────────────────────────────

    [Fact]
    public async Task Benchmark_200Parts_ComputeUnder5Seconds()
    {
        // Use multiple panels to ensure all 200 parts can actually be placed
        var panels = Enumerable.Range(0, 5).Select(_ => Panel(2440, 1220)).ToArray();
        var parts  = Enumerable.Range(0, 200).Select(_ => Part(200, 150, qty: 1)).ToArray();
        var input  = new NestingInput(parts, panels, SawBladeGapMm: 4);

        var sw = Stopwatch.StartNew();
        await _sut.ComputeAsync(input);
        sw.Stop();

        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5),
            $"200-part compute took {sw.Elapsed.TotalSeconds:F2}s, expected ≤5s");
    }

    // ── Offcut priority ───────────────────────────────────────────────────────

    [Fact]
    public async Task Offcut_UsedBeforeFullPanel()
    {
        var offcutId = "offcut-1";
        var panels   = new[]
        {
            Panel(2800, 2070, isOffcut: false, id: "full-1"),
            Panel(700,  600,  isOffcut: true,  id: offcutId)
        };
        var input = new NestingInput(new[] { Part(600, 500) }, panels);

        var result = await _sut.ComputeAsync(input);

        result.Assignments.Should().HaveCount(1);
        result.Assignments[0].PanelId.Should().Be(offcutId);
    }

    // ── Rotation ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Part_FitsOnlyRotated_IsPlacedRotated()
    {
        var panels = new[] { Panel(400, 2000) };
        var input  = new NestingInput(new[] { Part(600, 300, canRotate: true) }, panels);

        var result = await _sut.ComputeAsync(input);

        var placed = result.Assignments[0].PlacedParts.Single();
        placed.IsRotated.Should().BeTrue();
        placed.WidthMm.Should().Be(300);
        placed.HeightMm.Should().Be(600);
    }

    // ── AlgorithmUsed ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Result_AlgorithmUsedIsGuillotine()
    {
        var input = new NestingInput(new[] { Part(600, 500) }, new[] { Panel(2800, 2070) });

        var result = await _sut.ComputeAsync(input);

        result.AlgorithmUsed.Should().Be("Guillotine");
    }

    // ── Empty inputs ──────────────────────────────────────────────────────────

    [Fact]
    public async Task NoParts_ReturnsEmptyAssignments()
    {
        var input = new NestingInput(Array.Empty<NestingPart>(), new[] { Panel(2800, 2070) });

        var result = await _sut.ComputeAsync(input);

        result.Assignments.Should().BeEmpty();
        result.UnplacedParts.Should().BeEmpty();
    }
}
