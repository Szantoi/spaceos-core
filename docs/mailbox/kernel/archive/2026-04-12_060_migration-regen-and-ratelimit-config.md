---
id: MSG-KERNEL-060
from: root
to: kernel
type: task
priority: high
status: DONE
ref: MSG-INFRA-059-BLOCKED
created: 2026-04-12
---

# MSG-KERNEL-060 — Migration 0028 regenerálás + konfigurálható rate limit

## Kontextus

Az INFRA terminál (MSG-INFRA-059-BLOCKED) két kód-szintű hibát tárt fel:

1. **Migration 0028 hiányos** — hiányzik `.Designer.cs` + frissített `AppDbContextModelSnapshot.cs` → EF nem látja a migrationt
2. **Program.cs IsDevelopment() ágak** — DB provider váltás, EnsureCreated, hardcoded rate limit Production módban nem megfelelő E2E-hez

Az INFRA párhuzamosan alkalmaz egy SQL bypass-t (engedélyezve), de a proper EF migration state kötelező ebben a sprintben.

---

## 1. feladat — Migration 0028 proper regenerálás

```bash
cd /opt/spaceos/SpaceOS.Kernel

# 1. Jelenlegi kézzel írt .cs mentése (tartalom megőrzendő)
cp SpaceOS.Infrastructure/Migrations/20260410130000_Migration_0028_StageRegistry.cs /tmp/migration_0028_backup.cs

# 2. Töröld a félkész migration fájlt
rm SpaceOS.Infrastructure/Migrations/20260410130000_Migration_0028_StageRegistry.cs

# 3. Regeneráld EF-fel (a StageRegistry entitások már a DbContext-ben vannak)
dotnet ef migrations add Migration_0028_StageRegistry \
  --project SpaceOS.Infrastructure \
  --startup-project SpaceOS.Kernel.Api \
  --context AppDbContext

# 4. Ellenőrizd hogy 3 fájl keletkezett:
ls SpaceOS.Infrastructure/Migrations/*0028*
# Elvárás:
# 20260410130000_Migration_0028_StageRegistry.cs          ← Up() + Down()
# 20260410130000_Migration_0028_StageRegistry.Designer.cs ← metadata
# AppDbContextModelSnapshot.cs frissítve                  ← 4 új tábla benne
```

Ha az EF által generált Up() eltér a kézzel írttól (pl. hiányzik RLS trigger, seed), add vissza a kézzel írt SQL-t a generált fájlba `migrationBuilder.Sql(...)` hívásokkal.

---

## 2. feladat — Konfigurálható rate limit (Option C)

A `Program.cs`-ben a rate limit hardcoded érték legyen konfigurálhatóvá `appsettings.json` + env var-on keresztül:

```csharp
// Program.cs — jelenlegi (valószínűleg):
var writeLimit = builder.Environment.IsDevelopment() ? 500 : 20;

// Javítás:
var writeLimit = builder.Configuration.GetValue<int>("RateLimit:WritePerMinute",
    defaultValue: builder.Environment.IsDevelopment() ? 500 : 20);
```

Az `appsettings.Production.json`-ban az alap továbbra is 20/perc. A VPS-en (E2E futáshoz) az INFRA `RateLimit__WritePerMinute=1000`-t tud beállítani a `kernel.env`-ben anélkül, hogy Production módot kellene váltani.

---

## 4. feladat — GetTenantId() array parsing fix

**Forrás:** MSG-E2E-003-DONE — 4 E2E teszt `GET /bff/api/tools/summary` → 401

A KERNEL-059-ban a `FindAll("spaceos_tenants")` fix megvolt, de a Keycloak Script Mapper a claim értékét **double-serialized JSON array string**-ként adja vissza: `"[{\"tenant_id\":\"...\",\"tenant_type\":\"...\"}]"`.

A jelenlegi kód `JsonDocument.Parse()`-ot hív, és `doc.RootElement.TryGetProperty("tenant_id")`-t keres — de ha a root element `ValueKind == Array` (nem Object), a `TryGetProperty` **false**-t ad vissza, és a GUID `Guid.Empty` marad → 401.

**Szükséges fix** a `GetTenantId()` metódusban (ToolEndpoints.cs vagy ahol definiálva van):

```csharp
// Meglévő Object-ág mellé, VAGY az Object-ág elé ellenőrizni:
if (doc.RootElement.ValueKind == JsonValueKind.Array)
{
    foreach (var element in doc.RootElement.EnumerateArray())
    {
        if (element.TryGetProperty("tenant_id", out var idEl))
            if (Guid.TryParse(idEl.GetString(), out var g) && g != Guid.Empty)
                return g;
    }
}
```

A meglévő Object-ág (`TryGetProperty` direktben a root-on) maradhat fallback-ként.

---

## 3. feladat — Port dokumentáció (mellékes)

Az INFRA terminál észlelte: `ASPNETCORE_URLS=5001` a `kernel.env`-ben, de a Kernel ténylegesen 5000-en hallgat (`appsettings.Production.json: "Urls": "http://127.0.0.1:5000"`).

Rendezd el: vagy töröld az `ASPNETCORE_URLS` overridet a `kernel.env`-ből, vagy frissítsd az `appsettings.Production.json`-t hogy 5001-et használjon. Legyen konzisztens.

---

## Definition of Done

- [ ] Migration 0028 regenerálva: 3 fájl létezik (`.cs` + `.Designer.cs` + frissített `ModelSnapshot`)
- [ ] `dotnet ef migrations list` mutatja Migration_0028_StageRegistry-t
- [ ] Rate limit `appsettings`-ből olvas (`RateLimit:WritePerMinute`)
- [ ] Port konzisztens (5000 vagy 5001 — mindenhol ugyanaz)
- [ ] `GetTenantId()` helyesen parse-olja a `ValueKind == Array` esetet (double-serialized JSON array)
- [ ] Meglévő **1075** teszt zöld · 0 build warning
- [ ] Commit + push

## Visszajelzés

Outboxba: `MSG-KERNEL-060-DONE`
