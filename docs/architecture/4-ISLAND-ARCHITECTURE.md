# 4-Sziget Architektúra — SpaceOS 2026-07-11

## Áttekintés

A SpaceOS rendszer 4 független szigetre van bontva, mindegyik saját felelősségi körrel és knowledge-service-szel.

```
┌─────────────────────────────────────────────────────────────────┐
│                         VPS (spaceos.hu)                         │
│                                                                  │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐           │
│  │    NEXUS     │  │ JOINERYTECH  │  │   DOORSTAR   │           │
│  │   (infra)    │  │  (platform)  │  │  (customer)  │           │
│  │  port 3456   │  │  port 3458   │  │  port 3460   │           │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘           │
│         │                 │                 │                    │
│         └─────────────────┼─────────────────┘                    │
│                           │                                      │
│                  ┌────────┴────────┐                             │
│                  │    SPACEOS      │                             │
│                  │ (orchestration) │                             │
│                  │   port 3462     │                             │
│                  └─────────────────┘                             │
│                                                                  │
│         ══════════ Federation Bridge ══════════                  │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
                    ┌─────────────────┐
                    │   CABINET VPS   │
                    │   (external)    │
                    └─────────────────┘
```

---

## Szigetek

### 1. Nexus (`/opt/nexus/`)

**Szerep:** Agent Infrastructure Development

**Felelősség:**
- Knowledge-service fejlesztés
- MCP tool implementáció
- Pipeline szkriptek
- Federation protokoll

**Terminálok:**
| Terminál | Szerep |
|----------|--------|
| root | Infra stratégia |
| backend | TypeScript fejlesztés |
| qa | Tesztelés |

**Szolgáltatások:**
| Service | Port |
|---------|------|
| Knowledge Service | 3456 |
| Datahaven | 3457 |

---

### 2. JoineryTech (`/opt/joinerytech/`)

**Szerep:** Faipar SaaS Platform

**Felelősség:**
- 7 modul fejlesztés (CRM, HR, EHS, Kontrolling, Maintenance, QA, DMS)
- Általános platform funkciók
- API endpoints

**Terminálok:**
| Terminál | Szerep |
|----------|--------|
| root | Platform stratégia |
| conductor | Sprint koordináció |
| backend | .NET + Node.js |
| frontend | React UI |
| designer | UI/UX review |

**Szolgáltatások:**
| Service | Port |
|---------|------|
| Knowledge Service | 3458 |
| Datahaven | 3459 |

---

### 3. Doorstar (`/opt/doorstar/`)

**Szerep:** Ügyfél-specifikus Implementation

**Felelősség:**
- Doorstar Kft. specifikus fejlesztések
- 6-STAGE production workflow
- Cabinet-VPS kommunikáció

**Terminálok:**
| Terminál | Szerep |
|----------|--------|
| root | Ügyfél döntések, Cabinet comm |
| conductor | Production koordináció |
| backend | 6-STAGE API |
| frontend | Doorstar UI |

**Szolgáltatások:**
| Service | Port |
|---------|------|
| Knowledge Service | 3460 |
| Datahaven | 3461 |

---

### 4. SpaceOS (`/opt/spaceos/`)

**Szerep:** Orchestration & Research

**Felelősség:**
- 4-sziget koordináció
- Stratégiai tervezés
- Tudásbázis karbantartás
- Federation felügyelet

**Terminálok:**
| Terminál | Szerep |
|----------|--------|
| root | Stratégiai döntések |
| conductor | Kutatási koordináció |
| architect | Architektúra konzultáció |
| librarian | Tudásbázis |
| explorer | Codebase kutatás |

**Szolgáltatások:**
| Service | Port |
|---------|------|
| Knowledge Service | 3462 |
| Datahaven | 3463 |

---

## Federation Protokoll

### Kommunikációs Mappa Struktúra

Minden szigeten:
```
terminals/federation/
├── inbox/      ← Beérkező üzenetek
├── outbox/     ← Kimenő üzenetek
└── archive/    ← Feldolgozott
```

### Üzenet Formátum

