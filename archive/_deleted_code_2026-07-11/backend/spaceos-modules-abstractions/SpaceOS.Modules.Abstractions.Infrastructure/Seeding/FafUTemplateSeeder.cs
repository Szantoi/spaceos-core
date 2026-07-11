using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Abstractions.Application.Seeding;
using SpaceOS.Modules.Abstractions.Domain.Aggregates;
using SpaceOS.Modules.Abstractions.Domain.Enums;
using SpaceOS.Modules.Abstractions.Infrastructure.Persistence;

namespace SpaceOS.Modules.Abstractions.Infrastructure.Seeding;

/// <summary>
/// Seeds the Doorstar FAF_Ü product template (fa ajtófélfa, üveges — glass variant).
/// Operation is idempotent — skipped if the template already exists for the tenant.
/// </summary>
public sealed class FafUTemplateSeeder(AbstractionsDbContext db) : ITemplateSeeder
{
    private static readonly Guid DoorstarTenantId =
        new("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

    private readonly AbstractionsDbContext _db = db;

    /// <inheritdoc />
    public async Task SeedAsync(CancellationToken ct = default)
    {
        await _db.Database.OpenConnectionAsync(ct).ConfigureAwait(false);
        try
        {
            await SeedInternalAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            await _db.Database.CloseConnectionAsync().ConfigureAwait(false);
        }
    }

    private async Task SeedInternalAsync(CancellationToken ct)
    {
        // DoorstarTenantId is a compile-time constant Guid — no injection risk.
#pragma warning disable EF1002
        await _db.Database.ExecuteSqlRawAsync(
            $"SET app.tenant_id = '{DoorstarTenantId}'", ct)
            .ConfigureAwait(false);
#pragma warning restore EF1002

        var exists = await _db.ProductTemplates
            .AnyAsync(t => t.TenantId == DoorstarTenantId && t.Name == "FAF_Ü", ct)
            .ConfigureAwait(false);

        if (exists) return;

        // 1. Create template
        var template = ProductTemplate.Create(DoorstarTenantId, "door", "FAF_Ü").Value;

        // 2. Set parameter
        template.SetParameter("CuttingOversize", 1.0m);

        // 3. Add slots
        var rootSlot      = template.AddSlot("Root",           "Root",      null,    null, 1, isVirtual: true,  semanticRole: null, sortOrder: 0).Value;
        var bkmPanelSlot  = template.AddSlot("BKM-panel",      "Panel",     "MDF",   18m,  1, isVirtual: false, semanticRole: null, sortOrder: 1).Value;
        var ajtoLapSlot   = template.AddSlot("Üveges-Ajtólap", "Door",      "MDF",   18m,  1, isVirtual: false, semanticRole: null, sortOrder: 2).Value;
        var uvegSlot      = template.AddSlot("Üveg",            "Glass",     null,    6m,   1, isVirtual: false, semanticRole: null, sortOrder: 3).Value;
        var frameCoreSlot = template.AddSlot("FrameCore-Alap",  "FrameCore", "Solid", 40m,  2, isVirtual: false, semanticRole: null, sortOrder: 4).Value;
        var ajtoElVSlot   = template.AddSlot("Ajtó-Él-V",       "Edge",      "ABS",   2m,   2, isVirtual: false, semanticRole: null, sortOrder: 5).Value;
        var ajtoElFSlot   = template.AddSlot("Ajtó-Él-F",       "Edge",      "ABS",   2m,   2, isVirtual: false, semanticRole: null, sortOrder: 6).Value;

        // 4. Add connections
        // Root → BKM-panel
        template.AddConnection(rootSlot.Id, bkmPanelSlot.Id,
            DimensionAxis.Width,  RuleOperator.Identity, 0m, null, null,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Design);
        template.AddConnection(rootSlot.Id, bkmPanelSlot.Id,
            DimensionAxis.Height, RuleOperator.Identity, 0m, null, null,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Design);

        // BKM-panel → Üveges-Ajtólap
        template.AddConnection(bkmPanelSlot.Id, ajtoLapSlot.Id,
            DimensionAxis.Width,  RuleOperator.Identity, 0m, null, null,
            JointType.Butt, MachiningOperation.Cut, ProcessPhase.Cutting);
        template.AddConnection(bkmPanelSlot.Id, ajtoLapSlot.Id,
            DimensionAxis.Height, RuleOperator.Subtract, 6m, null, null,
            JointType.Butt, MachiningOperation.Cut, ProcessPhase.Cutting);

        // Üveges-Ajtólap → Üveg
        template.AddConnection(ajtoLapSlot.Id, uvegSlot.Id,
            DimensionAxis.Width,  RuleOperator.Subtract, 120m, null, null,
            JointType.Offset, MachiningOperation.Cut, ProcessPhase.Cutting);
        template.AddConnection(ajtoLapSlot.Id, uvegSlot.Id,
            DimensionAxis.Height, RuleOperator.Subtract, 120m, null, null,
            JointType.Offset, MachiningOperation.Cut, ProcessPhase.Cutting);

        // Üveges-Ajtólap → FrameCore-Alap
        template.AddConnection(ajtoLapSlot.Id, frameCoreSlot.Id,
            DimensionAxis.Width,  RuleOperator.Subtract, 8m, null, null,
            JointType.Dado, MachiningOperation.Groove, ProcessPhase.CNC);
        template.AddConnection(ajtoLapSlot.Id, frameCoreSlot.Id,
            DimensionAxis.Height, RuleOperator.Subtract, 4m, null, null,
            JointType.Dado, MachiningOperation.Groove, ProcessPhase.CNC);

        // Üveges-Ajtólap → Ajtó-Él-V
        template.AddConnection(ajtoLapSlot.Id, ajtoElVSlot.Id,
            DimensionAxis.Height, RuleOperator.Identity, 0m, null, null,
            JointType.EdgeBand, MachiningOperation.EdgeBand, ProcessPhase.EdgeBanding);
        template.AddConnection(ajtoLapSlot.Id, ajtoElVSlot.Id,
            DimensionAxis.Depth,  RuleOperator.Constant, 2m, null, null,
            JointType.EdgeBand, MachiningOperation.EdgeBand, ProcessPhase.EdgeBanding);

        // Üveges-Ajtólap → Ajtó-Él-F
        template.AddConnection(ajtoLapSlot.Id, ajtoElFSlot.Id,
            DimensionAxis.Width,  RuleOperator.Identity, 0m, null, null,
            JointType.EdgeBand, MachiningOperation.EdgeBand, ProcessPhase.EdgeBanding);
        template.AddConnection(ajtoLapSlot.Id, ajtoElFSlot.Id,
            DimensionAxis.Depth,  RuleOperator.Constant, 2m, null, null,
            JointType.EdgeBand, MachiningOperation.EdgeBand, ProcessPhase.EdgeBanding);

        // 5. Persist
        await _db.ProductTemplates.AddAsync(template, ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
