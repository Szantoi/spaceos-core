using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Joinery.Application.Seeding;
using SpaceOS.Modules.Joinery.Infrastructure.Persistence;

namespace SpaceOS.Modules.Joinery.Infrastructure.Seeding;

/// <summary>
/// Seeds the Doorstar reference tables (GlobalConstants, DoorTypeRules, ProcessTaskTemplates)
/// using raw SQL with ON CONFLICT DO NOTHING for idempotency.
/// </summary>
public sealed class DoorRulesDataSeeder(JoineryDbContext db) : IDataSeeder
{
    /// <inheritdoc/>
    public async Task SeedAsync(CancellationToken ct = default)
    {
        // GlobalConstants is seeded via migration (0002) — spaceos user has SELECT-only
        await SeedDoorTypeRulesAsync(ct).ConfigureAwait(false);
        await SeedProcessTaskTemplatesAsync(ct).ConfigureAwait(false);
    }

    private async Task SeedDoorTypeRulesAsync(CancellationToken ct)
    {
        foreach (var r in DoorstarSeedData.DoorTypeRules)
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO spaceos_joinery."DoorTypeRules"
                    ("DoorType", "AjtólapCount", "BkmWidthFixed", "BkmHeightFixed", "BkmWidthMoving", "BkmHeightMoving")
                VALUES
                    ({r.DoorType}, {r.AjtólapCount}, {r.BkmWidthFixed}, {r.BkmHeightFixed}, {r.BkmWidthMoving}, {r.BkmHeightMoving})
                ON CONFLICT ("DoorType") DO NOTHING
                """,
                ct).ConfigureAwait(false);
        }
    }

    private async Task SeedProcessTaskTemplatesAsync(CancellationToken ct)
    {
        foreach (var t in DoorstarSeedData.ProcessTasks)
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO spaceos_joinery."ProcessTaskTemplates"
                    ("TaskId", "ShortName", "Description", "Department", "UnitTimeSec", "Headcount", "ParentTaskId")
                VALUES
                    ({t.TaskId}, {t.ShortName}, {t.Description}, {t.Department}, {t.UnitTimeSec}, {t.Headcount}, {t.ParentTaskId})
                ON CONFLICT ("TaskId") DO NOTHING
                """,
                ct).ConfigureAwait(false);
        }
    }
}
