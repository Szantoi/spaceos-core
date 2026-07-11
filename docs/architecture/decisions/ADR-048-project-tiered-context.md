# ADR-048: Project-Level Tiered Context System

**Status:** PROPOSED
**Date:** 2026-06-24
**Author:** Root Terminal
**Deciders:** Root, Architect

## Context

A `get_project_context` MCP tool ~10.8k tokent ad vissza, mert beolvassa:
- `SpaceOS_Vision_Master.md` (~8k token)
- `knowledge/INDEX.md` (~1.5k token)
- `Codebase_Status.md` (~1.5k token)

Ez pazarló és lassítja a terminálokat. Emellett a projekt kontextus statikus — nem veszi figyelembe, hogy:
- Mi történt **ma** (hot) vs. **múlt héten** (warm) vs. **2 hónapja** (cold)
- Melyik **projekten** dolgozik épp a terminál
- Mi a **releváns** kontextus az adott feladathoz

## Decision

Kiterjesztjük a meglévő **tiered memory rendszert** (ADR-046) projekt szintre.

### Hierarchia

```
TERMÉK (SpaceOS)
  └── shared tier: vízió, 5 Golden Rule, architektúra alapok
        │
        ├── PROJEKT (spaceos/cutting)
        │     ├── hot:  mai taskok, aktuális blockerek
        │     ├── warm: heti sprint döntések, teszt számok
        │     └── cold: ADR-ek, API contractok
        │
        ├── PROJEKT (spaceos/sales)
        │     ├── hot:  ...
        │     ├── warm: ...
        │     └── cold: ...
        │
        └── PROJEKT (datahaven)
              ├── hot:  MCP auth fix, TaskMessageBox
              ├── warm: DDD refactor, 126 teszt
              └── cold: ADR-047, MCP protocol
```

### Tier Policies (projekt szint)

| Tier | TTL | Decay | Tartalom | Betöltés |
|------|-----|-------|----------|----------|
| **shared** | ∞ | 0% | Termék vízió (tömörített), 5 Golden Rule | Mindig |
| **hot** | 48h | 15%/nap | Mai/tegnapi DONE, blockerek, friss döntések | Auto (aktív projekt) |
| **warm** | 14d | 5%/nap | Heti sprint, teszt számok, döntések | On-demand |
| **cold** | 365d | 1%/nap | ADR-ek, API contract, architekturális minták | Explicit lekérdezés |

### Adatstruktúra

```typescript
interface ProjectContext {
  product: string;           // "spaceos" | "datahaven"
  project: string;           // "cutting" | "sales" | "nexus"
  tier: 'hot' | 'warm' | 'cold';

  // Dinamikusan generált
  summary: string;           // ~200-500 token tömörítés
  activeEpics: string[];     // Epic ID-k
  recentDecisions: string[]; // Utolsó N döntés
  blockers: string[];        // Aktuális blockerek
  testCounts?: {             // Ha releváns
    backend: number;
    frontend: number;
  };

  // Metadata
  generatedAt: string;
  validUntil: string;
  sourceFiles: string[];     // Honnan lett generálva
}
```

### SQLite séma bővítés

```sql
CREATE TABLE IF NOT EXISTS project_context (
  id INTEGER PRIMARY KEY,
  product TEXT NOT NULL,        -- 'spaceos', 'datahaven'
  project TEXT NOT NULL,        -- 'cutting', 'sales', 'nexus'
  tier TEXT NOT NULL,           -- 'hot', 'warm', 'cold', 'shared'
  summary TEXT NOT NULL,        -- Tömörített kontextus
  metadata TEXT,                -- JSON: activeEpics, blockers, stb.
  salience REAL DEFAULT 0.5,
  created_at TEXT DEFAULT CURRENT_TIMESTAMP,
  updated_at TEXT DEFAULT CURRENT_TIMESTAMP,
  expires_at TEXT,
  UNIQUE(product, project, tier)
);

CREATE INDEX idx_project_context_lookup
  ON project_context(product, project, tier);
```

