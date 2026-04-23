---
id: MSG-CUTTING-017
from: root
to: cutting
type: task
priority: high
status: READ
ref: MSG-TESTER-008
created: 2026-04-18
---

# BUG-004 regresszió — reteszten még mindig 500

A `d91ce53` deploy után manuális reteszten `POST /bff/cutting/plans` **még mindig 500**-at ad. A fix letesztelve a unit teszteken (77 pass), de VPS-en nem érvényes.

## Tesztelő által küldött payload (UI-ból)

```json
{ "name": "Reteszt vágóterv 2026-04-18", "date": "2026-04-18" }
```

## Vizsgálandó

### 1. A BFF mit küld ténylegesen a Cutting service-nek?

Ellenőrizd az Orchestrator cutting route-ot:
```
/opt/spaceos/spaceos-orchestrator/src/routes/cutting.route.ts
```
- Milyen JSON-t proxyzik a BFF a Cutting service felé?
- A `date` mező neve egyezik-e a `CreateDailyCuttingPlanCommand`-ban elvárt mezőnévvel (`Date` vs `date` vs `PlanDate`)?

### 2. Esetleges mélyebb exception

Ha az INFRA log elérhető, nézd meg az exception stack trace-t — az INFRA-010 üzenet kéri a logot.

### 3. Lehetséges ok: C# JSON deszializáció

A `System.Text.Json` alapértelmezetten **case-sensitive**. Ha a BFF `"date"` (lowercase) küld és a C# `CreatePlanRequest` `Date` (PascalCase) property-t vár JSON attribute nélkül → null/default érték → esetleg még mindig crashel.

Ellenőrizd:
```csharp
// Ha ez van, case-sensitive:
public string Date { get; set; }
// Fix: explicit JsonPropertyName
[JsonPropertyName("date")]
public string Date { get; set; }
// Vagy: builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.PropertyNameCaseInsensitive = true)
```

## DoD

- [ ] `POST /bff/cutting/plans` a TESTER payload-dal 201-et ad VPS-en
- [ ] `dotnet test` → ≥77 zöld
- [ ] Ha BFF-oldali probléma → jelezd BLOCKED-ban (ORCH terminál fix szükséges)

---

*Skill: `/spaceos-terminal`*
