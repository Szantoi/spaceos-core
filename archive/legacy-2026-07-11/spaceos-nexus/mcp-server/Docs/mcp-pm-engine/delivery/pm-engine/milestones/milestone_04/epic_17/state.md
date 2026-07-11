---
id: epic-pm-engine-17
title: "Epic 17: Multi-Domain Demonstráció — Iparág-Agnosztikus Bizonyítás"
type: epic
milestone: M04
project: pm-engine
project_id: mcp-pm-engine
status: planned
fsm_workflow_id: "agile-epic-lifecycle-v1"
fsm_state: "BACKLOG_READY"
fsm_retry_count: 0
created: 2026-03-04
assignee: backend_developer
depends_on: EPIC-13
---

# 🌍 EPIC-17: Multi-Domain Demonstráció — Iparág-Agnosztikus Bizonyítás

## Célkitűzés

Bizonyítani, hogy a JoineryTech MCP Server valóban **iparág-agnosztikus**: egy második,
teljesen eltérő domainen (marketing) a rendszer kódfejlesztés nélkül, kizárólag
`database/roles/` mappa feltöltésével és seeder futtatásával teljes értékűen üzemel.

**Ez a program végső validációja** — a „kalap metafora" gyakorlatban igazolva.

---

## Szcenárió

```
Kiindulás: csak database/roles/engineering/ létezik
Demo után: database/roles/marketing/ + database/knowledge/marketing/ is feltöltve

Elvégzett lépések:
  1. database/roles/marketing/campaign_manager/ mappa létrehozva
       ├── campaign_manager.role.md
       ├── campaign_manager.schema.yaml
       ├── campaign_manager.runbook.md
       ├── workflows/delivery.workflow.md
       └── templates/campaign_brief.template.md
  2. database/knowledge/marketing/campaign_management.knowledge.md létrehozva
  3. npm run seed — semmi más
  4. bootstrap_agent(domain="marketing", role="campaign_manager") → teljes kontextus
  5. search_experience(query="content brief marketing") → epizodikus keresés marketing domain-ben
```

Nincs kódfejlesztés, nincs konfiguráció-fájl módosítás, nincs szerver-újraindítás szükséges
(hot-reload esetén), nincs hardcoded engineering domain hivatkozás.

---

## Teszt Domain: Marketing / Campaign Manager

### Minimális role csomag tartalma

**`campaign_manager.role.md`** — identitás, felelősségek, hatáskör

**`campaign_manager.schema.yaml`** — engedélyek:
```yaml
mcp_tool_permissions:
  - bootstrap_agent
  - search_knowledge
  - get_workflow
  - get_template
  - store_experience
  - search_experience
  - submit_artifact
```

**`delivery.workflow.md`** — campaign launch FSM:
```
BRIEF → REVIEW → APPROVED → LIVE → COMPLETED
```

**`campaign_brief.template.md`** — kimenet sablon

---

## Mit Ellenőriz Ez az Epic

| Ellenőrzés | Hogyan |
|:-----------|:-------|
| Seeder felveszi az új role-t | `SELECT * FROM roles WHERE domain='marketing'` → 1 sor |
| RBAC helyesen tölti a marketing engedélyeket | `bootstrap_agent` → `allowed_tools` tartalmaz `get_template`-t |
| Knowledge indexelés működik | `search_knowledge("campaign brief")` → releváns chunk |
| Epizodikus memória domain-szeparált | `store_experience(domain="marketing")` → `search_experience(domain="marketing")` |
| REST API megmutatja a marketing domain-t | `GET /api/program` → marketing epics megjelennek |
| Nincs hardcoded domain hivatkozás a kódban | `grep -r "engineering" src/` → csak test fixture-ökben |

---

## Feladatok

| Típus | ID | Feladat | Becslés | Állapot |
|:-:|:---|:--------|:--------|:--------|
| `Arch` | T17-01 | Marketing domain role csomag tartalmának megtervezése (minimális, de valószerű) | 0.5 nap | 🗓️ Planned |
| `Dev`  | T17-02 | `database/roles/marketing/campaign_manager/` mappa és fájlok létrehozása | 0.5 nap | 🗓️ Planned |
| `Dev`  | T17-03 | `database/knowledge/marketing/campaign_management.knowledge.md` létrehozása | 0.5 nap | 🗓️ Planned |
| `QA`   | T17-04 | `npm run seed` futtatása — nincs hiba, rows count nő | 0.5 nap | 🗓️ Planned |
| `QA`   | T17-05 | E2E teszt: `bootstrap_agent("marketing", "campaign_manager")` teljes kontextust ad | 0.5 nap | 🗓️ Planned |
| `QA`   | T17-06 | E2E teszt: `store_experience` + `search_experience` marketing domain-ben | 0.5 nap | 🗓️ Planned |
| `QA`   | T17-07 | Kód audit: nincs hardcoded `"engineering"` vagy fájlútvonal a core logikában | 0.5 nap | 🗓️ Planned |

**Összesített becslés:** ~3.5 nap

---

## Definition of Done

- [ ] `bootstrap_agent(domain="marketing", role="campaign_manager")` visszaadja: role, runbook, allowed_tools, workflow, template.
- [ ] `search_knowledge("marketing campaign")` releváns knowledge chunk-ot ad vissza.
- [ ] `store_experience` + `search_experience` a marketing domain-ben is működik.
- [ ] `GET /api/program` tartalmazza a marketing role-t (ha PM Engine épített erre).
- [ ] A szerver kódjában nincs hardcoded domain-specifikus hivatkozás.
- [ ] Az összes meglévő engineering domain E2E teszt változtatás nélkül zöld marad.

---

## Blokkolók / Kockázatok

| Kockázat | Valószínűség | Mitigáció |
|:---------|:-------------|:----------|
| Valamelyik tool hardcoded `engineering` domain-t vár | Alacsony | EPIC-13 (T13-06) már feltárja ezt; itt csak bizonyítás |
| Marketing knowledge tartalom gyenge → rossz keresési eredmény | Közepes | Demo célra elég 3-4 paragrafus valószerű marketing tartalom |
