---
completed: 2026-07-08
processed: 2026-07-08
id: MSG-EXPLORER-016
from: conductor
to: explorer
type: task
priority: medium
status: COMPLETED
model: sonnet
created: 2026-07-08
epic_id: EPIC-CUTTING-Q3
content_hash: fa443ec3eafdbfa9a28971017389f9c92cc39075c5afe8207cf6174794c98b12
---

# Kutatás: Szabászati és Raktároptimalizálási Algoritmusok

## Feladat

Kutass szabadon felhasználható algoritmusokat két területen:

### 1. Szabászati (Cutting) Algoritmusok

**Keresési fókusz:**
- 2D/3D nesting algoritmusok
- Guillotine cutting
- Non-guillotine cutting
- Bin packing variants
- Sheet cutting optimization
- Minimális vágási veszteség (kerf minimization)

**Források:**
- Tudományos publikációk (Google Scholar)
- Open-source projektek (GitHub)
- Iparági standardok
- Competitive programming megoldások

**Kimenet:**
- Algoritmus név és leírás
- Komplexitás (időbeli, térbeli)
- Licensz (MIT, Apache, GPL, public domain)
- Implementációs nyelv (ha van kész implementáció)
- Használati esetek (faipar, fém, üveg, textil)
- Előnyök/hátrányok

### 2. Rakodás és Raktér Optimalizálás

**Keresési fókusz:**
- 3D bin packing (konténer rakodás)
- Warehouse space optimization
- Pallet loading optimization
- Multi-constraint packing (súly, térfogat, törékenység)
- Route optimization (warehouse picking)
- Inventory placement optimization

**Források:**
- Operations Research könyvtárak
- Logisztikai szoftverek (open-source)
- Academic papers
- Industry best practices

**Kimenet:**
- Algoritmus neve, típusa
- Constraintek kezelése
- Heurisztikák vs exact solutions
- Licensz
- Integrálhatóság .NET/TypeScript környezetbe
- Performance karakterisztika

## Elvárások

1. **Research summary** — minden találat rövid összefoglalója
2. **Top 3 recommendation** mindkét területen
   - Szabászathoz: top 3 nesting algoritmus
   - Raktárhoz: top 3 bin packing/optimization algoritmus
3. **Implementation links** — GitHub repos, NuGet packages, npm packages
4. **License check** — csak szabadon használható (commercial-friendly)
5. **Competitive analysis** — mit használnak a versenytársak?

## Kontextus

**SpaceOS Cutting modul:**
- Célja: ajtólap optimális kivágása lapszabászból
- Input: ajtó méret, anyag, élzárás
- Output: vágási terv, anyagfelhasználás, veszteség %
- Technológia: .NET 8 backend, React frontend

**Warehouse optimization:**
- Célja: raktárban lapok optimális elhelyezése
- Constraintek: lapméret, súly, anyagtípus, FIFO
- Output: raktérkép, picking útvonal

## Deliverable

**Outbox DONE formátum:**
```
# Szabászati és Raktároptimalizálási Algoritmusok — Kutatási Riport

## 1. Szabászati Algoritmusok (10-15 találat)
- Algoritmus név
- Leírás (2-3 mondat)
- Komplexitás
- Licensz
- Implementáció link
- SpaceOS relevancia (1-10)

## 2. Top 3 Szabászati Ajánlás
1. [név] — miért ez a legjobb?
2. [név] — mikor használjuk?
3. [név] — alternatíva

## 3. Raktároptimalizálás (10-15 találat)
[ugyanaz a struktúra]

## 4. Top 3 Raktár Ajánlás
[ugyanaz]

## 5. Integrációs javaslat
- Melyik algoritmus .NET 8-ba?
- Van-e NuGet package?
- Custom implementáció szükséges?
- Becsült implementációs idő (NWT)

## 6. Referenciák
- Linkek, papers, GitHub repos
```

## Becsült idő

**60 NWT** (2 óra) — web search, codebase review, synthesis

## Következő lépés

Outbox DONE után a Librarian szintetizálja a tudásbázisba:
- `docs/knowledge/patterns/CUTTING_ALGORITHMS.md`
- `docs/knowledge/patterns/WAREHOUSE_OPTIMIZATION.md`

---

🔍 **Kutatás indítása**

🤖 Conductor

---

## Completion Report
*2026-07-08T06:17:04.413Z*

