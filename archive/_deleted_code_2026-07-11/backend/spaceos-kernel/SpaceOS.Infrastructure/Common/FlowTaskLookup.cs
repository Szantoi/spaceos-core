// SpaceOS.Infrastructure/Common/FlowTaskLookup.cs
using Microsoft.EntityFrameworkCore;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Modules.FlowManagement.Infrastructure;

namespace SpaceOS.Infrastructure.Common;

/// <summary>
/// Implements <see cref="IFlowTaskLookup"/> by querying the FlowManagement module's
/// <see cref="ModulesDbContext"/> to resolve FlowTask tenant ownership without
/// a direct domain-layer dependency on the module.
/// </summary>
public class FlowTaskLookup : IFlowTaskLookup
{
    private readonly ModulesDbContext _modulesDb;

    /// <summary>
    /// Initialises a new <see cref="FlowTaskLookup"/>.
    /// </summary>
    /// <param name="modulesDb">The modules database context.</param>
    public FlowTaskLookup(ModulesDbContext modulesDb)
    {
        ArgumentNullException.ThrowIfNull(modulesDb);
        _modulesDb = modulesDb;
    }

    /// <inheritdoc/>
    public async Task<Guid?> GetTenantIdAsync(Guid flowTaskId, CancellationToken ct)
    {
        var task = await _modulesDb.FlowTasks
            .AsNoTracking()
            .Where(t => t.Id == flowTaskId)
            .Select(t => new { t.TenantId })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return task?.TenantId;
    }
}