```yaml
---
id: MSG-FEDERATION-NNN
from: nexus              # Küldő sziget
to: joinerytech          # Címzett sziget
type: request|response|info|task
priority: critical|high|medium|low
status: UNREAD
created: YYYY-MM-DD
ref: MSG-XXX-NNN         # Kapcsolódó üzenet
---

# Üzenet tartalma
```

### Routing

**Belső szigetek:** Federation Watcher másolja az outbox-ból az inbox-ba.

**Cabinet VPS:** Doorstar szigeten keresztül, Telegram/HTTP bridge-en.

### Watcher

```bash
# Minden 30 másodpercben
/opt/spaceos/scripts/federation-watcher.sh
```

---

## Termék Hierarchia (ADR-064)

```
┌─────────────────────────────────────────────────────────────────────┐
│                           NEXUS                                      │
│                (Agent Orchestration Platform)                        │
│                                                                      │
│   CLI Integration: Claude Code │ Google ADK │ Codex │ ...           │
│   Agent Services:  Identity │ Memory │ Knowledge │ Goals │ ...      │
│                                                                      │
│                        @nexus/platform                               │
└─────────────────────┬───────────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────────────┐
│                           TASKOS                                     │
│              (Általános Feladatmenedzsment Eszköz)                   │
│                                                                      │
│   Tasks │ Workflow │ Scheduling │ Tracking │ Dependencies           │
│                                                                      │
│                        @taskos/core                                  │
└─────────────────────┬───────────────────────────────────────────────┘
                      │
        ┌─────────────┼─────────────┐
        │             │             │
        ▼             ▼             ▼
┌─────────────┐ ┌─────────────┐ ┌─────────────┐
│    Orbit    │ │   SpaceOS   │ │  [Más...]   │
│ @orbit/core │ │@spaceos/core│ │             │
└──────┬──────┘ └──────┬──────┘ └─────────────┘
       │               │
       │    ┌──────────┘
       │    │
       ▼    ▼
┌─────────────────────────────────────────────────────────────────────┐
│                        JOINERYTECH                                   │
│                    (Faipari SaaS Platform)                           │
│                                                                      │
│   Használja: @orbit/core + @spaceos/core (mindkettő @taskos/core-ral)│
│   Asztalos vertikum — bútorgyártás, ajtó/ablak, szekrény             │
│                                                                      │
│                    @joinerytech/platform                             │
└─────────────────────────┬───────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────────────┐
│                         DOORSTAR                                     │
│                  (JoineryTech Ügyfél Instance)                       │
└─────────────────────────────────────────────────────────────────────┘

       │
       │ Orbit önállóan is használható:
       ▼
┌─────────────┐ ┌─────────────┐ ┌─────────────┐
│  [Pékség]   │ │[Vendéglátás]│ │  [Más...]   │
└─────────────┘ └─────────────┘ └─────────────┘
```

### Termék Összefoglaló

| Termék | Típus | Dependency | Leírás |
|--------|-------|------------|--------|
| **Nexus** | Agent Orchestration Platform | — | CLI integráció, identity/memory/goals, federation |
| **TaskOS** | Általános Feladatmenedzsment | Nexus | Tasks, Workflow, Scheduling, Tracking, Dependencies |
| **Orbit** | Általános ERP Eszköztár | TaskOS | CRM, HR, Inventory, Orders, Kontrolling, stb. |
| **SpaceOS** | Építészeti/Térkialakítási OS | TaskOS | Térszervezés, BIM, parametrikus tervezés |
| **JoineryTech** | Faipari SaaS Platform | Orbit + SpaceOS | Asztalos vertikum |
| **Doorstar** | Ügyfél Instance | JoineryTech | Doorstar Kft. ajtógyártó |

**Referencia:** `docs/architecture/decisions/ADR-064-shared-code-inheritance-architecture.md`

---

## Szolgáltatás Indítás

### Indító parancsok

```bash
# Nexus
cd /opt/nexus/nexus-core/knowledge-service
TERMINALS_PATH=/opt/nexus/terminals PORT=3456 node dist/server.js

# JoineryTech
cd /opt/joinerytech/joinerytech-nexus/knowledge-service
TERMINALS_PATH=/opt/joinerytech/terminals PORT=3458 node dist/server.js

# Doorstar
cd /opt/doorstar/doorstar-nexus/knowledge-service
TERMINALS_PATH=/opt/doorstar/terminals PORT=3460 node dist/server.js

# SpaceOS
cd /opt/spaceos/spaceos-nexus/knowledge-service
TERMINALS_PATH=/opt/spaceos/terminals PORT=3462 node dist/server.js
```

