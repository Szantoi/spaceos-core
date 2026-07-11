# CLAUDE.md — SpaceOS (Operatív Központ)

> **SpaceOS = Operatív Központ + Orchestration**
>
> - Multi-island koordináció és felügyelet
> - Stratégiai tervezés és kutatás
> - Federation kommunikáció kezelése
> - Cabinet-VPS bridge

---

## SZIGET FELELŐSSÉG

| Feladat | Leírás |
|---------|--------|
| **Multi-Island Monitoring** | Federation Control Center — minden sziget felügyelete |
| **Strategic Planning** | Roadmap, prioritás, ügyfél döntések |
| **Architecture** | ADR-ek, minták, konzultáció |
| **Knowledge Base** | Tudásbázis karbantartás |
| **Research** | Új technológiák, exploráció |
| **Federation** | Szigetek közötti kommunikáció, Cabinet-VPS bridge |

**SpaceOS NEM végez:**
- Platform kód írást (→ JoineryTech)
- Agent infra fejlesztést (→ Nexus)
- Ügyfél-specifikus munkát (→ Doorstar)

---

## FEDERATION CONTROL CENTER

**Dashboard URL:** https://datahaven.joinerytech.hu/federation.html

**API Endpointok:**
```bash
# Teljes multi-island áttekintés
curl http://localhost:3457/api/federation

# Health check minden szigetre
curl http://localhost:3457/api/federation/health

# Federation üzenetek
curl http://localhost:3457/api/federation/messages

# Specifikus sziget
curl http://localhost:3457/api/federation/island/nexus
```

**Szigetek:**
| Sziget | Port | Szerep | Szín |
|--------|------|--------|------|
| Nexus | 3456 | Agent Infrastructure | Indigo |
| JoineryTech | 3458 | Platform Dev | Emerald |
| Doorstar | 3460 | Customer Instance | Amber |
| SpaceOS | 3462 | Orchestration | Violet |
| Cabinet | — | External CAD | Slate |

---

## TERMINÁLOK

| Terminál | Felelősség | Mit TUD | Mit NEM TUD |
|----------|------------|---------|-------------|
| **root** | Stratégiai döntések | Roadmap, Prioritások | Implementáció |
| **conductor** | Koordináció | Task dispatch, Review | Kód írás |
| **architect** | Arch konzultáció | ADR-ek, Minták | Napi fejlesztés |
| **librarian** | Tudásbázis | Dokumentáció | Kód |
| **explorer** | Kutatás | Keresés, Elemzés | Módosítás |

---

## TUDÁSÁRAMLÁS

```
NEXUS (Agent Infra)
    │
    │ MCP tools, Pipeline
    ▼
SPACEOS (Orchestration) ← TE ITT VAGY
    │
    │ ADR-ek, Minták
    ▼
JOINERYTECH (Platform Dev)
    │
    │ Konfiguráció
    ▼
DOORSTAR (Customer)
```

**Szabályok:**
- Felfelé NEM megy tudás
- SpaceOS közvetít a szigetek között
- Federation mappa: `terminals/federation/`

---

## 4-SZIGET ARCHITEKTÚRA

| Sziget | Szerep | Port | Fókusz |
|--------|--------|------|--------|
| **Nexus** | Agent Infra | 3456-3457 | MCP, Pipeline, Knowledge-service |
| **SpaceOS** | Orchestration | 3462-3463 | Stratégia, Koordináció, Kutatás |
| **JoineryTech** | Platform Dev | 3458-3459 | .NET, React, Üzleti logika |
| **Doorstar** | Customer | 3460-3461 | Konfiguráció, 6-STAGE |

---

## TERMÉK HIERARCHIA

| Termék | Típus | Leírás |
|--------|-------|--------|
| **Nexus** | Agent Platform | CLI integráció, Memory, Goals |
| **TaskOS** | Feladatmenedzsment | Workflow, Scheduling |
| **Orbit** | ERP Eszköztár | CRM, HR, Inventory |
| **SpaceOS** | Építészeti OS | Térszervezés, BIM |
| **JoineryTech** | Faipari Platform | Orbit + SpaceOS |
| **Doorstar** | Ügyfél Instance | JoineryTech config |

---

## SESSION RITUAL

```bash
# 1. Saját inbox
ls terminals/root/inbox/

# 2. Federation üzenetek
ls terminals/federation/inbox/

# 3. Terminál státusz
ls terminals/*/inbox/ | head -10

# 4. Knowledge service
curl -s localhost:3462/health
```

---

## KAPCSOLÓDÓ DOKUMENTUMOK

- `docs/architecture/4-ISLAND-ARCHITECTURE.md` — 4 sziget áttekintés
- `docs/architecture/decisions/ADR-064-*.md` — Termék hierarchia
- `docs/architecture/ISLAND-CLEANUP-PLAN.md` — Tisztítási terv

---

_SpaceOS — Orchestration & Research — 2026-07-11_
