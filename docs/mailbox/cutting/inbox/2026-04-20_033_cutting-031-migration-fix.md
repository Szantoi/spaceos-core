---
id: MSG-CUTTING-033
from: root
to: cutting
type: task
priority: high
status: READ
ref: MSG-CUTTING-031
created: 2026-04-20
---

# CUTTING-033 — CUTTING-031 migration javítás: dotnet ef kötelező

> **FONTOS:** A CUTTING-031 migration manuálisan lett megírva. Ez NEM elfogadható.
> A `dotnet ef` tool megkerülése tilos — a tool a szerveren telepítve van.

---

## Probléma

A `20260420000001_CuttingPlanStatusToEnum.cs` migration kézzel lett megírva, mert a terminálban nem volt elérhető a `dotnet-ef` tool. Ez kockázatos:
- Az EF Core snapshot (`CuttingDbContextModelSnapshot.cs`) szinkronban kell legyen a migration-nel
- Manuálisan írt migration könnyen eltér attól amit az EF generálna
- Jövőbeli `dotnet ef migrations add` hibás diff-et generálna

---

## Feladatok

### 1. dotnet-ef tool telepítése (ha hiányzik)

```bash
dotnet tool install --global dotnet-ef
# vagy ha már van globálisan:
dotnet tool list --global | grep dotnet-ef
```

Ha nem globálisan, hanem lokálisan (tool manifest):
```bash
dotnet new tool-manifest  # ha nincs még .config/dotnet-tools.json
dotnet tool install dotnet-ef
dotnet tool restore
```

### 2. Manuális migration törlése

```bash
# Töröld a kézzel írt migration fájlokat:
rm Migrations/20260420000001_CuttingPlanStatusToEnum.cs
rm Migrations/20260420000001_CuttingPlanStatusToEnum.Designer.cs
# A snapshot is visszaállítandó az előző állapotra (git checkout vagy kézi visszaírás)
```

### 3. Migration újragenerálása dotnet ef-fel

```bash
dotnet ef migrations add CuttingPlanStatusToEnum \
  --project SpaceOS.Modules.Cutting.Infrastructure \
  --startup-project SpaceOS.Modules.Cutting.Api
```

### 4. Ellenőrzés

- A generált migration SQL-je egyezzen a specifikált CASE konverzióval
- `dotnet build` 0 error, 0 warning
- `dotnet test` 195/195 zöld

---

## Definition of Done

- [ ] `dotnet ef migrations add` sikeresen lefutott
- [ ] Migration fájlok az EF által generáltak (nem kézzel írtak)
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` 195/195 zöld
- [ ] Outbox DONE üzenet küldve