### Health Check

```bash
curl http://localhost:3456/health  # Nexus
curl http://localhost:3458/health  # JoineryTech
curl http://localhost:3460/health  # Doorstar
curl http://localhost:3462/health  # SpaceOS
```

---

## Konfiguráció

### Környezeti változók (`.env`)

| Változó | Leírás |
|---------|--------|
| PORT | Knowledge service port |
| TERMINALS_PATH | Terminálok útvonala |
| KNOWLEDGE_BASE_PATH | Tudásbázis útvonala |
| ISLAND_ID | Sziget azonosító |
| ISLAND_ROLE | Sziget szerepe (infra/platform/customer/orchestration) |
| CHROMA_URL | ChromaDB URL |
| API_AUTH_TOKEN | API hitelesítési token (egyedi/sziget!) |
| DASHBOARD_AUTH_TOKEN | Dashboard hitelesítési token |

### Federation Config (`config/federation.yaml`)

Minden szigeten azonos, tartalmazza:
- Sziget definíciók
- Port allokáció
- Routing szabályok

---

## Token Konfiguráció (FONTOS!)

**Minden szigetnek EGYEDI tokenje van!** Tokenek NEM oszthatók meg szigetek között.

### API Tokenek

| Sziget | API Token |
|--------|-----------|
| **Nexus** | `nexus-api-8fef00095193da29d4e3ebaebcda287fb691a9c7` |
| **JoineryTech** | `joinerytech-api-826455c1854cb2b9f2361695e9eb4c3cbf4fbe6a` |
| **Doorstar** | `doorstar-api-7f2ee55831b27bd4664c42548eea88c8825f4e94` |
| **SpaceOS** | `spaceos-api-9a3f71c8d4b2e6f0a1c5d9e7b3f8a2c4d6e0f1a3` |

### Dashboard Tokenek

| Sziget | Dashboard Token |
|--------|-----------------|
| **Nexus** | `dev-token-nexus-dashboard-2026` |
| **JoineryTech** | `dev-token-joinerytech-dashboard-2026` |
| **Doorstar** | `dev-token-doorstar-dashboard-2026` |
| **SpaceOS** | `dev-token-spaceos-dashboard-2026` |

### Token Biztonsági Szabályok

1. **Sziget-izoláció:** Minden sziget csak a saját tokenjét használhatja
2. **Nincs megosztás:** Tokenek NEM másolhatók szigetek között
3. **Rotáció:** Tokenek időszakosan cserélendők (90 naponta)
4. **Audit:** Token használat naplózva

---

## ChromaDB Port Allokáció

| Sziget | ChromaDB Port |
|--------|---------------|
| SpaceOS | 8000 |
| Nexus | 8001 |
| JoineryTech | 8002 |
| Doorstar | 8003 |

---

## Miért 4 Sziget?

1. **Fókusz** — Minden sziget egy konkrét területre koncentrál
2. **Izoláció** — Hibák nem terjednek át
3. **Független fejlesztés** — Nexus fejlődhet anélkül, hogy zavarná a production-t
4. **Skálázhatóság** — Új ügyfelek új szigetként adhatók hozzá
5. **Tiszta felelősség** — Egyértelmű, ki miért felelős

---

---

## Kapcsolódó Dokumentumok

- `/opt/spaceos/docs/migration/4-ISLAND-MIGRATION-2026-07-11.md` — Migráció részletek
- `/opt/spaceos/docs/FEDERATION_PROTOCOL.md` — Federation protokoll
- `/opt/nexus/docs/knowledge/INDEX.md` — Nexus knowledge base
- `/opt/joinerytech/docs/knowledge/INDEX.md` — JoineryTech knowledge base
- `/opt/doorstar/docs/knowledge/INDEX.md` — Doorstar knowledge base

---

_4-Island Architecture v1.7 — TaskOS réteg hozzáadva — 2026-07-11_
