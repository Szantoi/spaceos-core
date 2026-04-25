---
id: MSG-FREETIER-013
from: root
to: freetier
type: task
priority: medium
status: UNREAD
created: 2026-04-25
---

# FREETIER-013 — Nesting.Algorithms NuGet pack (tech debt)

> A FreeTier API jelenleg ProjectReference-szel használja a Nesting.Algorithms-t. Ez NuGet csomagba kell kerüljön a proper package management-hez.
> **Skill:** `/spaceos-terminal` szerint dolgozz

## Feladat

1. **NuGet pack** a spaceos-nesting-algorithms projektből:

```bash
cd /opt/spaceos/spaceos-nesting-algorithms
dotnet pack -c Release -o /opt/spaceos/local-nuget/
```

2. **nuget.config** — lokális feed hozzáadás a FreeTier API-hoz:

```xml
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="local-spaceos" value="/opt/spaceos/local-nuget/" />
  </packageSources>
</configuration>
```

3. **ProjectReference → PackageReference** csere a FreeTier Infrastructure.csproj-ban:

```xml
<!-- VOLT: -->
<ProjectReference Include="../../../spaceos-nesting-algorithms/..." />
<!-- KELL: -->
<PackageReference Include="SpaceOS.Nesting.Algorithms" Version="1.1.0" />
```

4. **Build + test** — minden zöld marad

## Definition of Done

- [ ] NuGet .nupkg létrehozva `/opt/spaceos/local-nuget/`-ban
- [ ] FreeTier Infrastructure → PackageReference
- [ ] `dotnet build` 0 error
- [ ] `dotnet test` ≥ 176 pass
- [ ] Outbox DONE
