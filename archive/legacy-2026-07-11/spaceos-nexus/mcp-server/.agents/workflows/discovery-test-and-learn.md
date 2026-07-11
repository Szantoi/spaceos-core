---
description: how to run Discovery Phase 4 – Test & Learn with Operatív Handoff and standardization
---

# Discovery – Fázis 4: Test & Learn / Konklúzió és Handoff (The Integrator ⚖️)

**Szerepkör:** The Integrator
**Módszertan:** Double Diamond – Záró konvergencia → **Döntés a hipotézis sorsáról**
**Elhelyezés:** `Plans/Discoveries/<Program>/<Alprojekt>/04_test-and-learn/`
**Forrás:** `Plans_Discovery_Framework_Standard.md` — Fázis 4

> ⚖️ **Ez a fázis dönt az ötlet sorsáról.** A cáfolt hipotézis NEM kudarc — tanulás. Az iteráció a rendszer szerves része.

---

## Lépések

### 1. Input dokumentumok beolvasása

```
03_prototype/experiments/exp-XXX.md         ← Kísérlet terv
03_prototype/prototypes/<slug>/              ← Megvalósított prototípus
01_define/hypotheses/hyp-XXX.md             ← Eredeti hipotézis
01_define/success-criteria/                 ← Sikerkritériumok
```

### 2. Eredmények dokumentálása

```
04_test-and-learn/results/result-<NNN>.md
```

Tartalom:
- Mi volt a kísérlet pontos módszere?
- Milyen számszerű eredményt kaptunk?
- Teljesültek-e a sikerességi küszöbök?

### 3. Konklúzió: VALIDATED vagy INVALIDATED?

#### ✅ Ha VALIDATED (Igazolódott)

Hozd létre a Learning fájlt:

```
04_test-and-learn/learnings/learn-<NNN>.md
```

**Learning sablon (validated):**

```markdown
---
id: learn-XXX
hypothesis: hyp-XXX
experiment: exp-XXX
verdict: validated
date: YYYY-MM-DD
promoted-to-standard: false | [standard fájl neve]
---
# Tanulság: [Rövid cím]

## Mit teszteltünk?
[Rövid összefoglaló]

## Mi történt ténylegesen?
[Objektív eredmény, számokkal ahol lehetséges]

## Konklúzió
**Hipotézis:** igazolódott

## Mi lesz a következő lépés?
- [ ] **Kísérleti kód visszabontása:** A prototípus kód nyom nélkül eltávolítva (Reversible Rule)
- [ ] Átkerül az operatív projektbe → lásd Operatív Handoff
- [ ] Standardizálva → `../../../../05_reference/standards/<fájlnév>.md`
- [ ] Új hipotézis (finomítás) → `../../01_define/hypotheses/hyp-XXX+1.md`

## Operatív Handoff — Translation Layer

**Projektcél (goal.md-be kerül):**
[1-2 mondatos célkitűzés]

**Scope:**
- **In Scope:** [...]
- **Out of Scope:** [...]

**Sikerkritériumok (a learnings-ből átemelve):**
- [ ] [...]

**Indítandó Epic(ek):**
- EPIC-01: [...]

**PO Értesítés:**
- [ ] Product Owner értesítése az eredményről és a javasolt következő lépésről (Handoff).
```

**ADR véglegesítése:**
- Nyisd meg az érintett `02_ideate/adrs/ADR-XXX.md` fájlokat
- Frissítsd: `status: accepted`, `validated-by: learn-XXX`

**Operatív projekt indítása:**
- Hozd létre: `<EPIC_ROOT>/goal.md` a Handoff Translation Layer alapján
- → Menj az `orchestrator-epic-execution.md` workflow-ba

#### ❌ Ha INVALIDATED (Cáfolódott)

```
04_test-and-learn/learnings/learn-<NNN>.md    ← verdict: invalidated
04_test-and-learn/new-hypotheses/hyp-<NNN+1>.md
```

**Learning sablon (invalidated):**

```markdown
---
id: learn-XXX
hypothesis: hyp-XXX
experiment: exp-XXX
verdict: invalidated
date: YYYY-MM-DD
---
# Tanulság: [Rövid cím]

## Mit teszteltünk?
[Összefoglaló]

## Mi történt ténylegesen?
[Objektív eredmény — miért bukott meg?]

## Konklúzió
**Hipotézis:** cáfolódott — [miért?]

## Következő lépés
- [ ] **Kísérleti kód visszabontása:** A prototípus kód nyom nélkül eltávolítva (Reversible Rule)
- [ ] Új hipotézis → `../../01_define/hypotheses/hyp-XXX+1.md`
- [ ] Projekt leáll (ha nincs életképes alternatíva)
```

**ADR lezárása:**
- Érintett `02_ideate/adrs/ADR-XXX.md`: `status: rejected` (NEM törölhető!)

**Vissza Fázis 1-be:** `discovery-define.md`

### 4. Standard-má emelés (ha általánosan alkalmazható)

Ha az eredmény **nem csak erre a projektre** érvényes, konzerváld standardként:

```
Plans/Discoveries/05_reference/standards/<új-standard-fájlnév>.md
```

> **Aranyszabály:** Ha ugyanazt a problémát kétszer kell "felfedezni", az elsőre Standard kellett volna legyen.

### 5. Definition of Done – Fázis 4

- [ ] `result-XXX.md` elkészítve számszerű adatokkal
- [ ] `learn-XXX.md` elkészítve (validated VAGY invalidated)
- [ ] ADR-ek státusza frissítve (accepted / rejected)
- [ ] **Prototípus kód visszabontva:** A kísérlet kódja nyom nélkül eltávolítva a rendszerből
- [ ] Ha validated: Operatív Handoff kitöltve, `goal.md` létrehozva
- [ ] **PO Értesítés:** A Product Owner értesítve a végeredményről.
- [ ] Ha invalidated: új hipotézis fájl vagy projekt leállítás dokumentálva
- [ ] Ha általánosan bevált: Standard-má emelve

---

## Kimenet

| Fájl | Útvonal |
|:-----|:--------|
| Eredmény | `04_test-and-learn/results/result-<NNN>.md` |
| Tanulság | `04_test-and-learn/learnings/learn-<NNN>.md` |
| Új hipotézis (ha cáfolva) | `04_test-and-learn/new-hypotheses/hyp-<NNN+1>.md` |
| Standard (ha bevált) | `Plans/Discoveries/05_reference/standards/<fájlnév>.md` |
| Operatív projekt (ha validated) | `<EPIC_ROOT>/goal.md` |

## Következő lépés

- ✅ Validated → **Operative:** `orchestrator-epic-execution.md`
- ❌ Invalidated → **Discovery újraindítás:** `discovery-define.md`
