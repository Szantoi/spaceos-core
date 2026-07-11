---
description: how to run Discovery Phase 1 – Define with The Framer role (hypothesis formalization)
---

# Discovery – Fázis 1: Define / Hipotézis Formalizálása (The Framer 📐)

**Szerepkör:** The Framer
**Módszertan:** Double Diamond – 1. gyémánt, Konvergens fázis → **Problématér LEZÁRÁSA**
**Elhelyezés:** `[DISCOVERY_ROOT]/01_define/`
**Forrás:** `Plans_Discovery_Framework_Standard.md` — Fázis 1

> ⚠️ **Premature Convergence tilalom:** Ebben a fázisban **tilos** technológiai döntést hozni (pl. "PostgreSQL-t fogunk használni"). Az ADR-ek a következő fázisban (`02_ideate`) születnek. A Define fázis célja a **probléma** pontos meghatározása, nem a megoldásé.

---

## Lépések

### 1. Input beolvasása

```
[DISCOVERY_ROOT]/00_discovery/observations/    ← Megfigyelések
[DISCOVERY_ROOT]/00_discovery/research/         ← Kutatások
[DISCOVERY_ROOT]/00_discovery/analyses/         ← Elemzések
```

### 2. Scope Definition elkészítése (KÖTELEZŐ Full Track-nél)

Mielőtt a hipotézist megírod, rögzítsd a határokat:

```
[DISCOVERY_ROOT]/01_define/constraints/scope.md
```

**Scope sablon:**

```markdown
---
hypothesis: hyp-XXX
date: YYYY-MM-DD
---
# Scope: [Hipotézis rövid neve]

## In Scope (mit vizsgálunk?)
- [...]

## Out of Scope (mit NEM vizsgálunk — ebben a kísérletben?)
- [...]

## Technológia-független peremfeltételek
- [...]
```

### 3. Hipotézis fájl létrehozása

```
[DISCOVERY_ROOT]/01_define/hypotheses/hyp-<NNN>.md
```

**HDD Hipotézis sablon:**

```markdown
---
id: hyp-XXX
status: active
created: YYYY-MM-DD
scope: 01_define/constraints/scope.md
epic-link: [Ha már van projekt: link az epic_state.md-re]
---
# Hipotézis: [Rövid cím]

## Állítás
Ha [ezt az akciót tesszük] [ezeknek a felhasználóknak/rendszernek],
akkor [ez a mérhető eredmény] következik be,
mert [ez az ok/mechanizmus].

## Mérhető Sikerkritérium
- [ ] [1. mérhető feltétel]
- [ ] [2. mérhető feltétel]

## Out of Scope (mit NEM vizsgálunk ebben a kísérletben?)
- [...]
> Részletes scope: lásd `01_define/constraints/scope.md`

## Bukta esetén
Ha a hipotézis nem igazolódik → [új hipotézis ID] vagy [projekt leáll]

## Kapcsolódó ADR vázlat (ha szoftverdöntés várható)
- [Hivatkozz a `02_ideate/adrs/` alatt majd létrejövő ADR-re]
```

### 4. Sikerkritériumok dokumentálása

```
[DISCOVERY_ROOT]/01_define/success-criteria/criteria-<NNN>.md
```

Mindig mérhető, ellenőrizhető kritériumokat írj! Kerüld a "jobb lesz" típusú megfogalmazásokat.

### 5. Definition of Done – Fázis 1

- [ ] `scope.md` létrehozva IN/OUT Scope listával
- [ ] `hyp-XXX.md` létrehozva HDD formátumban
- [ ] Hipotézis ÁLLÍTÁST tartalmaz (Ha... akkor... mert...)
- [ ] Mérhető sikerkritériumok definiálva
- [ ] NINCS technológiai döntés a fájlban (sem ADR)

---

## Kimenet

| Fájl | Útvonal |
|:-----|:--------|
| Hipotézis | `01_define/hypotheses/hyp-<NNN>.md` |
| Scope | `01_define/constraints/scope.md` |
| Sikerkritériumok | `01_define/success-criteria/criteria-<NNN>.md` |

## Következő fázis

→ **Fázis 2:** `discovery-ideate.md` (The Framer + Explorer 📐🧭)
