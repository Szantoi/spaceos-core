---
description: how to run a Fast Track (S-size) single-file discovery iteration
---

# Discovery – Fast Track (S méret) ⚡

**Mikor használd?** Kis módosítás validálása: script refaktor, konfiguráció finomítás, apró UX változtatás — max. 2 óra ráfordítással elvégezhető és 1 fájlban dokumentálható.
**Elhelyezés:** `Plans/Discoveries/<Program>/<Alprojekt>/06_fast-tracks/fast-track-<NNN>.md`
**Forrás:** `Plans_Discovery_Framework_Standard.md` — Fast Track szekció

> ⚡ **Ha a Fast Track közben kiderül, hogy a munka valójában L méretű** (több fájl, > 2 óra, production kód érintett), **állj meg** és kezd el a Full Track workflow-t: `discovery-observation.md`.

---

## Lépések

### 1. Fast Track fájl létrehozása

```
Plans/Discoveries/<Program>/<Alprojekt>/06_fast-tracks/fast-track-<NNN>.md
```

Töltsd ki az összes alábbi szekciót egyetlen fájlban:

**Fast Track sablon:**

```markdown
---
id: ft-NNN
size: S
date: YYYY-MM-DD
verdict: open | validated | invalidated
---
# Fast Track: [Rövid cím]

## 📡 Megfigyelés
[Mit láttam? Mi a probléma? — Tényszerű leírás, értékelés nélkül]

## 🧪 Hipotézis
Ha [X-et teszem], akkor [Y következik], mert [Z ok].

## 🔬 Kísérlet & Eredmény
[Mit csináltam? Mi lett az eredmény? Max. X óra ráfordítással.]

## 📊 Konklúzió
- **Validálva / Cáfolva**
- Következő lépés:
  - [ ] Projekt indulás → `orchestrator-epic-execution.md`
  - [ ] Bevált módszer standardizálása → `05_reference/standards/`
  - [ ] Új Fast Track szükséges → `fast-track-NNN+1.md`
- **PO Értesítés:**
  - [ ] Értesítsd a Product Ownert a Fast Track eredményéről.
```

### 2. Kísérlet végrehajtása (max. 2 óra)

Hajtsd végre a legkisebb szükséges változtatást, és mérd az eredményt.

> ⏱️ **STOP szabály:** Ha 2 óra után nincs eredmény, állj meg. Értékeld: Full Track-ké kell-e emelni?

### 3. Verdict kitöltése

Frissítsd a YAML frontmatter-t:
- `verdict: validated` — Ha az eredmény igazolta a hipotézist
- `verdict: invalidated` — Ha cáfolódott

### 4. Tudás konzerválása (ha általánosan alkalmazható)

> **Aranyszabály:** Ha a Fast Track eredménye általánosan alkalmazható, emeld át Standard-dá.

```
Plans/Discoveries/05_reference/standards/<új-standard>.md
```

A `learn-XXX.md` helyett a Fast Track fájlra hivatkozz: `promoted-from: ft-NNN`.

### 5. Definition of Done

- [ ] `fast-track-NNN.md` fájl létrehozva a `06_fast-tracks/` mappában
- [ ] Mind a 4 szekció kitöltve (Megfigyelés, Hipotézis, Kísérlet, Konklúzió)
- [ ] `verdict` mező frissítve
- [ ] **PO Értesítés:** A Product Owner értesítve a Fast Track eredményéről.
- [ ] Ha általánosan bevált: Standard-má emelve

---

## Kimenet

| Fájl | Útvonal |
|:-----|:--------|
| Fast Track | `06_fast-tracks/fast-track-<NNN>.md` |
| Standard (opcionális) | `Plans/Discoveries/05_reference/standards/<fájlnév>.md` |

## Esetleges eszkaláció

Ha a Fast Track során kiderül, hogy L méretű a feladat:
→ **Full Track:** `discovery-observation.md` → `discovery-define.md` → ...
