---
id: MSG-ABSTRACTIONS-051
from: root
to: abstractions
type: task
priority: high
status: READ
created: 2026-04-10
ref: docs/archive/SpaceOS_Joinery_v2_Claude_Code_Package.md
---

# MSG-ABSTRACTIONS-051: Modules.Joinery v2 — Track C-Abs + Track E-Seed

## Kontextus

A Joinery terminál befejezte Track A + B + D-t (172 teszt, zöld). Most az Abstractions repóban kell elvégezni a Track C-Abs és E-Seed feladatokat.

---

## Track C-Abs — POST /api/templates/{name}/calculate

### Feladat

Új endpoint az Abstractions API-ban:

```
POST /api/templates/{name}/calculate
```

**Elvárások:**
- `{name}` = template neve (pl. `FAF_T`)
- Tenant check: a kérő tenant hozzáférhet-e az adott template-hez (RLS)
- Request body: `CalculationRequestDto` (door dimensions, material params)
- Response: `CalculationResultDto` (cutting list, process plan, CNC instructions)
- A Graph Engine meglévő logikáját hívja — nem duplikálja
- 401 ha nincs auth, 403 ha tenant nem férhet hozzá, 404 ha template nem létezik

**Blokkoló gate:** SEC-01 — tenant check kötelező, nem kihagyható

---

## Track E-Seed — FAF_T + FAF_Ü + BFAJ ProductTemplate seed

### Feladat

3 ProductTemplate seed adat létrehozása:

**FAF_T** — Fa ajtófélfa, tokos (kb. 15 slot, 20 connection, 7 param)
**FAF_Ü** — Fa ajtófélfa, üveges (FAF_T klón, eltérő üveg-slot)
**BFAJ** — Belső fa ajtó, jobbos

**Elvárások:**
- `ITemplateValidator.Validate()` PASS mindháromra
- Seed migration vagy seeder class (EF Core)
- A Graph Engine le tudja futtatni mindháromra a kalkulációt

---

## Tesztek

- Track C-Abs: ≥5 új teszt (tenant check, 401/403/404, sikeres kalkuláció)
- Track E-Seed: ≥3 új teszt (ITemplateValidator PASS mindháromra)
- Meglévő baseline (61 teszt) marad zöld

## Minden track után kötelező

```bash
dotnet test && dotnet build
```

## Válasz

Outbox üzenet: `docs/mailbox/abstractions/outbox/2026-04-10_051_joinery-v2-track-c-e-done.md`
