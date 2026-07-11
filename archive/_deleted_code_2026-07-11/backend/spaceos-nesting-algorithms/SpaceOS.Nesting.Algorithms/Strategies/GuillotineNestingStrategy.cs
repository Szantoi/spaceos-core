using System.Diagnostics;
using SpaceOS.Nesting.Algorithms.Models;

namespace SpaceOS.Nesting.Algorithms.Strategies;

/// <summary>
/// L2 Nesting: Guillotine cut with Best Area Fit (BAF) heuristic.
/// Each placement guillotine-splits the used free rectangle into two new ones.
/// Offcuts have priority over full panels; rotation tried when CanRotate=true.
///
/// Benchmark targets (SawBladeGapMm=0):
///   10 parts  → yield ≥ 85%
///   50 parts  → yield ≥ 88%
///   100 parts → yield ≥ 90%
///   200 parts → compute ≤ 5s
/// </summary>
public sealed class GuillotineNestingStrategy : INestingStrategy
{
    public string AlgorithmName => "Guillotine";

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
            return new NestingResult(Array.Empty<PanelAssignment>(), Array.Empty<NestingPart>(),
                                     0m, 0, "Guillotine", sw.Elapsed);

        // Expand by Quantity, sort by area descending (largest first → better packing)
        var expandedParts = input.Parts
            .SelectMany(p => Enumerable.Range(0, p.Quantity).Select(_ => p))
            .OrderByDescending(p => p.WidthMm * p.HeightMm)
            .ToList();

        // Offcuts first, then full panels by ascending area
        var orderedPanels = input.Panels
            .OrderBy(p => p.IsOffcut ? 0 : 1)
            .ThenBy(p => p.WidthMm * p.HeightMm)
            .ToList();

        var panelStates = new List<PanelState>();
        var remaining = new List<NestingPart>(expandedParts);

        // Open panels lazily: open a new panel only when needed
        int nextPanelIdx = 0;

        while (remaining.Count > 0 && nextPanelIdx < orderedPanels.Count)
        {
            var panel = orderedPanels[nextPanelIdx++];
            var state = new PanelState(panel);
            panelStates.Add(state);

            // Place as many remaining parts on this panel as possible
            var stillRemaining = new List<NestingPart>();
            foreach (var part in remaining)
            {
                bool placed = TryPlaceBaf(part, state, input.SawBladeGapMm);
                if (!placed)
                    stillRemaining.Add(part);
            }
            remaining = stillRemaining;
        }

        var assignments = panelStates
            .Where(ps => ps.PlacedParts.Count > 0)
            .Select(ps => ps.ToAssignment())
            .ToList();

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
        return new NestingResult(assignments, remaining, totalWaste, assignments.Count, "Guillotine", sw.Elapsed);
    }

    /// <summary>
    /// Tries to place <paramref name="part"/> on <paramref name="panel"/> using Best Area Fit.
    /// Returns true if placed successfully.
    /// </summary>
    private static bool TryPlaceBaf(NestingPart part, PanelState panel, int gap)
    {
        // Try normal orientation then rotated; pick best area fit across both
        var (bestRect, bestRotated, bestScore) = FindBestFit(part, panel, gap);

        if (bestRect is null)
            return false;

        var w = bestRotated ? part.HeightMm : part.WidthMm;
        var h = bestRotated ? part.WidthMm  : part.HeightMm;

        // Place and split
        panel.PlacedParts.Add(new PlacedPart(part.PartId, part.Name, bestRect.X, bestRect.Y, w, h, bestRotated));
        panel.FreeRects.Remove(bestRect);
        SplitFreeRect(bestRect, w, h, gap, panel.FreeRects);

        return true;
    }

    private static (FreeRect? rect, bool rotated, decimal score) FindBestFit(
        NestingPart part, PanelState panel, int gap)
    {
        FreeRect? bestRect  = null;
        bool bestRotated    = false;
        decimal bestScore   = decimal.MaxValue;

        foreach (var fr in panel.FreeRects)
        {
            // Try normal
            if (part.WidthMm + gap <= fr.Width && part.HeightMm + gap <= fr.Height)
            {
                var score = fr.Width * fr.Height - part.WidthMm * part.HeightMm;
                if (score < bestScore)
                {
                    bestScore  = score;
                    bestRect   = fr;
                    bestRotated = false;
                }
            }
            // Try rotated
            if (part.CanRotate &&
                part.HeightMm + gap <= fr.Width && part.WidthMm + gap <= fr.Height)
            {
                var score = fr.Width * fr.Height - part.HeightMm * part.WidthMm;
                if (score < bestScore)
                {
                    bestScore  = score;
                    bestRect   = fr;
                    bestRotated = true;
                }
            }
        }

        return (bestRect, bestRotated, bestScore);
    }

    /// <summary>
    /// Guillotine split: after placing a part of size (w×h) at the top-left of <paramref name="used"/>,
    /// produces (up to) two new free rectangles using the Shorter Axis Rule for split orientation.
    /// </summary>
    private static void SplitFreeRect(FreeRect used, decimal w, decimal h, int gap, List<FreeRect> freeRects)
    {
        var rightW = used.Width  - w - gap;
        var topH   = used.Height - h - gap;

        // Shorter Axis Rule: split along the shorter remaining dimension
        bool horizontalSplit = rightW < topH;

        if (horizontalSplit)
        {
            // Right piece spans only the part's height row
            if (rightW > 0 && h > 0)
                freeRects.Add(new FreeRect(used.X + w + gap, used.Y, rightW, h));
            // Top piece spans the full original width
            if (topH > 0)
                freeRects.Add(new FreeRect(used.X, used.Y + h + gap, used.Width, topH));
        }
        else
        {
            // Right piece spans the full original height
            if (rightW > 0)
                freeRects.Add(new FreeRect(used.X + w + gap, used.Y, rightW, used.Height));
            // Top piece spans only the part's width column
            if (topH > 0 && w > 0)
                freeRects.Add(new FreeRect(used.X, used.Y + h + gap, w, topH));
        }
    }

    // ── Internal helpers ─────────────────────────────────────────────────────

    private sealed class FreeRect
    {
        public decimal X      { get; }
        public decimal Y      { get; }
        public decimal Width  { get; }
        public decimal Height { get; }

        public FreeRect(decimal x, decimal y, decimal width, decimal height)
        {
            X = x; Y = y; Width = width; Height = height;
        }
    }

    private sealed class PanelState
    {
        public AvailablePanel Panel { get; }
        public List<FreeRect> FreeRects { get; }
        public List<PlacedPart> PlacedParts { get; } = new();

        public PanelState(AvailablePanel panel)
        {
            Panel = panel;
            FreeRects = new List<FreeRect>
            {
                new(0m, 0m, panel.WidthMm, panel.HeightMm)
            };
        }

        public PanelAssignment ToAssignment() => new(
            Panel.PanelId,
            Panel.MaterialCode,
            Panel.WidthMm,
            Panel.HeightMm,
            PlacedParts)
        {
            WastePieces = FreeRects
                .Where(r => r.Width > 0 && r.Height > 0)
                .Select(r => new WastePiece(r.X, r.Y, r.Width, r.Height))
                .ToList()
        };
    }
}
