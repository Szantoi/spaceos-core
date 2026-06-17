---
name: spaceos-session-kickoff
description: >
  SpaceOS tervezési session indító skill. Aktiválódjon amikor a felhasználó
  egy új fejlesztési session-t nyit és nem adott meg konkrét feladatot —
  pl. "Szia!", "Hol tartunk?", "Mi a következő lépés?", "Folytassuk a tervezést",
  "Mit csináljunk ma?", "Mi van még hátra?", "Melyik phase-t tervezzük meg?",
  "Összefoglalás kérem", "Frissítsd a képet", vagy ha a felhasználó felsorolja
  mi készült el és megkérdezi mi a következő lépés. A skill betölti a projekt
  kontextust, PM + SaaS business szemüveggel értékeli a helyzetet, és javaslatot
  tesz a következő tervezési lépésre — szakmai és vezető számára is érthető
  formában. NE aktiválódjon ha konkrét feladatot kap (pl. "tervezzük a Phase 3B-t"
  → spaceos-arch-planner, "implementáld a Phase 3B-t" → Claude Code).
---

# SpaceOS Session Kickoff

Tervezési session indító. Betölti a projekt kontextust, három szemüvegen át értékeli
(PM · SaaS business · technikai), és javaslatot tesz a következő lépésre.

Kimenet: **vezető számára is érthető** döntési alap — nem implementációs terv.

---

## 0. Sub-skill betöltés (MINDIG ELŐSZÖR, de SORBAN)

A helyzetelemzéshez két sub-skill analitikai keretrendszerét kell alkalmazni:

**Lépés 1 — Projekt kontextus betöltése után:**
→ Olvasd el: `references/sub-senior-pm.md`
Alkalmazd: WSJF prioritizáció, portfolio health, risk assessment a SpaceOS fázisokra.

**Lépés 2 — Üzleti hatás értékelésekor:**
→ Olvasd el: `references/sub-saas-metrics-coach.md`
Alkalmazd: business gate szemüveg — mi blokkolja az ARR-t, mikor kapcsolható be az Escrow.

Ne töltsd be egyszerre — sorban, ahogy az elemzés halad.

---

## 1. Projekt kontextus betöltése

Olvasd be ebben a sorrendben:

**1.1 Legfrissebb kódbázis státusz**
→ `Codebase_Status_YYYYMMDD.md` — a legnagyobb dátumú fájlt töltsd be a Project Knowledge-ból.
Kivond: test baseline, deployed státuszok, nyitott fejlesztések listája.

**1.2 Sprint és tervdokumentum státuszok**
→ Minden `*_Architecture_v*.md` és `*_Sprint_D_Phase*_v*.md` fájlt nézz meg.
Kérdés: melyik `IMPLEMENTÁCIÓRA KÉSZ` de még nincs implementálva?

**1.3 Security adósság**
→ `SpaceOS_Security_Task_Register.md` — nyitott P1/P2 tételek listája.

---

## 2. Helyzetelemzés — kimenet struktúrája

### 2.1 Implementációs pipeline táblázat

| Fázis | Tervdok | Státusz | Üzleti hatás |
|-------|---------|---------|-------------|
| [fázis] | [vN / nincs] | CLOSED_DONE / READY / DESIGN | [1 mondat] |

**Státusz értékek:**
- `CLOSED_DONE` — implementálva, deployed
- `READY vN` — tervdok kész, implementáció vár
- `DESIGN` — tervdok nincs, tervezés szükséges

### 2.2 Design-előny mutató

```
Design-előny: X fázis READY · Y fázis DESIGN igényel
```

| Állapot | Értelmezés |
|---------|-----------|
| X ≥ 2 READY | Pipeline tele — implementáció a szűk keresztmetszet |
| X = 1, Y ≥ 1 | Optimális — egy pályán, következő tervezhető |
| X = 0 | Sürgős tervezés — implementáció hamarosan blokkolva lesz |

### 2.3 WSJF prioritizáció (sub-senior-pm alapján)

Minden DESIGN státuszú fázisra:

| Fázis | Business Value | Time Criticality | Risk Reduction | Job Size | WSJF |
|-------|---------------|-----------------|---------------|---------|------|
| [fázis] | 1-10 | 1-10 | 1-10 | 1-10 | (BV+TC+RR)/JS |

Legmagasabb WSJF = javasolt következő tervezési feladat.

### 2.4 SaaS business gate (sub-saas-metrics-coach alapján)

Két kérdés, egyenként 1-2 mondatban:

**Doorstar Kft. pilot gate:** Mi hiányzik ahhoz, hogy az első fizető ügyfél production-ban legyen?

**Escrow GA gate:** Mi az egyetlen technikai blokkoló az Escrow feature flag bekapcsolásához?

### 2.5 Nyitott security adósság (ha releváns)

Csak ≥ HIGH, még nem lezárt tételek — max 5 sor táblázatban.

---

## 3. Javaslat formátuma

```
## Javasolt következő lépés

**[Fázis neve]** tervezése — [1 mondatos indoklás]

| Szempont | Értékelés |
|---------|----------|
| WSJF score | [szám] — [helyezés a többi fázis között] |
| Üzleti gate | [Escrow GA / Doorstar pilot / egyéb] |
| Blokkoló feltétel | [mi kell előtte] |
| Tervezési effort | [~N session / nap] |
| Ha most kihagyjuk | [konkrét következmény] |

→ Aktiválandó: `/spaceos-arch-planner`
```

Ha több egyenrangú opció van, mutasd be mindkettőt a táblázattal, és adj explicit ajánlást.

---

## 4. Kimenet stílusa

- **Vezető számára is érthető:** minden technikai döntéshez 1 mondatos üzleti következmény
- **Tábla > próza:** státuszok és prioritások mindig táblázatban
- **Nem kérdez vissza** ha a kontextusból kiderül — dönt és indokol
- **Kérdez**, ha valóban két egyenrangú opció van és a döntés az architektől függ
- **Magyar nyelv** az elemzésben, technikai terminológia angolul marad

---

## 5. Átadás más skilleknek

| Helyzet | Következő |
|---------|----------|
| Tervezés szükséges | `spaceos-arch-planner` |
| Implementáció indul | Claude Code + CLAUDE.md |
| Részletes sprint tervezés | `/senior-pm` |
| Security audit kell | `/senior-security` |
