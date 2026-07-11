using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Joinery.Application.Orders.Repositories;
using SpaceOS.Modules.Joinery.Domain.Rules;

namespace SpaceOS.Modules.Joinery.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IDoorRulesRepository"/> backed by <see cref="JoineryDbContext"/>.
/// </summary>
public sealed class DoorRulesRepository(JoineryDbContext db) : IDoorRulesRepository
{
    /// <inheritdoc/>
    public async Task<DoorTypeRule?> GetDoorTypeRuleAsync(string doorType, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(doorType))
            return null;

        return await db.DoorTypeRules
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.DoorType == doorType, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PartDimensionRule>> GetPartDimensionRulesAsync(string doorType, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(doorType))
            return [];

        return await db.PartDimensionRules
            .AsNoTracking()
            .Where(r => r.DoorType == doorType)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<GlobalConstant>> GetGlobalConstantsAsync(CancellationToken ct)
        => await db.GlobalConstants
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ProcessTaskTemplate>> GetProcessTaskTemplatesAsync(CancellationToken ct)
        => await db.ProcessTaskTemplates
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);
}