### MCP Tool-ok

**1. `get_project_summary` (ÚJ)**
```typescript
// Input
{ project: "spaceos/cutting", tier?: "hot" | "warm" | "cold" }

// Output (~300-500 token)
{
  project: "spaceos/cutting",
  tier: "hot",
  summary: "Cutting Q3 aktív. Mai: MSG-CUTTING-055 DONE (nesting fix). Blocker: nincs. Tesztek: 994 BE.",
  activeEpics: ["EPIC-CUTTING-Q3"],
  recentDecisions: ["Q3 expansion approved (2026-06-22)"],
  testCounts: { backend: 994, frontend: 941 }
}
```

**2. `refresh_project_context` (ÚJ)**
```typescript
// Manuális frissítés triggerelése
{ project: "spaceos/cutting", tier: "hot" }
```

**3. `get_project_context` (DEPRECATED)**
```typescript
// Marad visszafelé kompatibilitásra, de warning-ot ad
// "Warning: This tool returns ~10k tokens. Use get_project_summary instead."
```

### Automatikus frissítés

```
┌─────────────────────────────────────────────────────┐
│  PIPELINE EVENTS                                    │
├─────────────────────────────────────────────────────┤
│  DONE outbox → hot tier frissül                     │
│  BLOCKED outbox → hot tier + blocker lista          │
│  ADR merged → cold tier frissül                     │
│  Napi digest → warm tier összefoglaló               │
│  Heti retrospective → warm→cold promotion           │
└─────────────────────────────────────────────────────┘
```

### Tömörítési stratégia

A pipeline Haiku-val tömöríti a nyers fájlokat:

```
SpaceOS_Vision_Master.md (8000 token)
  → Haiku tömörítés
  → shared tier summary (500 token)

Codebase_Status.md (1500 token)
  → projekt szétválasztás
  → cutting/hot (200 token)
  → sales/warm (150 token)
  → nexus/hot (180 token)
```

## Alternatives Considered

### A) Statikus összefoglaló fájlok
- Manuálisan karbantartott `PROJECT_SUMMARY.md` minden projekthez
- **Elvetés oka:** Nem skálázik, elavul, extra karbantartás

### B) Vector search a teljes kontextuson
- RAG-alapú keresés a nagy fájlokban
- **Elvetés oka:** Nem garantál konzisztens kontextust, latency

### C) Nincs változás, terminálok figyeljenek
- Warning a CLAUDE.md-ben (jelenlegi állapot)
- **Elvetés oka:** Nem megoldás, továbbra is pazarló

## Consequences

### Pozitív
- **Token megtakarítás:** ~10k → ~500 token per lekérdezés
- **Dinamikus:** A kontextus követi a projektek állapotát
- **Skálázható:** Új projektek automatikusan kapnak kontextust
- **Konzisztens:** Minden terminál ugyanazt a tömörített kontextust látja

### Negatív
- **Implementációs költség:** ~4-6 óra fejlesztés
- **Haiku API költség:** Tömörítés token költsége (de egyszer, cache-elve)
- **Komplexitás:** Új tábla, új pipeline lépések

## Implementation Plan

### Phase 1: Core (2-3 óra)
1. SQLite séma: `project_context` tábla
2. `projectContextStore.ts`: CRUD műveletek
3. `get_project_summary` MCP tool

### Phase 2: Pipeline Integration (2 óra)
4. DONE watcher → hot tier frissítés
5. Napi digest → warm tier generálás
6. ADR merge hook → cold tier frissítés

### Phase 3: Tömörítés (1-2 óra)
7. Haiku-alapú summarization
8. Cache invalidation logika

## References

- ADR-046: Tiered Memory System
- `memoryStore.ts`: Meglévő tiered memory implementáció
- `EPICS.yaml`: Projekt/epic definíciók
