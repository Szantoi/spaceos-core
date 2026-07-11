// SpaceOS.Infrastructure/Data/Repositories/ModuleSubscriptionRepository.cs

using Microsoft.EntityFrameworkCore;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Repositories;

namespace SpaceOS.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IModuleSubscriptionRepository"/>.
/// </summary>
internal sealed class ModuleSubscriptionRepository : IModuleSubscriptionRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initialises a new <see cref="ModuleSubscriptionRepository"/>.
    /// </summary>
    /// <param name="context">The application database context.</param>
    public ModuleSubscriptionRepository(AppDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc/>
    public async Task AddAsync(ModuleSubscription subscription, CancellationToken ct = default)
    {
        await _context.ModuleSubscriptions.AddAsync(subscription, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ModuleSubscription>> GetActiveByEventTypeAsync(
        string eventType,
        CancellationToken ct = default)
    {
        return await _context.ModuleSubscriptions
            .AsNoTracking()
            .Where(s => s.IsActive && s.EventType == eventType)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<ModuleSubscription?> GetBySubscriberAndEventAsync(
        string subscriberModule,
        string eventType,
        CancellationToken ct = default)
    {
        return await _context.ModuleSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.SubscriberModule == subscriberModule && s.EventType == eventType,
                ct)
            .ConfigureAwait(false);
    }
}
