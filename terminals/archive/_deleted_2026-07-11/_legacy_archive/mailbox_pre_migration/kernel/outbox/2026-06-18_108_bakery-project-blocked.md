---
id: MSG-KERNEL-108-BLOCKED
from: kernel
to: conductor
type: blocked
status: READ
ref: MSG-KERNEL-109
created: 2026-06-18
---

# BLOCKED — Bakery Project architektúrális tisztázás szükséges

## Probléma

A MSG-KERNEL-109 feladat (BAKERY-V1 — Pékség vertikál) **architektúrális tisztázást** igényel mielőtt a Kernel terminál implementálni tudná.

## Ütközés ADR-024-gyel (IParametricProduct)

A tervdokumentum szerint:

```
Technikai implementálás terv:
1. bakery_domain.md megírása
2. Receptúra (BOM) store modul
3. Lejárat-modul — lot-szintű expiresAt + FIFO kivét
4. Sütés-ütemező
5. HACCP napló
```

**ADR-024 szerint:** "A Kernel nem tudja és nem kell tudnia, hogy mi egy termék konkrétan. Termékspecifikus logika → Joinery/MEP/Cutting Driver-ekben."

Ez a feladat egy **új domain adapter modul** (mint a Joinery, Cutting, stb.), **nem** Kernel-beli implementáció.

## Tisztázandó kérdések

### 1. Kernel hatásköre ebben a projektben

**Kérdés:** Mi az a Kernel-specifikus implementálandó rész?

A tervdokumentum szerint:
- "Domén-független MAG (ugyanaz, mint JoineryTechnél)" — ez **már létezik** (Kernel + közös modulok)
- "Domén-ADAPTER — Pékség specifikus" — ez **új modul** kell legyen (SpaceOS.Modules.Bakery)

**Lehetséges Kernel-érintettség:**
- ❓ Lejárat-modul: Infrastructure-beli kiterjesztés? (lot-szintű `expiresAt`)
- ❓ Új tábla a Kernel DB-ben? (vagy külön bakery DB ADR-039 szerint?)
- ❓ Kernel API új endpoint-ok? (vagy csak a Bakery modul saját API-ja?)

### 2. Modul architektúra

**ADR-039 szerint:** "Kernel nem hív közvetlenül Joinery/Cutting/Inventory DB-t. Modulok csak publikus API-n keresztül kommunikálnak."

**Kérdés:** A Bakery modul:
- [ ] Saját adatbázis-t kap? (mint Joinery, Cutting)
- [ ] Saját API projektet? (`SpaceOS.Modules.Bakery.Api`)
- [ ] Kernel Infrastructure-ben kap extension-öket? (pl. lot-lejárat support)

### 3. Nyitott tervezési kérdések

A tervdokumentum "Blokkolók" szekciója még döntetlen:
- [ ] Lejárat-modul: integrálódik-e a meglévő raktár lot-modelljébe?
- [ ] HACCP naplózás: DMS-be mint dokumentum VAGY külön modul?
- [ ] Domain név és brand
- [ ] Első ügyfél azonosítása

Ezek ROOT/ARCHITECT döntések, nem implementációs feladatok.

## Javaslat

### Opció A — ARCHITECT koordináció

ROOT hívja be az ARCHITECT terminált:
1. ARCHITECT elemzi a tervdokumentumot
2. ARCHITECT definiálja a modul-határokat (Kernel vs. Bakery modul)
3. ARCHITECT spec-et ír minden érintett terminálnak:
   - Kernel: mi kell a Infrastructure-ben? (ha kell egyáltalán)
   - Új BAKERY terminál: SpaceOS.Modules.Bakery implementáció
   - FE terminál: pekseg.portal brand portal
   - INFRA: új modul deploy konfig

### Opció B — ROOT átütemezés

ROOT átgondolja a feladatkiosztást:
- Ez a feladat túl magas szintű egy terminálnak
- Bontás kisebb, modul-specifikus taskokra:
  - BAKERY-001: SpaceOS.Modules.Bakery skeleton projekt
  - BAKERY-002: bakery_domain.md dokumentáció
  - KERNEL-XXX: (ha van Kernel-érintettség)

## Következő lépés

Kérem ROOT vagy ARCHITECT döntését:
- Mi a Kernel-specifikus implementálandó rész ebben a projektben?
- Vagy ez a feladat rossz terminálhoz került?

---

**Kernel terminál READY** — várom a tisztázást mielőtt folytatnám.
