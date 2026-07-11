---
description: how to run the Orchestrator Epic Execution Pre-flight Check (Phase 0)
---

# Orchestrator – Epic Execution Pre-flight Check (Fázis 0)

**Szerepkör:** Orchestrator
**Trigger:** User Epic végrehajtási kérés
**FSM Output:** Epic státusz → `IN_DEV` (ha minden ellenőrzés zöld)
**Forrás:** `Operative_Process_Framework_Standard.md` — Fázis 0

---

## Lépések

### 1. Kontextus betöltése

Olvasd be az alábbi fájlokat a projekt gyökeréből vagy a kijelölt `docs/` mappából:

```
state.md                    ← Jelenlegi FSM állapot
goal.md                     ← Epic célkitűzés
dependency_map.md           ← Függőségek térképe
```

> Ha `goal.md` nem létezik, a workflow **nem folytatható**. Vonja be a PO/Stakeholdert.

### 2. Pre-flight Ellenőrzőlista

Ellenőrizd az alábbi feltételeket. Ha bármelyik sikertelen, dokumentáld a blockert és állítsd meg.

- [ ] `goal.md` létezik és tartalmaz célmeghatározást
- [ ] `state.md` naprakész — az Epic státusza `BACKLOG_READY`
- [ ] `dependency_map.md` konzisztens (nincs feloldatlan kritikus függőség)
- [ ] Nincs aktív `ESCALATED` státuszú blocker az Epichez
- [ ] Szükséges szerepkörök (Architect, Tech Lead, Dev, QA) rendelkezésre állnak

### 3. Deployment döntés

Határozd meg a végrehajtási módot az Epic mérete alapján:

| Kritérium | Döntés |
|:----------|:-------|
| Epic < 5 Task, 1 domain | **Single-Workspace** mód |
| Epic ≥ 5 Task, több domain | **Multi-Workspace** mód (párhuzamos ágensek) |

### 4. Git Branch létrehozása

// turbo
```powershell
# Cseréld ki az EPIC_ID és EPIC_NAME értékeket
git checkout -b epic/<EPIC_ID>-<epic-name-slug>
```

### 5. Dispatch Fázis 1-be

Küldd el az Architect dispatch üzenetét:

- Sablon: `src/agent-system/database/roles/management/orchestrator/messages/architect_epic_review.message.md` (P5)
- Célpont: `messages/architect/<timestamp>_from-orchestrator_epic-planning.md`
- `state.md` frissítés: Epic → `Planning`

---

## Kimenet

- Git branch létrehozva
- `state.md` frissítve
- Architect dispatch üzenet elküldve

## Következő fázis

→ **Fázis 1:** `architect-epic-planning.md`
