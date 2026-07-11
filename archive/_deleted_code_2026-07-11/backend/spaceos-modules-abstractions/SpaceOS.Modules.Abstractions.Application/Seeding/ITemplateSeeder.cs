namespace SpaceOS.Modules.Abstractions.Application.Seeding;

/// <summary>
/// Contract for seeding product templates into the data store.
/// </summary>
public interface ITemplateSeeder
{
    /// <summary>
    /// Seeds the template data, idempotently.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task SeedAsync(CancellationToken ct = default);
}
