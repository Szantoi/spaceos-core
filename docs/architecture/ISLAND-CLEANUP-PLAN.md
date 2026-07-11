# Sziget Tisztítási Terv — 2026-07-11

## Probléma

A jelenlegi struktúra kaotikus:
- Felelősségek összemosódnak
- Terminálok duplikálódnak szigetek között
- Nem egyértelmű, ki miért felelős
- Tudásáramlás nem irányított

## Célstruktúra

```
┌─────────────────────────────────────────────────────────────────────────┐
│                              NEXUS                                       │
│                    (Agent Infrastructure Dev)                            │
│                                                                          │
│  Felelősség: Knowledge-service, MCP tools, Pipeline, Federation         │
│  Terminálok: root, backend, qa                                          │
│  Tudás: Agent patterns, MCP specs, Pipeline docs                        │
│                                                                          │
│  KIZÁRÓLAG infrastruktúra fejlesztés — NEM üzleti logika!               │
└──────────────────────────────┬──────────────────────────────────────────┘
                               │
                               │ Infrastruktúra szolgáltatás
                               │ (knowledge-service, MCP tools)
                               ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                             SPACEOS                                      │
│                    (Orchestration & Research)                            │
│                                                                          │
│  Felelősség: Stratégia, Kutatás, Architektúra, Koordináció             │
│  Terminálok: root, conductor, architect, librarian, explorer            │
│  Tudás: Architektúra döntések, Roadmap, Általános minták                │
│                                                                          │
│  NEM ír kódot — tervez, koordinál, kutat!                               │
└──────────────────────────────┬──────────────────────────────────────────┘
                               │
                               │ Architektúra guidance,
                               │ Általános minták
                               ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                           JOINERYTECH                                    │
│                      (Faipari SaaS Platform)                             │
│                                                                          │
│  Felelősség: Platform fejlesztés, 7 modul, Üzleti logika               │
│  Terminálok: root, conductor, backend, frontend, designer               │
│  Tudás: Faipari domain, ERP modulok, UI patterns                        │
│                                                                          │
│  Konkrét kód fejlesztés — .NET, React, API-k!                           │
└──────────────────────────────┬──────────────────────────────────────────┘
                               │
                               │ Platform konfiguráció,
                               │ Tenant-specifikus beállítások
                               ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                            DOORSTAR                                      │
│                    (Ügyfél Instance)                                     │
│                                                                          │
│  Felelősség: Doorstar Kft. specifikus konfiguráció, 6-STAGE            │
│  Terminálok: root, conductor, (minimal — legtöbb JoineryTech-ből)       │
│  Tudás: Doorstar workflow, Cabinet-VPS, Ügyfél igények                  │
│                                                                          │
│  Konfiguráció és testreszabás — NEM platform fejlesztés!                │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Terminál Felelősségi Mátrix

### NEXUS (Agent Infra)

| Terminál | Felelősség | Mit TUD | Mit NEM TUD |
|----------|------------|---------|-------------|
| **root** | Infra stratégia, MCP prioritások | Agent patterns, Pipeline | Üzleti domain, UI |
| **backend** | Knowledge-service TS kód | TypeScript, MCP, Node.js | .NET, React, Domain |
| **qa** | Infra tesztelés | Jest, Integration tests | E2E, UI tesztek |

### SPACEOS (Orchestration)

| Terminál | Felelősség | Mit TUD | Mit NEM TUD |
|----------|------------|---------|-------------|
| **root** | Stratégiai döntések, Roadmap | Üzleti prioritások | Implementáció |
| **conductor** | Koordináció, Pipeline | Task dispatch, Review | Kód írás |
| **architect** | Architektúra konzultáció | Minták, ADR-ek | Napi fejlesztés |
| **librarian** | Tudásbázis karbantartás | Dokumentáció | Kód |
| **explorer** | Codebase kutatás | Keresés, Elemzés | Módosítás |

### JOINERYTECH (Platform Dev)

| Terminál | Felelősség | Mit TUD | Mit NEM TUD |
|----------|------------|---------|-------------|
| **root** | Platform stratégia | Modulok prioritása | Infra |
| **conductor** | Sprint koordináció | Task dispatch | Architektúra |
| **backend** | .NET + Node.js API | C#, TypeScript, SQL | React, CSS |
| **frontend** | React UI | React, TypeScript, CSS | .NET, SQL |
| **designer** | UI/UX review | Figma, Design patterns | Kód |

### DOORSTAR (Customer)

| Terminál | Felelősség | Mit TUD | Mit NEM TUD |
|----------|------------|---------|-------------|
| **root** | Ügyfél döntések | Doorstar workflow | Platform dev |
| **conductor** | Production koordináció | 6-STAGE | Új feature dev |

---

## Tudásáramlás

```
NEXUS (Agent Patterns)
    │
    │ MCP tools, Pipeline docs
    ▼
