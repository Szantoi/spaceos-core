---
id: MSG-FREETIER-003
from: root
to: freetier
type: task
priority: high
status: READ
ref: MSG-FREETIER-001
created: 2026-04-20
---

# FREETIER-003 — Persistence réteg (Nap 3.0–5.0)

> **Skill:** `/spaceos-terminal` szerint dolgozz — inbox → build → test → outbox DONE
> **Spec:** `docs/architecture/SpaceOS_FreeTier_Architecture_v4.md` Section 3, 4, D-21..D-26
> **Timeline:** ~2 nap (Nap 3.0..5.0)
> **Blokkoló:** FREETIER-001 (Nap 1.0–2.5) ✅ DONE kell előtte
> **Infra:** Redis + PostgreSQL `spaceos_freetier` LIVE ✅ (`/etc/spaceos/freetier.env`)

---

## Nap 3.0 — FreeTierDbContext + EF konfigurációk

**Fájl:** `Infrastructure/Persistence/FreeTierDbContext.cs`

```csharp
public class FreeTierDbContext : DbContext
{
    public DbSet<FreeTierUser> Users => Set<FreeTierUser>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<WorkspaceRevision> WorkspaceRevisions => Set<WorkspaceRevision>();
    public DbSet<ShareToken> ShareTokens => Set<ShareToken>();
    public DbSet<MagicLinkToken> MagicLinkTokens => Set<MagicLinkToken>();
    public DbSet<UpgradeRequest> UpgradeRequests => Set<UpgradeRequest>();
}
```

**JSONB string mapping (D-21):** `NestingInput` value object-et `jsonb` oszlopként tárolni:
```csharp
// EF config-ban:
builder.Property(e => e.NestingInput)
    .HasConversion(v => v.ToJson(), v => NestingInput.FromJson(v))
    .HasColumnType("jsonb");
// NE használj OwnsOne().ToJson() — az EF8 JSON columns funkció Npgsql-lel nem kompatibilis megbízhatóan
```

**UserSessionInterceptor (BE-22):** Pool poison fix — try/catch-be csomagolva, exception nem propagál:
```csharp
// Kernel TenantSessionInterceptor mintájára:
// docs/knowledge/patterns/DATABASE_PATTERNS.md — GUC pattern
// spaceos-modules-cutting/Infrastructure/Data/ — referencia implementáció
```

**ShareDbContext:** Külön `IDbContextFactory<ShareDbContext>` — csak olvasás, `spaceos_freetier_share_reader` role-lal fut, nincs migration.

**Tesztek (+5):** DbContext példányosítás, JSONB round-trip, ShareDbContext factory

---

## Nap 3.5 — Migration F_0001_InitialSchema (raw SQL)

**FONTOS:** EF Core `dotnet ef migrations add` futtatása kötelező — manuális migration írás TILOS.

```bash
dotnet ef migrations add F_0001_InitialSchema \
  --project SpaceOS.FreeTier.Infrastructure \
  --startup-project SpaceOS.FreeTier.Api
```

