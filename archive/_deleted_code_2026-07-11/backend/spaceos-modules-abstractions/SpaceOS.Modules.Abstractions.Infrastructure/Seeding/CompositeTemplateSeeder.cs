using SpaceOS.Modules.Abstractions.Application.Seeding;

namespace SpaceOS.Modules.Abstractions.Infrastructure.Seeding;

/// <summary>
/// Composite seeder that runs all product template seeders in dependency order.
/// </summary>
public sealed class CompositeTemplateSeeder(
    FafTTemplateSeeder fafT,
    FafUTemplateSeeder fafU,
    BfajTemplateSeeder bfaj) : ITemplateSeeder
{
    /// <inheritdoc />
    public async Task SeedAsync(CancellationToken ct = default)
    {
        await fafT.SeedAsync(ct).ConfigureAwait(false);
        await fafU.SeedAsync(ct).ConfigureAwait(false);
        await bfaj.SeedAsync(ct).ConfigureAwait(false);
    }
}
