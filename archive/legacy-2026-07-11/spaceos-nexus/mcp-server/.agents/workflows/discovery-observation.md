---
description: how to run Discovery Phase 0 – Observation with The Explorer role
---

# Discovery – Fázis 0: Megfigyelés (The Explorer 🧭)

**Szerepkör:** The Explorer
**Módszertan:** Double Diamond – 1. gyémánt, Divergens fázis
**Elhelyezés:** `Plans/Discoveries/<Program>/<Alprojekt>/00_discovery/`
**Forrás:** `Plans_Discovery_Framework_Standard.md` — Fázis 0

> 🧭 **Szabály:** Az Explorer csak **tényeket** rögzít. Semmiféle értékelés, következtetés vagy megoldásjavaslat nem kerülhet ebbe a fázisba. Az „idő előtti konvergencia" (premature convergence) megsértése a Double Diamond egyik leggyakoribb hibája.

---

## Lépések

### 1. Méretezés eldöntése (Fast Track vs. Full Track)

Mielőtt bármi mást csinálsz, döntsd el a Discovery méretét:

| Méret | Mikor? | Mit csinálj? |
|:------|:-------|:-------------|
| **S – Fast Track** | Kis módosítás, < 2 óra, 1 fájl | → Menj a `discovery-fast-track.md` workflow-ba |
| **L – Full Track** | Komplex, több fájl, rendszerszintű | → Folytasd ezt a workflow-t |

### 2. Alprojekt mappa létrehozása

Ha még nem létezik, hozd létre a struktúrát:

```
Plans/Discoveries/<Program>/<Alprojekt>/
├── README.md                         ← KÖTELEZŐ: alprojekt fókusza
├── 00_discovery/
│   ├── observations/
│   ├── research/
│   └── analyses/
```

A `README.md` tartalma: Az alprojekt fókusza, rövid összefoglalója, ami megmagyarázza, mit és miért vizsgálunk.

### 3. Megfigyelés fájl létrehozása

```
Plans/Discoveries/<Program>/<Alprojekt>/00_discovery/observations/obs-<YYYY-MM-DD>-<NNN>.md
```

**Kötelező sablon:**

```markdown
---
id: obs-YYYY-MM-DD-XXX
type: observation
source: felhasználói visszajelzés | saját tapasztalat | elemzés
date: YYYY-MM-DD
---
# Megfigyelés: [Rövid cím]

## Mit láttam/tapasztaltam?
[Tényszerű leírás, értékelés NÉLKÜL]

## Milyen kérdéseket vet fel?
- [Kérdés 1]
- [Kérdés 2]

## Kapcsolódó fájlok
- [link a research/ vagy analyses/ alá]
```

### 4. Kutatás elvégzése (opcionális, de ajánlott)

Ha a megfigyelés mögé mélyebb alap kell, keresd meg a releváns kontextust:

- **Piacelemzés / versenytárs elemzés** → `00_discovery/research/<téma>.md`
- **Mélyfúrás / sérülékenységi elemzés** → `00_discovery/analyses/<téma>.md`

### 5. Definition of Done – Fázis 0

- [ ] Alprojekt `README.md` létrehozva és kitöltve
- [ ] Legalább 1 `obs-XXX.md` fájl létrehozva tényszerű leírással
- [ ] NINCS megoldásjavaslat vagy technológiai döntés a fájlban
- [ ] A megfigyelés kérdéseket vet fel (nem válaszokat)

---

## Kimenet

| Fájl | Útvonal |
|:-----|:--------|
| Megfigyelés | `00_discovery/observations/obs-<YYYY-MM-DD>-<NNN>.md` |
| Kutatás (opcionális) | `00_discovery/research/<téma>.md` |

## Következő fázis

→ **Fázis 1:** `discovery-define.md` (The Framer 📐)