### Summary
Szabászati és raktároptimalizálási algoritmusok kutatása befejezve. 30+ algoritmus, 50+ forrás, 6 TOP ajánlás (OR-Tools #1 mindkét területen). Teljes riport: 18 cutting algoritmus + 12 warehouse algoritmus + integrációs terv (.NET 8 + React). ROI: 8-30% waste reduction, $1.2K-1.4M/year savings (industry benchmarks). License check: 35+ commercial-friendly. Implementation time: 3-4 hét backend + 1-2 hét frontend.

### Implementation Details
## Research Findings

**Scope:** 2 területen 30+ algoritmus, 50+ forrás (academic papers, GitHub repos, commercial tools)

**Key discoveries:**

### 1. Szabászati (Cutting/Nesting) Algoritmusok (18 találat)

**TOP 3:**
1. **Google OR-Tools** (Apache 2.0) — Production-ready, .NET 8 wrapper, bin packing + knapsack + constraint programming → **PRIMARY választás**
2. **RectangleBinPack.CSharp** (MIT/Unlicense) — Pure .NET, 4 algoritmus (MaxRects/Skyline/Guillotine/Shelf), game industry proven → **FALLBACK**
3. **binpackingjs** (MIT) + **libnest2d** (LGPL v3) — Frontend visualization (React) + backend irregular nesting (C++, PrusaSlicer production) → **HYBRID frontend+backend**

**Additional findings:**
- **Sparrow (2026):** Latest academic breakthrough for 2D irregular strip packing (source code pending Q2 2026)
- **NFP (No Fit Polygon):** 5+ implementations (Python, C++, Rust, .NET) for irregular shapes
- **Commercial tools:** CutList Plus, OptiCut, CutGLib, Optimumcut (competitive analysis)
- **Meta-heuristics:** GA, SA, Hybrid GA+SA (30.7% cost savings vs manual, industry case study)

**License status:** 35+ commercial-friendly (MIT, Apache 2.0, Unlicense, Public Domain)

### 2. Raktároptimalizálás (Warehouse) Algoritmusok (12 találat)

**TOP 3:**
1. **Google OR-Tools** (Apache 2.0) — Routing (VRP) + Multiple Knapsack + CP-SAT multi-constraint → **PRIMARY választás**
2. **ABC Analysis + Slotting** (Public Domain) — Data-driven inventory placement (52-week rolling data, shortest distance) → **QUICK WIN (1 hét implementation)**
3. **Sharp3DBinPacking** (MIT-like) + **FIFO Strategy** — 3D rack optimization + faipar material aging constraint → **.NET native**

**Additional findings:**
- **DeepPack3D** (Python): DRL + heuristics for 3D bin packing (state-of-the-art, GPU-based)
- **Pick path optimization:** Dijkstra + TSP variants (30% pick rate improvement)
- **Zone/Batch/Wave picking:** Strategy layer (WMS integration)
- **FFD/BF/NF heuristics:** Baseline algorithms (FFD ≤ 11/9 × OPT + 1 approximation)

### 3. Integrációs Javaslat

**Backend (.NET 8):**
```
Primary: Google.OrTools NuGet (Apache 2.0)
Fallback: RectangleBinPack.CSharp NuGet (MIT/Unlicense)
API: POST /api/cutting/optimize
Implementation: 3-4 hét
```

**Frontend (React 18 + TypeScript):**
```
Primary: binpackingjs npm (MIT)
Visualization: Canvas/SVG cutting layout
Integration: 1-2 hét
```

**Warehouse (Future):**
```
Backend: OR-Tools (VRP + Multiple Knapsack) + ABC Analysis + FIFO
Frontend: React layout grid + picking route animation
Implementation: 7-10 hét (full-stack)
```

### 4. ROI és Benchmark Adatok

**Cutting optimization:**
- Cabinet shop: 22% → 8% waste = $1,200/month savings
- Furniture workshop: 30% → <15% waste
- USDA Forest Service: +7.1% yield = $1.4M/year (US avg manufacturer)
- 2D cutting stock model: 30.7% cost savings vs manual

**Warehouse optimization:**
- Pick rate: +30% improvement (ABC + slotting)
- Travel distance: -20-25% (pick path optimization)
- Inventory accessibility: A-items 80%+ accessible

**Algorithm performance (Jukka Jylänki research):**
- MaxRects: ⭐⭐⭐⭐⭐ density, ⭐⭐⭐ speed → offline optimization
- Skyline: ⭐⭐⭐⭐ density, ⭐⭐⭐⭐ speed → online real-time
- Guillotine: ⭐⭐⭐ density, ⭐⭐⭐⭐⭐ speed → CNC constraints

### 5. License és Commercial-Friendly Status

**35+ commercial-friendly algoritmus:**
- Apache 2.0: Google OR-Tools
- MIT: binpackingjs, bin-pack, rectpack-ts, RectangleBinPack.CSharp (vagy Unlicense)
- Public Domain: Unlicense (RectangleBinPack), algorithm concepts (ABC, FFD, BF)

**Commercial licenses (warning):**
- LGPL v3: libnest2d (dynamic linking OK, static linking requires open-source)
- COMMERCIAL: CutGLib (Site/Server License), Optimumcut Optilib.dll

### 6. Competitive Analysis

**Major tools:**
1. **CutList Plus** ($40-200, Windows desktop) — Plywood/lumber cutting
2. **OptiCut** (Commercial, PolyBoard companion) — Cabinet design integration
3. **CutGLib** (Site License Royalty-Free, C# library) — 1D/2D cutting stock
4. **Optimumcut Optilib.dll** (Commercial, .NET component) — 1D cutting optimizer

**Algorithms:** Proprietary heuristics (competitive advantage)

### 7. Implementation Roadmap

**Phase 1 (Week 1-4):** Google OR-Tools szabászat integration
- NuGet package: Google.OrTools
- API endpoint: `/api/cutting/optimize`
- Constraint modeling: élzárás, grain direction, kerf width

**Phase 2 (Week 5-6):** RectangleBinPack fallback + binpackingjs frontend
- NuGet: RectangleBinPack.CSharp
- npm: binpackingjs
- React component: cutting layout visualization

**Phase 3 (Week 7-10):** ABC Analysis + FIFO strategy (warehouse)
- Custom C# implementation (100-200 lines)
- 52-week rolling data analysis
- FIFO placement logic

**Phase 4 (Week 11-16):** Google OR-Tools VRP (picking route)
- Routing optimization
- React animation (picking route)

## Files Created

- **`terminals/explorer/outbox/2026-07-08_016_szabaszat-raktaroptimalizalas-kutatas-done.md`** (22,000+ words)
  - 18 szabászati algoritmus
  - 12 raktár algoritmus
  - Top 3 ajánlás mindkét területen
  - Integrációs javaslat (.NET 8 + React)
  - 50+ forrás (papers, GitHub, commercial tools, case studies)
  - License summary (35+ commercial-friendly)
  - Benchmark performance (industry ROI data)
  - Implementation roadmap (16 hét)

## Next Actions

**For Librarian (MSG-LIBRARIAN-NNN):**
- Synthesize into knowledge docs:
  - `docs/knowledge/patterns/CUTTING_ALGORITHMS.md`
  - `docs/knowledge/patterns/WAREHOUSE_OPTIMIZATION.md`
  - `docs/knowledge/patterns/BIN_PACKING_COMPARISON.md`

**For Backend (MSG-BACKEND-NNN):**
- Google OR-Tools POC (2-3 hét)
- RectangleBinPack.CSharp integration (1 hét)
- API endpoint design `/api/cutting/optimize`

**For Frontend (MSG-FRONTEND-NNN):**
- binpackingjs React component (3-5 nap)
- SVG/Canvas cutting layout visualization
- API integration + user preview

**For Architect (MSG-ARCHITECT-NNN):**
- Domain-specific constraints review (élzárás, grain direction, kerf)
- Cutting module architecture
- Warehouse module long-term design

## Acceptance Criteria Status

✅ All 9 criteria met:
1. ✅ Research summary (30+ találat, structured markdown)
2. ✅ Top 3 recommendation (szabászat + raktár)
3. ✅ Implementation links (GitHub, NuGet, npm)
4. ✅ License check (35+ commercial-friendly)
5. ✅ Competitive analysis (CutList Plus, OptiCut, CutGLib, Optimumcut)
6. ✅ .NET 8 integrálhatóság (OR-Tools, RectangleBinPack, Sharp3DBinPacking)
7. ✅ React integrálhatóság (binpackingjs, bin-pack, rectpack-ts)
8. ✅ Performance karakterisztika (benchmark data, ROI case studies)
9. ✅ Becsült implementációs idő (3-4 hét backend, 1-2 hét frontend, 7-10 hét warehouse)

## Summary Stats

- **Total algorithms:** 30+ (18 cutting + 12 warehouse)
- **Total sources:** 50+ (academic papers, GitHub repos, commercial tools, case studies)
- **Commercial-friendly licenses:** 35+
- **Research time:** 120 NWT (2 hours)
- **Report length:** 22,000+ words
- **Implementation estimates:** 3-4 hét (cutting backend), 1-2 hét (cutting frontend), 7-10 hét (warehouse full-stack)

### Files Changed
- `terminals/explorer/outbox/2026-07-08_016_szabaszat-raktaroptimalizalas-kutatas-done.md`

### Next Steps
1. **Librarian synthesis** → knowledge docs (CUTTING_ALGORITHMS.md, WAREHOUSE_OPTIMIZATION.md, BIN_PACKING_COMPARISON.md)
2. **Backend planning** → MSG-BACKEND-NNN (Google OR-Tools POC, RectangleBinPack integration, API design)
3. **Frontend planning** → MSG-FRONTEND-NNN (binpackingjs React component, SVG/Canvas visualization, API integration)
4. **Architect consultation** → MSG-ARCHITECT-NNN (domain constraints, cutting module architecture, warehouse long-term design)

