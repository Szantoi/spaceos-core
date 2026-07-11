using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Abstractions.Application.Seeding;
using SpaceOS.Modules.Abstractions.Domain.Aggregates;
using SpaceOS.Modules.Abstractions.Domain.Enums;
using SpaceOS.Modules.Abstractions.Infrastructure.Persistence;

namespace SpaceOS.Modules.Abstractions.Infrastructure.Seeding;

/// <summary>
/// Seeds the Doorstar FAF_T product template (Phase B-Manufacturing T4).
/// Operation is idempotent — skipped if the template already exists for the tenant.
/// </summary>
public sealed class FafTTemplateSeeder(AbstractionsDbContext db) : ITemplateSeeder
{
    private static readonly Guid DoorstarTenantId =
        new("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

    private readonly AbstractionsDbContext _db = db;

    /// <inheritdoc />
    public async Task SeedAsync(CancellationToken ct = default)
    {
        // Pin a single physical connection for the entire seed operation.
        // SET app.tenant_id is session-scoped — it would be lost if EF uses a
        // different pooled connection for the subsequent query/save.
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
            .AnyAsync(t => t.TenantId == DoorstarTenantId && t.Name == "FAF_T", ct)
            .ConfigureAwait(false);

        if (exists) return;

        // 1. Create template
        var template = ProductTemplate.Create(DoorstarTenantId, "door", "FAF_T").Value;

        // 2. Set parameter
        template.SetParameter("CuttingOversize", 1.0m);

        // 3. Add slots
        var rootSlot          = template.AddSlot("Root",         "Root",      null,     null, 1, isVirtual: true,  semanticRole: null, sortOrder: 0).Value;
        var bkmPanelSlot      = template.AddSlot("BKM-panel",    "Panel",     "MDF",    18m,  1, isVirtual: false, semanticRole: null, sortOrder: 1).Value;
        var ajtólapSlot       = template.AddSlot("Ajtólap",      "Door",      "MDF",    18m,  1, isVirtual: false, semanticRole: null, sortOrder: 2).Value;
        var frameCoreAlapSlot = template.AddSlot("FrameCore-Alap","FrameCore", "Solid",  40m,  2, isVirtual: false, semanticRole: null, sortOrder: 3).Value;
        var ajtóÉlVSlot       = template.AddSlot("Ajtó-Él-V",    "Edge",      "ABS",    2m,   2, isVirtual: false, semanticRole: null, sortOrder: 4).Value;
        var ajtóÉlFSlot       = template.AddSlot("Ajtó-Él-F",    "Edge",      "ABS",    2m,   2, isVirtual: false, semanticRole: null, sortOrder: 5).Value;

        // 4. Add connections
        // Root → BKM-panel
        template.AddConnection(rootSlot.Id, bkmPanelSlot.Id,
            DimensionAxis.Width,  RuleOperator.Identity, 0m, null, null,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Design);

        template.AddConnection(rootSlot.Id, bkmPanelSlot.Id,
            DimensionAxis.Height, RuleOperator.Identity, 0m, null, null,
            JointType.Offset, MachiningOperation.None, ProcessPhase.Design);

        // BKM-panel → Ajtólap
        template.AddConnection(bkmPanelSlot.Id, ajtólapSlot.Id,
            DimensionAxis.Width,  RuleOperator.Identity, 0m, null, null,
            JointType.Butt, MachiningOperation.Cut, ProcessPhase.Cutting);

        template.AddConnection(bkmPanelSlot.Id, ajtólapSlot.Id,
            DimensionAxis.Height, RuleOperator.Subtract, 6m, null, null,
            JointType.Butt, MachiningOperation.Cut, ProcessPhase.Cutting);

        // Ajtólap → FrameCore-Alap
        template.AddConnection(ajtólapSlot.Id, frameCoreAlapSlot.Id,
            DimensionAxis.Width,  RuleOperator.Subtract, 8m, null, null,
            JointType.Dado, MachiningOperation.Groove, ProcessPhase.CNC);

        template.AddConnection(ajtólapSlot.Id, frameCoreAlapSlot.Id,
            DimensionAxis.Height, RuleOperator.Subtract, 4m, null, null,
            JointType.Dado, MachiningOperation.Groove, ProcessPhase.CNC);

        // Ajtólap → Ajtó-Él-V
        template.AddConnection(ajtólapSlot.Id, ajtóÉlVSlot.Id,
            DimensionAxis.Height, RuleOperator.Identity, 0m, null, null,
            JointType.EdgeBand, MachiningOperation.EdgeBand, ProcessPhase.EdgeBanding);

        template.AddConnection(ajtólapSlot.Id, ajtóÉlVSlot.Id,
            DimensionAxis.Depth,  RuleOperator.Constant, 2m, null, null,
            JointType.EdgeBand, MachiningOperation.EdgeBand, ProcessPhase.EdgeBanding);

        // Ajtólap → Ajtó-Él-F
        template.AddConnection(ajtólapSlot.Id, ajtóÉlFSlot.Id,
            DimensionAxis.Width,  RuleOperator.Identity, 0m, null, null,
            JointType.EdgeBand, MachiningOperation.EdgeBand, ProcessPhase.EdgeBanding);

        template.AddConnection(ajtólapSlot.Id, ajtóÉlFSlot.Id,
            DimensionAxis.Depth,  RuleOperator.Constant, 2m, null, null,
            JointType.EdgeBand, MachiningOperation.EdgeBand, ProcessPhase.EdgeBanding);

        // 5. Persist
        await _db.ProductTemplates.AddAsync(template, ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
