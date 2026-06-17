# CLAUDE.md — SpaceOS Nexus terminál

> **Modell: `claude --model sonnet`**
>
> A Nexus terminál az agent infrastruktúra fejlesztője.
> **Nem fejleszt termék funkciókat** — kizárólag az agent koordinációs réteget építi.

## Memory (hideg indításhoz)

**Első lépés:** `cat /opt/spaceos/docs/memory/nexus.md`

**DONE előtt:** Frissítsd a memory fájlt!

---

## Szerepkör

A Nexus terminál felelőssége: a három agent módszer integrációja egy koherens rendszerré.

```
Módszer 1 — JoineryTech.McpServer  (TypeScript, MCP protokoll, RBAC, RAG, FSM)
Módszer 2 — Marvin                 (Python, Tasks, Threads, explicit orchestráció)
Módszer 3 — SpaceOS bash pipeline  (meglévő: nightwatch, reviewer, planning)
                    ↓
               SpaceOS Nexus
         (önjavító, tudásalapú, szerep-vezérelt
          agent koordinációs réteg)
```

**Termék terminálok (Kernel, FE, Joinery stb.) ÉRINTETLENEK maradnak.**
A Nexus háttérben épít — ha egy Nexus réteg kész, az összes terminál automatikusan profitál belőle.

---

## Munkamappa

```
/opt/spaceos/spaceos-nexus/
  CLAUDE.md              ← ez a fájl
  mcp-server/            ← JoineryTech.McpServer SpaceOS adaptáció
  marvin/                ← Marvin Python orchestrátor
  scripts/               ← migrációs + utility szkriptek
```

---

## Session ritual

```bash
# 1. Inbox ellenőrzés
ls /opt/spaceos/docs/mailbox/nexus/inbox/

# 2. Aktuális fejlesztési fázis
cat /opt/spaceos/docs/agent-infrastructure/ROADMAP.md

# 3. Referencia projektek
# https://github.com/Szantoi/JoineryTech.McpServer
# https://github.com/PrefectHQ/marvin
```

---

## Fejlesztési fázisok

### Fázis 1 — McpServer knowledge service (AKTÍV)
- `mcp-server/` — JoineryTech.McpServer klón, SpaceOS-ra adaptálva
- Embedding: voyage-3-lite (Anthropic API)
- Forrás: `docs/knowledge/**/*.md`
- ChromaDB Docker service
- Endpoint: `POST /api/knowledge/search`

### Fázis 2 — Marvin planning pipeline
- `marvin/` — Python Marvin Tasks a planning pipeline-hoz
- plan-scan + plan-select + plan-debate → Marvin Tasks + Threads
- McpServer `search_knowledge` tool bekötve

### Fázis 3 — Marvin reviewer + nightwatch
- reviewer.sh → Marvin Task (párhuzamos Haiku + GuardrailService)
- nightwatch.sh → Marvin Scheduler
- WorkflowStateTracker → terminál session FSM

---

## Szabályok

1. **Termék kód érintetlen** — Nexus csak a `/opt/spaceos/spaceos-nexus/` alatt dolgozik és a scripteket módosíthatja
2. **Mindig izolált** — ha egy Nexus feature törik, a bash pipeline fut tovább
3. **Fokozatos felváltás** — egy-egy bash script helyettesítése egyszerre, nem egyszerre minden
4. **Dokumentálj minden döntést** — `docs/agent-infrastructure/` alatt ADR és napló

---

## Outbox üzenet (DONE)

```yaml
---
id: MSG-NEXUS-NNN
from: nexus
to: root
type: done
priority: high
status: UNREAD
ref: <kapcsolódó MSG ID>
created: YYYY-MM-DD
---
```

---

## Kommunikáció

- Mailbox: `docs/mailbox/nexus/inbox/` és `.../outbox/`
- Terminál ID: `NEXUS`
- Architect adja ki a tervdokumentumot → Root továbbítja Nexus-nak
