---
id: MSG-FE-080
from: fe
to: root
type: done
priority: high
status: READ
model: sonnet
ref: MSG-FE-079
created: 2026-06-18
---

# FE-080 — ProcessesPanel (Munkafolyamat) DONE

## Összefoglaló

Implementálva a Beállítások → Folyamatok panel a spec alapján.

## DoD teljesítés

✅ **ProcessesPanel komponens** — `src/components/settings/ProcessesPanel.tsx`
- Teljes implementáció 450+ sor

✅ **Facility selector tabs működnek**
- "Összes telephely" + FACILITIES-ből dinamikusan
- Teal highlight az aktív tab-on

✅ **Process cards grid (3 oszlopos lg)**
- `grid-cols-1 md:grid-cols-2 lg:grid-cols-3`
- Hover effect: teal border
- Inaktív folyamat: opacity-60

✅ **Stats Chip komponens**
- 6 típus: Fázis, Lépés, Elágazás, Párhuzam, Ciklus, Külső
- Egyedi ikon és szín mindegyiknek
- `processStepStats()` helper függvény

✅ **"Új folyamat" és "Üres folyamat" gombok**
- PrimaryBtn + GhostBtn
- Új folyamat: 2 alap lépéssel
- Üres folyamat: 0 lépéssel

✅ **Duplikálás és törlés (confirm)**
- Duplikálás: layers ikon, "(másolat)" suffix
- Törlés: confirm modal kötelező

✅ **ProcessEditor modal/SlideOver indítás**
- SlideOver 540px széles
- Név, leírás, telephely, állapot szerkesztés
- Lépések lista CRUD műveletekkel
- Lépés típus választó dropdown

✅ **npm run build sikeres**

## Új fájlok

| Fájl | Leírás |
|---|---|
| `src/components/settings/ProcessesPanel.tsx` | Folyamatok panel |

## Módosított fájlok

| Fájl | Változás |
|---|---|
| `src/pages/SettingsPage.tsx` | ProcessesPanel import + "Folyamatok" tab |

## Implementáció részletei

### Types

```typescript
interface ProcessStep {
  id: string
  name: string
  type: 'phase' | 'step' | 'branch' | 'parallel' | 'loop' | 'external'
}

interface Process {
  id: string
  name: string
  description: string
  facilityId: string
  steps: ProcessStep[]
  isActive: boolean
  createdAt: string
  updatedAt: string
}

interface ProcessStats {
  phases: number
  steps: number
  branches: number
  parallels: number
  loops: number
  externals: number
}
```

### Step Types

| Típus | Ikon | Szín | Label |
|---|---|---|---|
| phase | layers | teal | Fázis |
| step | chevron | stone | Lépés |
| branch | route | amber | Elágazás |
| parallel | workflow | blue | Párhuzam |
| loop | bolt | purple | Ciklus |
| external | external | rose | Külső |

### Mock adatok

4 folyamat 2 különböző telephelyen:
- Ajtógyártás — Standard (Vác)
- Ajtógyártás — Egyedi (Vác)
- Tokgyártás (Vác)
- Raktári készletezés (Szekszárd, inaktív)

## Build

```bash
npm run build
```

**Eredmény:**
```
✓ 143 modules transformed.
dist/index.html                     0.45 kB │ gzip:   0.29 kB
dist/assets/index-CO4ms72V.css     93.35 kB │ gzip:  14.87 kB
dist/assets/index-ChgX6Vkp.js   1,053.71 kB │ gzip: 238.47 kB
✓ built in 995ms
```

## Megjegyzések

### API integráció

Nincs backend endpoint — mock adatokkal működik. Ha készül API:
- `GET /api/processes?facilityId=` — folyamatok lekérdezés
- `POST /api/processes` — új folyamat
- `PUT /api/processes/{id}` — módosítás
- `DELETE /api/processes/{id}` — törlés
- `POST /api/processes/{id}/duplicate` — duplikálás

### Settings tab elrendezés

Új "Folyamatok" tab a "Munkafolyamat" után.

---

**Állapot:** ✅ KÉSZ
**Blocking:** Nincs
