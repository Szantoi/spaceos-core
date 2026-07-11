// SpaceOS.Modules.FlowManagement/Services/FlowNodeResolver.cs
using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.FlowManagement.Infrastructure;

namespace SpaceOS.Modules.FlowManagement.Services;

/// <summary>
/// Resolves the level and identity of a flow node by searching all node collections
/// in the FlowManagement hierarchy (Task → Milestone → Project → Program).
/// </summary>
public sealed class FlowNodeResolver
{
    private readonly ModulesDbContext _db;

    /// <summary>
    /// Initialises a new instance of <see cref="FlowNodeResolver"/>.
    /// </summary>
    /// <param name="db">The modules database context.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="db"/> is <c>null</c>.</exception>
    public FlowNodeResolver(ModulesDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
    }

    /// <summary>
    /// Resolves the level and identifier of a flow node with the given <paramref name="id"/>.
    /// Searches FlowTasks, then FlowMilestones, then FlowProjects, then FlowPrograms.
    /// </summary>
    /// <param name="id">The identifier to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A tuple of <c>(Level, Id)</c> where <c>Level</c> is one of "Task", "Milestone", "Project",
    /// "Program"; or <c>null</c> if no matching node is found.
    /// </returns>
    public async Task<(string Level, Guid Id)?> ResolveAsync(Guid id, CancellationToken ct)
    {
        if (await _db.FlowTasks.AsNoTracking().AnyAsync(t => t.Id == id, ct).ConfigureAwait(false))
            return ("Task", id);

        if (await _db.FlowMilestones.AsNoTracking().AnyAsync(m => m.Id == id, ct).ConfigureAwait(false))
            return ("Milestone", id);

        if (await _db.FlowProjects.AsNoTracking().AnyAsync(p => p.Id == id, ct).ConfigureAwait(false))
            return ("Project", id);

        if (await _db.FlowPrograms.AsNoTracking().AnyAsync(p => p.Id == id, ct).ConfigureAwait(false))
            return ("Program", id);

        return null;
    }
}
