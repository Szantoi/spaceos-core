using System.Diagnostics;
using SpaceOS.Nesting.Algorithms.Models;

namespace SpaceOS.Nesting.Algorithms.Strategies;

/// <summary>
/// L1 Nesting: Greedy First Fit Decreasing Height (FFDH) strip packing.
/// Ported from SpaceOS.Modules.Cutting.Domain.Services.NestingService.
/// Additions over the original: Quantity expansion, UnplacedParts tracking, ComputationTime.
/// </summary>
public sealed class FfdhNestingStrategy : INestingStrategy
{
    public string AlgorithmName => "FFDH";

    public Task<NestingResult> ComputeAsync(NestingInput input, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var result = Compute(input);
        sw.Stop();

        return Task.FromResult(result with { ComputationTime = sw.Elapsed });
    }

    private static NestingResult Compute(NestingInput input)
    {
        var sw = Stopwatch.StartNew();

        if (!input.Parts.Any() || !input.Panels.Any())
        {
            return new NestingResult(
                Array.Empty<PanelAssignment>(),
                Array.Empty<NestingPart>(),
                0m, 0, "FFDH", sw.Elapsed);
        }

        // Expand parts by Quantity, sort descending by height (FFDH)
        var expandedParts = input.Parts
            .SelectMany(p => Enumerable.Range(0, p.Quantity).Select(_ => p))
            .OrderByDescending(p => p.HeightMm)
            .ToList();

        // Offcuts first, then full panels by ascending area
        // remainingPanels is a mutable pool; panels are removed when opened so smaller
        // parts can still access panels that were skipped for oversized predecessors.
        var remainingPanels = input.Panels
            .OrderBy(p => p.IsOffcut ? 0 : 1)
            .ThenBy(p => p.WidthMm * p.HeightMm)
            .ToList();

        var openPanels = new List<PanelState>();
        var unplaced   = new List<NestingPart>();

        foreach (var part in expandedParts)
        {
            bool placed = false;

            // 1. Try already-open panels
            foreach (var panel in openPanels)
            {
                if (TryPlace(part, panel, input.SawBladeGapMm, rotate: false) ||
                    (part.CanRotate && TryPlace(part, panel, input.SawBladeGapMm, rotate: true)))
                {
                    placed = true;
                    break;
                }
            }

            // 2. Open the first fitting panel from the remaining pool
            if (!placed)
            {
                for (int i = 0; i < remainingPanels.Count; i++)
                {
                    var candidate = remainingPanels[i];
                    var fits = part.WidthMm <= candidate.WidthMm && part.HeightMm <= candidate.HeightMm;
                    var fitsRotated = part.CanRotate &&
                                     part.HeightMm <= candidate.WidthMm && part.WidthMm <= candidate.HeightMm;
                    if (!fits && !fitsRotated) continue;

                    remainingPanels.RemoveAt(i);  // panel is now open — remove from pool
                    var newPanel = new PanelState(candidate);
                    openPanels.Add(newPanel);

                    if (TryPlace(part, newPanel, input.SawBladeGapMm, rotate: false) ||
                        (part.CanRotate && TryPlace(part, newPanel, input.SawBladeGapMm, rotate: true)))
                    {
                        placed = true;
                    }
                    break;
                }
            }

            if (!placed)
                unplaced.Add(part);
        }

        var assignments = openPanels.Select(p => p.ToAssignment()).ToList();

        decimal totalWaste = 0m;
        if (assignments.Count > 0)
        {
            var totalPanelArea = assignments.Sum(a => a.PanelWidthMm * a.PanelHeightMm);
            var totalPartArea  = assignments.Sum(a => a.PlacedParts.Sum(p => p.WidthMm * p.HeightMm));
            totalWaste = totalPanelArea > 0
                ? Math.Round((totalPanelArea - totalPartArea) / totalPanelArea * 100m, 2)
                : 0m;
        }

        sw.Stop();
        return new NestingResult(assignments, unplaced, totalWaste, assignments.Count, "FFDH", sw.Elapsed);
    }

    private static bool TryPlace(NestingPart part, PanelState panel, int gap, bool rotate)
    {
        var w = rotate ? part.HeightMm : part.WidthMm;
        var h = rotate ? part.WidthMm : part.HeightMm;

        foreach (var shelf in panel.Shelves)
        {
            if (shelf.CurrentX + w <= panel.Panel.WidthMm)
            {
                panel.PlacedParts.Add(new PlacedPart(part.PartId, part.Name, shelf.CurrentX, shelf.Y, w, h, rotate));
                shelf.Place(w, gap);
                return true;
            }
        }

        var nextY = panel.Shelves.Count == 0
            ? 0m
            : panel.Shelves.Sum(s => s.Height + gap);

        if (nextY + h <= panel.Panel.HeightMm && w <= panel.Panel.WidthMm)
        {
            var shelf = new Shelf(nextY, h);
            shelf.Place(w, gap);
            panel.Shelves.Add(shelf);
            panel.PlacedParts.Add(new PlacedPart(part.PartId, part.Name, 0m, nextY, w, h, rotate));
            return true;
        }

        return false;
    }

    private sealed class PanelState
    {
        public AvailablePanel Panel { get; }
        public List<Shelf> Shelves { get; } = new();
        public List<PlacedPart> PlacedParts { get; } = new();

        public PanelState(AvailablePanel panel) => Panel = panel;

        public PanelAssignment ToAssignment()
        {
            var wastePieces = Shelves
                .Where(s => s.CurrentX < Panel.WidthMm)
                .Select(s => new WastePiece(s.CurrentX, s.Y, Panel.WidthMm - s.CurrentX, s.Height))
                .ToList();

            return new PanelAssignment(
                Panel.PanelId,
                Panel.MaterialCode,
                Panel.WidthMm,
                Panel.HeightMm,
                PlacedParts)
            {
                WastePieces = wastePieces
            };
        }
    }

    private sealed class Shelf
    {
        public decimal Y { get; }
        public decimal Height { get; }
        public decimal CurrentX { get; private set; }

        public Shelf(decimal y, decimal height)
        {
            Y = y;
            Height = height;
        }

        public void Place(decimal partWidth, int gap)
            => CurrentX += partWidth + gap;
    }
}
