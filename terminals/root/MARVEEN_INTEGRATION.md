# Marveen Integráció — Fejlesztési Napló

> **Forrás:** https://github.com/Szotasz/marveen
> **Kezdés:** 2026-06-21
> **Cél:** SpaceOS agent infrastruktúra megerősítése Marveen mintákkal

---

## Összefoglaló

A Marveen egy Claude Code alapú multi-agent framework, amely inspirációként szolgál a SpaceOS Nexus fejlesztéséhez. A cél nem a teljes Marveen átvétele, hanem a leghasznosabb komponensek adaptálása.

---

## Implementált Komponensek ✅

| Marveen | SpaceOS Nexus | Fájl | Dátum |
|---|---|---|---|
| `pane-state.ts` | Session állapot detektálás | `pipeline/paneState.ts` | 2026-06-21 |
| `auto-restart.ts` | Automatikus újraindítás | `pipeline/autoRestart.ts` | 2026-06-21 |
| `heartbeat.ts` | Óránkénti monitoring | `pipeline/heartbeat.ts` | 2026-06-21 |
| `channel-coordinator.ts` | Channel fallback | `pipeline/channelCoordinator.ts` | 2026-06-21 |
| `channel-provider.ts` | Telegram/Slack/Discord | `pipeline/channelProvider.ts` | 2026-06-21 |
| `team-trust.ts` | Team trust rendszer | `pipeline/teamTrust.ts` | 2026-06-21 |
| Inter-agent messaging | Agent kommunikáció | `pipeline/agentMessages.ts` | 2026-06-21 |
| Message routing | Üzenet továbbítás | `pipeline/messageRouter.ts` | 2026-06-21 |

---

## Folyamatban Lévő Fejlesztések 🚧

### 1. Process Lock + Pending Retries (P1)

**Cél:** Megakadályozni a duplikált session indítást és kezelni a sikertelen műveleteket.

**Marveen referencia:**
- `src/process-lock.ts` — PID fájl alapú lock
- `src/pending-retries.ts` — retry queue SQLite-ban

**SpaceOS implementáció:**
- Fájl: `spaceos-nexus/knowledge-service/src/pipeline/processLock.ts`
- Fájl: `spaceos-nexus/knowledge-service/src/pipeline/pendingRetries.ts`

**Specifikáció:**

```typescript
// processLock.ts
interface ProcessLock {
  acquire(name: string, timeout?: number): Promise<boolean>;
  release(name: string): Promise<void>;
  isLocked(name: string): boolean;
  getHolder(name: string): { pid: number; startedAt: string } | null;
}

// Lock fájl: /tmp/spaceos-lock-<name>.json
// Tartalom: { pid: number, startedAt: string, terminal?: string }
```

```typescript
// pendingRetries.ts
interface RetryEntry {
  id: string;
  operation: 'session_start' | 'message_send' | 'inbox_write';
  payload: Record<string, unknown>;
  attempts: number;
  maxAttempts: number;
  lastError?: string;
  nextRetryAt: string;
  createdAt: string;
}

interface PendingRetries {
  add(entry: Omit<RetryEntry, 'id' | 'attempts' | 'createdAt'>): Promise<string>;
  getNext(): Promise<RetryEntry | null>;
  markSuccess(id: string): Promise<void>;
  markFailed(id: string, error: string): Promise<void>;
  cleanup(olderThanDays: number): Promise<number>;
}
```

**Státusz:** ✅ KÉSZ (2026-06-21)

**Implementált fájlok:**
- `spaceos-nexus/knowledge-service/src/pipeline/processLock.ts`
- `spaceos-nexus/knowledge-service/src/pipeline/pendingRetries.ts`

**Tesztelve:** Build OK, unit tesztek OK

---

### 2. FTS5 Memory Layer (P2)

**Cél:** Gyors full-text keresés a ChromaDB vector search mellé.

**Státusz:** ✅ KÉSZ (2026-06-21)

**Implementált fájlok:**
- `spaceos-nexus/knowledge-service/src/pipeline/memoryStore.ts` — SQLite + FTS5
- `spaceos-nexus/knowledge-service/src/pipeline/hybridSearch.ts` — Unified search API

**Funkciók:**
- `saveMemory(input)` — memory mentés (semantic/episodic/procedural)
- `searchMemories(query)` — FTS5 keresés
- `hybridSearch(query, options)` — FTS5 + recency kombinált
- `unifiedSearch(query, options)` — FTS5 + ChromaDB hybrid
- `runSalienceDecay()` — salience decay alkalmazása
- `cleanupMemories()` — lejárt memóriák törlése
- `getMemoryStats()` — statisztikák

**Tesztelve:** Build OK, FTS5 keresés OK, hybrid search OK

