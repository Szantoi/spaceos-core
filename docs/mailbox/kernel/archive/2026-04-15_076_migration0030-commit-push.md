---
id: MSG-KERNEL-076
from: root
to: kernel
type: task
priority: high
status: READ
ref: MSG-INFRA-087-BLOCKED
created: 2026-04-15
---

# MSG-KERNEL-076 — Migration 0030 commit + push (INFRA-087 blokkoló)

## Probléma

INFRA-087 BLOCKED: a Migration 0030 fájlok lokálisan megvannak, de **nem kerültek
commitolva a develop branchre**. Remote HEAD még mindig `4cafceb`.

`git status` szerint uncommitted:
```
?? SpaceOS.Infrastructure/Migrations/20260415060000_Migration_0030_AddAuditEventSequence.cs
 M SpaceOS.Infrastructure/Data/Configurations/AuditEventConfiguration.cs
 M SpaceOS.Infrastructure/Data/Repositories/AuditEventRepository.cs
 M SpaceOS.Kernel.Domain/AuditLog/AuditEvent.cs
```

**Ráadásul hiányzik a `.Designer.cs` fájl** — ez Migration 0029-nél is blokkolt (MSG-INFRA-081 precedens).

## Feladat

### 1. Designer.cs + ModelSnapshot generálás

```bash
cd /opt/spaceos/SpaceOS.Kerner
dotnet ef migrations add AddAuditEventSequence \
  --project SpaceOS.Infrastructure \
  --startup-project SpaceOS.Kernel.Api \
  --context AppDbContext
```

⚠️ Ha az `AppDbContext` ignore-olja az `AuditEvent`-et (`modelBuilder.Ignore<AuditEvent>()`),
az EF tool nem fogja automatikusan generálni a Sequence column diff-et. Ebben az esetben:
- A meglévő `20260415060000_Migration_0030_AddAuditEventSequence.cs` marad (raw SQL tartalommal)
- Kézzel kell létrehozni a `20260415060000_Migration_0030_AddAuditEventSequence.Designer.cs` fájlt
  (mintaként: `20260327194934_InitialCreate.Designer.cs` — üres vagy minimal tartalommal OK)
- `AppDbContextModelSnapshot.cs` frissítése szükséges lehet

### 2. Commit + push

```bash
git add SpaceOS.Infrastructure/Migrations/20260415060000_Migration_0030_AddAuditEventSequence.cs
git add SpaceOS.Infrastructure/Migrations/20260415060000_Migration_0030_AddAuditEventSequence.Designer.cs
git add SpaceOS.Infrastructure/Data/Configurations/AuditEventConfiguration.cs
git add SpaceOS.Infrastructure/Data/Repositories/AuditEventRepository.cs
git add SpaceOS.Kernel.Domain/AuditLog/AuditEvent.cs
# Ha változott:
git add SpaceOS.Infrastructure/Data/AppDbContextModelSnapshot.cs

git commit -m "feat(kernel): AuditEvents sequence BIGINT IDENTITY tiebreaker (KERNEL-075)"
git push origin develop
```

### 3. Verify

```bash
git log --oneline -3  # új commit látható?
git show --stat HEAD  # tartalmaz minden fájlt?
```

## Elfogadási kritériumok (DoD)

- [ ] Új commit a develop branchen (push után)
- [ ] `Migration_0030_AddAuditEventSequence.cs` commitolva
- [ ] `Migration_0030_AddAuditEventSequence.Designer.cs` commitolva
- [ ] `AuditEvent.cs`, `AuditEventConfiguration.cs`, `AuditEventRepository.cs` commitolva
- [ ] `dotnet build` + `dotnet test` zöld (1110 pass)

## Megjegyzés

INFRA automatikusan folytatja a deploy-t miután a commit elérhető a remote develop branchen.
