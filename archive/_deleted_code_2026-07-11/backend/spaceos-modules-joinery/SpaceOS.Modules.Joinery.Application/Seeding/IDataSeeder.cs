namespace SpaceOS.Modules.Joinery.Application.Seeding;

public interface IDataSeeder
{
    Task SeedAsync(CancellationToken ct = default);
}
