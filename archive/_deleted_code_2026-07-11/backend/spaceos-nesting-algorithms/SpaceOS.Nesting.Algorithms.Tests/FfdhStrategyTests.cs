using FluentAssertions;
using SpaceOS.Nesting.Algorithms.Models;
using SpaceOS.Nesting.Algorithms.Strategies;
using Xunit;

namespace SpaceOS.Nesting.Algorithms.Tests;

public class FfdhStrategyTests
{
    private readonly FfdhNestingStrategy _sut = new();

    private static AvailablePanel FullPanel(decimal w = 2800, decimal h = 2070, string? id = null)
        => new(id ?? Guid.NewGuid().ToString(), "MDF18mm", w, h, IsOffcut: false);

    private static NestingPart Part(string name, decimal w, decimal h, bool canRotate = true, int qty = 1)
        => new(Guid.NewGuid().ToString(), name, w, h, canRotate, qty);

    // ── Basic placement ───────────────────────────────────────────────────────

    [Fact]
    public async Task OnePart_OnePanelFits_PlacedAtOrigin()
    {
        var input = new NestingInput(
            new[] { Part("Door", 600, 2000) },
            new[] { FullPanel() });

        var result = await _sut.ComputeAsync(input);

        result.Assignments.Should().HaveCount(1);
        var placed = result.Assignments[0].PlacedParts.Single();
        placed.X.Should().Be(0);
        placed.Y.Should().Be(0);
        placed.Name.Should().Be("Door");
    }

    [Fact]
    public async Task TwoParts_FitSideBySide_OnePanelTwoPlacements()
    {
        var input = new NestingInput(
            new[] { Part("L", 600, 800), Part("R", 600, 800) },
            new[] { FullPanel() });

        var result = await _sut.ComputeAsync(input);

        result.Assignments.Should().HaveCount(1);
        result.Assignments[0].PlacedParts.Should().HaveCount(2);
    }

    [Fact]
    public async Task TwoParts_NoFitOnOnePanel_TwoPanelsUsed()
    {
        var panels = new[]
        {
            new AvailablePanel(Guid.NewGuid().ToString(), "MDF18mm", 1000, 600, false),
            new AvailablePanel(Guid.NewGuid().ToString(), "MDF18mm", 1000, 600, false)
        };
        var input = new NestingInput(
            new[] { Part("A", 600, 500), Part("B", 600, 500) },
            panels, SawBladeGapMm: 4);

        var result = await _sut.ComputeAsync(input);

        result.Assignments.Should().HaveCount(2);
        result.Assignments.Sum(a => a.PlacedParts.Count).Should().Be(2);
    }

    // ── Saw-blade gap ─────────────────────────────────────────────────────────

    [Fact]
    public async Task SawBladeGap_IncludedInXPosition()
    {
        var input = new NestingInput(
            new[] { Part("A", 300, 500), Part("B", 300, 500) },
            new[] { FullPanel() },
            SawBladeGapMm: 4);

        var result = await _sut.ComputeAsync(input);

        var placements = result.Assignments[0].PlacedParts.OrderBy(p => p.X).ToList();
        placements[0].X.Should().Be(0);
        placements[1].X.Should().Be(304);
    }

    // ── Rotation ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Part_FitsOnlyRotated_IsPlacedRotated()
    {
        var panels = new[] { new AvailablePanel(Guid.NewGuid().ToString(), "MDF18mm", 400, 2000, false) };
        var input = new NestingInput(new[] { Part("Wide", 600, 300, canRotate: true) }, panels);

        var result = await _sut.ComputeAsync(input);

        var placed = result.Assignments[0].PlacedParts.Single();
        placed.IsRotated.Should().BeTrue();
        placed.WidthMm.Should().Be(300);
        placed.HeightMm.Should().Be(600);
    }

    // ── Offcut priority ───────────────────────────────────────────────────────

    [Fact]
    public async Task Offcut_UsedBeforeFullPanel()
    {
        var offcutId    = "offcut-1";
        var fullPanelId = "full-1";
        var panels = new[]
        {
            new AvailablePanel(fullPanelId, "MDF18mm", 2800, 2070, IsOffcut: false),
            new AvailablePanel(offcutId,    "MDF18mm", 700,  600,  IsOffcut: true)
        };
        var input = new NestingInput(new[] { Part("P", 600, 500) }, panels);

        var result = await _sut.ComputeAsync(input);

        result.Assignments.Should().HaveCount(1);
        result.Assignments[0].PanelId.Should().Be(offcutId);
    }

    // ── Quantity expansion ────────────────────────────────────────────────────

    [Fact]
    public async Task Quantity2_PlacesTwoCopiesOfPart()
    {
        var input = new NestingInput(
            new[] { Part("Door", 400, 500, qty: 2) },
            new[] { FullPanel() });

        var result = await _sut.ComputeAsync(input);

        result.Assignments.Sum(a => a.PlacedParts.Count).Should().Be(2);
    }

    [Fact]
    public async Task Quantity5_PlacesFiveCopies()
    {
        var input = new NestingInput(
            new[] { Part("Shelf", 200, 300, qty: 5) },
            new[] { FullPanel() });

        var result = await _sut.ComputeAsync(input);

        result.Assignments.Sum(a => a.PlacedParts.Count).Should().Be(5);
    }

    // ── UnplacedParts ─────────────────────────────────────────────────────────

    [Fact]
    public async Task OversizedPart_AppearsInUnplacedParts()
    {
        var smallPanel = new AvailablePanel(Guid.NewGuid().ToString(), "MDF18mm", 500, 500, false);
        var input = new NestingInput(
            new[] { Part("Small", 200, 200), Part("Huge", 600, 600, canRotate: false) },
            new[] { smallPanel });

        var result = await _sut.ComputeAsync(input);

        result.UnplacedParts.Should().ContainSingle(p => p.Name == "Huge");
        result.Assignments.Sum(a => a.PlacedParts.Count).Should().Be(1);
    }

    // ── Empty / edge cases ────────────────────────────────────────────────────

    [Fact]
    public async Task NoParts_ReturnsEmptyResult()
    {
        var input = new NestingInput(Array.Empty<NestingPart>(), new[] { FullPanel() });

        var result = await _sut.ComputeAsync(input);

        result.Assignments.Should().BeEmpty();
        result.UnplacedParts.Should().BeEmpty();
    }

    [Fact]
    public async Task NoPanels_ReturnsEmptyResult()
    {
        var input = new NestingInput(new[] { Part("P", 600, 500) }, Array.Empty<AvailablePanel>());

        var result = await _sut.ComputeAsync(input);

        result.Assignments.Should().BeEmpty();
    }

    // ── AlgorithmUsed / ComputationTime ──────────────────────────────────────

    [Fact]
    public async Task Result_AlgorithmUsedIsFfdh()
    {
        var input = new NestingInput(new[] { Part("P", 600, 500) }, new[] { FullPanel() });

        var result = await _sut.ComputeAsync(input);

        result.AlgorithmUsed.Should().Be("FFDH");
    }

    [Fact]
    public async Task Result_ComputationTimeIsPositive()
    {
        var input = new NestingInput(new[] { Part("P", 600, 500) }, new[] { FullPanel() });

        var result = await _sut.ComputeAsync(input);

        result.ComputationTime.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
    }
}
