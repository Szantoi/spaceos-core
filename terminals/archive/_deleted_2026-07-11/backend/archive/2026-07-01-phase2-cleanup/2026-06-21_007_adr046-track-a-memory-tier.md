---
id: MSG-BACKEND-007
from: root
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: ADR-046
created: 2026-06-21
content_hash: 99fae8b4789e30ba48efe7abc4ed8ff7ea3848ba079612fb28c9203233731f15
---

# ADR-046 Track A: Memory Tier Management

## Kontextus

Az ADR-046 (Marveen Cold Start Strategy) alapján implementáld a **Track A: Memory Tier Management** komponenst.

**ADR dokumentum:** `docs/adr/ADR-046_marveen-cold-start-strategy.md`

## Feladat

Bővítsd a meglévő `memoryStore.ts`-t tier szemantikával:

### A.1 — Tier oszlop hozzáadása

```sql
ALTER TABLE memories ADD COLUMN tier TEXT NOT NULL DEFAULT 'hot'
  CHECK (tier IN ('hot', 'warm', 'cold', 'shared'));

ALTER TABLE memories ADD COLUMN promoted_from TEXT;
ALTER TABLE memories ADD COLUMN promotion_count INTEGER NOT NULL DEFAULT 0;
ALTER TABLE memories ADD COLUMN last_promotion_at TEXT;
```

### A.2 — Tier-aware save/query

```typescript
interface TieredMemory {
  id: number;
  tier: 'hot' | 'warm' | 'cold' | 'shared';
  type: MemoryType;
  content: string;
  terminal?: string;
  salience: number;
  createdAt: string;
  accessedAt: string;
}

// Új függvények:
async function saveTieredMemory(input: MemorySaveInput): Promise<TieredMemory>
async function queryByTier(terminal: string, tiers: string[]): Promise<TieredMemory[]>
```

### A.3 — Tier promotion logic

```typescript
const TIER_POLICIES = {
  hot: { maxAge: '48h', decayRate: 0.15 },
  warm: { maxAge: '14d', decayRate: 0.05 },
  cold: { maxAge: '365d', decayRate: 0.01 },
  shared: { maxAge: 'forever', decayRate: 0 }
};

async function promoteMemory(id: number, newTier: string, reason: string): Promise<void>
```

### A.4 — Tier-specific decay rates

Módosítsd a `decaySalience()` függvényt, hogy tier-specifikus decay rate-et használjon.

### A.5 — Shared memories table

```sql
CREATE TABLE IF NOT EXISTS shared_memories (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  content TEXT NOT NULL,
  keywords TEXT NOT NULL DEFAULT '',
  salience REAL NOT NULL DEFAULT 0.7,
  created_by TEXT NOT NULL,
  created_at TEXT NOT NULL DEFAULT (datetime('now')),
  accessed_at TEXT NOT NULL DEFAULT (datetime('now'))
);
```

### A.6 — Unit tests

Írj teszteket a tier műveletekhez: `__tests__/memoryStore.tier.test.ts`

## Fájlok

| Fájl | Művelet |
|---|---|
| `src/pipeline/memoryStore.ts` | MODIFY — tier support |
| `__tests__/memoryStore.tier.test.ts` | CREATE — tier tests |

## Definition of Done

- [ ] Tier oszlop hozzáadva a memories táblához
- [ ] `saveTieredMemory()` és `queryByTier()` implementálva
- [ ] `promoteMemory()` implementálva
- [ ] Tier-specifikus decay működik
- [ ] Shared memories tábla létrehozva
- [ ] Unit tesztek zöldek
- [ ] Build PASS
- [ ] DONE outbox üzenet elküldve

## Referenciák

- ADR-046: `docs/adr/ADR-046_marveen-cold-start-strategy.md` (Section 3.1, 3.2)
- Meglévő memoryStore: `spaceos-nexus/knowledge-service/src/pipeline/memoryStore.ts`
- Prototípus referencia: `spaceos-nexus/knowledge-service/src/coldStart.prototype.ts`