Ezután a generált migration `Up()` metódusát **kiegészíteni** raw SQL-lel (D-24 + RLS):

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // EF által generált tábla DDL meghagyva

    // 1. AuthenticatedAt mező (SEC-15) — ha EF nem generálta
    migrationBuilder.Sql(@"
        ALTER TABLE freetier.""Users""
        ADD COLUMN IF NOT EXISTS ""AuthenticatedAt"" TIMESTAMPTZ NULL;
    ");

    // 2. TokenHash/TokenPrefix (D-13-REV)
    migrationBuilder.Sql(@"
        ALTER TABLE freetier.""ShareTokens""
        RENAME COLUMN IF EXISTS ""Token"" TO ""TokenHash"";
        ALTER TABLE freetier.""ShareTokens""
        ADD COLUMN IF NOT EXISTS ""TokenPrefix"" VARCHAR(8) NOT NULL DEFAULT '';
    ");

    // 3. DEFERRABLE FK circular reference (D-24)
    migrationBuilder.Sql(@"
        ALTER TABLE freetier.""Workspaces""
        ADD CONSTRAINT ""FK_Workspaces_CurrentRevision"" FOREIGN KEY (""CurrentRevisionId"")
        REFERENCES freetier.""WorkspaceRevisions""(""Id"")
        DEFERRABLE INITIALLY DEFERRED;
    ", suppressTransaction: true);

    // 4. RLS bekapcsolás
    migrationBuilder.Sql(@"
        ALTER TABLE freetier.""Users"" ENABLE ROW LEVEL SECURITY;
        ALTER TABLE freetier.""Workspaces"" ENABLE ROW LEVEL SECURITY;
        ALTER TABLE freetier.""WorkspaceRevisions"" ENABLE ROW LEVEL SECURITY;
        ALTER TABLE freetier.""ShareTokens"" ENABLE ROW LEVEL SECURITY;
        ALTER TABLE freetier.""MagicLinkTokens"" ENABLE ROW LEVEL SECURITY;

        CREATE POLICY freetier_user_isolation ON freetier.""Users""
            USING (""Id"" = current_setting('app.current_user_id', true)::uuid);
        -- (többi policy spec Section 4 szerint)
    ");

    // 5. spaceos_freetier_share_reader GRANT
    migrationBuilder.Sql(@"
        GRANT SELECT ON freetier.""Workspaces"" TO spaceos_freetier_share_reader;
        GRANT SELECT ON freetier.""WorkspaceRevisions"" TO spaceos_freetier_share_reader;
        GRANT SELECT ON freetier.""ShareTokens"" TO spaceos_freetier_share_reader;
    ");
}
```

```bash
dotnet ef database update \
  --project SpaceOS.FreeTier.Infrastructure \
  --startup-project SpaceOS.FreeTier.Api
```

**Tesztek (+3):** migration fut, tábla létezik, RLS be van kapcsolva

---

## Nap 4.0 — RLS izolációs tesztek

**Fájl:** `Integration.Tests/RlsIsolationTests.cs`

Testcontainerst használj (PostgreSQL), ne mock-ot.

Min. 10 teszt:
- User A nem látja User B workspace-ét (5 tábla mindegyikén)
- DEFERRABLE FK: circular insert tranzakción belül sikeres
- `app.current_user_id` GUC beállítás nélkül → üres eredmény (nem 500)

**Referencia:** `docs/knowledge/patterns/DATABASE_PATTERNS.md` — RLS + GUC + Testcontainers

---

## Nap 4.5 — SaveChangesAsync override (D-22)

**Fájl:** `Infrastructure/Persistence/FreeTierDbContext.cs`

```csharp
public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
{
    var result = await base.SaveChangesAsync(ct);
    var events = ChangeTracker.Entries<IDomainEventContainer>()
        .SelectMany(e => e.Entity.PopDomainEvents())
        .ToList();
    foreach (var evt in events)
        await _mediator.Publish(evt.ToNotification(), ct);
    return result;
}
```

`QueryTrackingBehavior.NoTracking` alapértelmezetten a DbContext-en.

**Tesztek (+5):** domain event dispatch SaveChanges-kor, NoTracking alapértelmezés

---

## Nap 5.0 — Testcontainers base + FakeClock + IClock DI

**Fájl:** `Integration.Tests/Infrastructure/FreeTierTestBase.cs`

```csharp
public abstract class FreeTierTestBase : IAsyncLifetime
{
    protected PostgreSqlContainer Pg { get; }
    protected RedisContainer Redis { get; }
    protected IServiceProvider Services { get; private set; }
    // WebApplicationFactory setup + FakeClock DI
}
```

**FakeClock:** `IClock` test implementáció — beállítható `UtcNow`.

**Tesztek (+7):** Testcontainers indul/leáll, FakeClock inject, DB suite futtat

---

## Definition of Done

- [ ] `FreeTierDbContext` + entity configs + JSONB mapping (D-21)
- [ ] `UserSessionInterceptor` try/catch (BE-22)
- [ ] `ShareDbContext` factory
- [ ] Migration `F_0001_InitialSchema` — `dotnet ef migrations add` (nem manuális!)
- [ ] Raw SQL kiegészítések: DEFERRABLE FK + RLS + share_reader GRANT
- [ ] `dotnet ef database update` sikeres
- [ ] RLS izolációs tesztek (min. 10)
- [ ] `SaveChangesAsync` domain event dispatch (D-22)
- [ ] `QueryTrackingBehavior.NoTracking`
- [ ] Testcontainers base + FakeClock
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` ≥72 pass (42 előző + 30 új)
- [ ] Outbox DONE üzenet küldve
