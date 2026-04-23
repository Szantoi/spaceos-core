---
id: MSG-J038
from: architect
to: kernel
type: task
date: 2026-04-09
priority: P0
---

# Modules.Joinery — Startup Seeder + Health + Seed Validation Tests

## Kontextus

Doorstar Onboarding DoD hiányzó elemek a `spaceos-modules-joinery` repóban.

## Feladat 1 — Startup Seeder (SEC-06)

`/opt/spaceos/spaceos-modules-joinery/SpaceOS.Modules.Joinery.Api/Program.cs`-ben az `app.Run()` elé:

```csharp
app.Lifetime.ApplicationStarted.Register(async () =>
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
    await seeder.SeedAsync(CancellationToken.None).ConfigureAwait(false);

    var db = scope.ServiceProvider.GetRequiredService<JoineryDbContext>();
    var count = await db.DoorTypeRules.CountAsync().ConfigureAwait(false);
    if (count == 0)
        throw new InvalidOperationException("FATAL: DoorTypeRules seed empty — startup aborted");
});
```

## Feladat 2 — Health endpoint

`Program.cs`-ben, a többi endpoint mellé:

```csharp
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "spaceos-joinery" }))
   .AllowAnonymous();
```

## Feladat 3 — Seed validation tesztek (BE-03)

`/opt/spaceos/spaceos-modules-joinery/SpaceOS.Modules.Joinery.Tests/Seeding/DoorstarSeedDataTests.cs`

```csharp
public class DoorstarSeedDataTests
{
    private static readonly string[] ValidComponentTypes =
        ["Frame", "Insert", "Clad", "FrameCore", "Blende", "Coating"];

    [Fact]
    public void AllPartDimensionRules_HaveValidComponentType()
    {
        Assert.All(DoorstarSeedData.PartDimensionRules,
            r => Assert.Contains(r.ComponentType, ValidComponentTypes));
    }

    [Fact]
    public void GlobalConstants_ContainRequiredKeys()
    {
        var required = new[] { "CuttingOversize", "CladdingOverhang", "MatyiWidth" };
        var keys = DoorstarSeedData.Constants.Select(c => c.Key).ToArray();
        Assert.All(required, k => Assert.Contains(k, keys));
    }

    [Fact]
    public void DoorTypeRules_NotEmpty()
    {
        Assert.NotEmpty(DoorstarSeedData.DoorTypeRules);
    }

    [Fact]
    public void ProcessTasks_NotEmpty()
    {
        Assert.NotEmpty(DoorstarSeedData.ProcessTasks);
    }
}
```

Megjegyzés: Ha `PartDimensionRules` üres a seedben (nincs még töltve), az első teszt trivially pass. OK.

## Ellenőrzés

```bash
dotnet build /opt/spaceos/spaceos-modules-joinery/ 2>&1 | tail -3
dotnet test /opt/spaceos/spaceos-modules-joinery/ --no-build 2>&1 | tail -5
```

0 error, 32+ teszt zöld (28 meglévő + 4 új).

## Output

Ha kész: `/opt/spaceos/docs/mailbox/kernel/outbox/2026-04-09_038_joinery-improvements-done.md`