SPACEOS (Architektúra)
    │
    │ ADR-ek, Általános minták
    ▼
JOINERYTECH (Domain Knowledge)
    │
    │ Platform config, Workflow
    ▼
DOORSTAR (Ügyfél specifikus)
```

**Szabályok:**
1. Felfelé NEM megy tudás — Doorstar nem tanítja a JoineryTech-et
2. Oldalra sem — JoineryTech és Nexus nem kommunikál közvetlenül
3. SpaceOS a közvetítő — minden inter-island kommunikáció rajta megy át

---

## Tisztítandó Elemek

### SpaceOS-ból TÖRÖLNI

```
terminals/
  ├── backend           → TÖRLÉS (JoineryTech-ben van)
  ├── backend-2         → TÖRLÉS
  ├── chat-root         → TÖRLÉS (nem használt)
  ├── frontend          → TÖRLÉS (JoineryTech-ben van)
  ├── frontend-2        → TÖRLÉS
  ├── nexus             → TÖRLÉS (külön sziget)
  ├── test-backend      → TÖRLÉS
  ├── monitor           → MARAD (de átgondolni)
  ├── reviewer          → TÖRLÉS (automatikus szkript)
  └── _legacy_archive/  → TÖRLÉS (régi szemét)
```

### SpaceOS-ban MARAD

```
terminals/
  ├── root              ← Stratégiai döntések
  ├── conductor         ← Koordináció
  ├── architect         ← Architektúra konzultáció
  ├── librarian         ← Tudásbázis
  ├── explorer          ← Kutatás
  └── federation/       ← Inter-island kommunikáció
```

### datahaven-web, backend, frontend mappák

**Kérdés:** Ezek SpaceOS-ban maradjanak vagy JoineryTech-be kerüljenek?

**Javaslat:**
- `datahaven-web` → **NEXUS** (agent infra dashboard)
- `backend/` → **TÖRLÉS** SpaceOS-ból (JoineryTech-ben a kód)
- `frontend/` → **TÖRLÉS** SpaceOS-ból (JoineryTech-ben a kód)

---

## Implementációs Lépések

### Phase 1: SpaceOS Tisztítás

1. Terminálok törlése/archiválása
2. `backend/` és `frontend/` mappák törlése
3. `datahaven-web` áthelyezése Nexus-ba (ha még nincs)
4. Dokumentáció frissítése

### Phase 2: CLAUDE.md Frissítés

Minden szigeten frissíteni:
- Mit TUD a sziget
- Mit NEM TUD
- Honnan kap tudást
- Kinek ad tudást

### Phase 3: Knowledge Service Konszolidáció

1. Nexus-ban a MASTER knowledge-service
2. Többi sziget symlink-kel vagy npm package-ként használja
3. Sziget-specifikus config (.env)

### Phase 4: Federation Protokoll

1. `terminals/federation/` mappa minden szigeten
2. Üzenet routing szabályok
3. Audit trail

---

## Döntésre Vár

1. **datahaven-web hova kerüljön?** Nexus vagy marad SpaceOS?
2. **Monitor terminál kell-e?** Vagy automatikus szkript elég?
3. **Reviewer terminál kell-e?** Vagy teljesen automatikus?

---

_ISLAND-CLEANUP-PLAN v1.0 — 2026-07-11_