---

### 3. Skill Factory (P3)

**Cél:** Automatikus skill generálás komplex feladatokból.

**Státusz:** ✅ KÉSZ (2026-06-21)

**Implementált fájl:**
- `spaceos-nexus/knowledge-service/src/pipeline/skillFactory.ts`

**Funkciók:**
- `isSkillCreationRequest(message)` — trigger detektálás ("turn this into a skill", "csinálj skill-t")
- `detectSkillCandidate(conversation)` — workflow pattern felismerés
- `generateSkill(candidate)` — SKILL.md generálás és mentés
- `saveCandidateForReview(candidate)` — kandidátus mentés review-ra
- `listSkills()` — telepített skillek listázása
- `getSkillStats()` — statisztikák

**Trigger minták:**
- "turn this into a skill" / "make a skill from this"
- "remember how to do this" / "save this workflow"
- "tanítsd meg magad" / "csinálj skill-t" (magyar)

**Tesztelve:** Build OK, trigger detection OK, candidate detection OK

---

### 4. Mission Control Szinkron (P4)

**Cél:** Marveen Mission Control ↔ Datahaven Dashboard szinkron.

**Státusz:** ✅ KÉSZ (2026-06-21)

**Implementált fájl:**
- `spaceos-nexus/knowledge-service/src/pipeline/missionControl.ts`

**Funkciók:**
- `getFleetSnapshot()` — teljes agent flotta állapot
- `getAgent(id)` — egyedi agent lekérdezés
- `updateAgentStatus(id, status, task)` — Datahaven státusz frissítés
- `delegateTask(from, to, task, priority)` — feladat delegálás inbox üzenettel
- `getPendingDelegations(agentId)` — függő delegálások
- `syncToMarveen(config)` — Marveen cross-sync (opcionális)
- `fetchFromMarveen(config)` — Marveen agent fetch (opcionális)
- `startMissionControl(config)` — sync loop indítás
- `getMarveenCompatibleSnapshot()` — Marveen API formátum
- `healthCheck()` — rendszer egészség ellenőrzés

**API formátum (Marveen kompatibilis):**
```json
{
  "agents": [
    {
      "id": "root",
      "name": "Root",
      "status": "idle",
      "alive": true,
      "paneState": "idle",
      "currentTask": null,
      "messages": { "inbox": 0, "unread": 0 }
    }
  ],
  "timestamp": "2026-06-21T10:22:29.047Z"
}
```

**Tesztelve:** Build OK, fleet snapshot OK, health check OK (Datahaven connected)

---

## Összes Implementált Modul

| # | Modul | Fájl | Funkció | Státusz |
|---|---|---|---|---|
| 1 | Process Lock | `processLock.ts` | Duplikált session megelőzés | ✅ |
| 2 | Pending Retries | `pendingRetries.ts` | Exponential backoff retry | ✅ |
| 3 | Memory Store | `memoryStore.ts` | SQLite + FTS5 memória | ✅ |
| 4 | Hybrid Search | `hybridSearch.ts` | FTS5 + ChromaDB keresés | ✅ |
| 5 | Skill Factory | `skillFactory.ts` | Auto skill generálás | ✅ |
| 6 | Mission Control | `missionControl.ts` | Fleet management + sync | ✅ |

---

## Döntések

| Döntés | Indoklás | Dátum |
|---|---|---|
| **Git audit trail megtartása** | Compliance-ready, verziókövetett | 2026-06-20 |
| **SQLite FTS5 SKIP task storage-ra** | Split-brain kockázat, fájl-alapú marad | 2026-06-20 |
| **Vault encryption SKIP** | .env + .gitignore elegendő | 2026-06-20 |
| **Process Lock ADOPT** | Stabilitás, duplikáció megelőzés | 2026-06-21 |
| **Pending Retries ADOPT** | Resilience, error recovery | 2026-06-21 |
| **FTS5 Memory Layer ADOPT** | Gyors keyword keresés | 2026-06-21 |
| **Skill Factory ADOPT** | Workflow újrafelhasználhatóság | 2026-06-21 |
| **Mission Control ADOPT** | Fleet management, cross-sync | 2026-06-21 |

---

## Referenciák

- **Marveen GitHub:** https://github.com/Szotasz/marveen
- **SpaceOS Nexus:** `/opt/spaceos/spaceos-nexus/`
- **Roadmap:** `/opt/spaceos/docs/agent-infrastructure/ROADMAP.md`
- **Marveen Inspiráció:** `/opt/spaceos/spaceos-nexus/docs/MARVEEN_INSPIRATION.md`

---

*Utolsó frissítés: 2026-06-21 — Integráció befejezve (6/6 modul)*
