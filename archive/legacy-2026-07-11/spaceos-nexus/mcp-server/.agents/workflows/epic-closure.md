---
description: how to close an Epic after Architect Sign-off approval (Phase 7)
---

# Epic Closure (Fázis 7)

**Szerepkör:** Knowledge Steward (7.1) + Product Owner (7.2)
**Trigger:** Architect Sign-off APPROVED
**FSM Output:** Epic → `CLOSED_DONE`
**Forrás:** `Operative_Process_Framework_Standard.md` — Fázis 7

---

## Fázis 7.1 — Knowledge Steward: Archiválás és Kalibráció

### 7.1.1 Archiválás

Másold át a teljes Epic mappát az archívumba:

// turbo
```powershell
$EPIC_ROOT = "path/to/epic"
$EPIC_ID = "EPIC-XX"

Copy-Item -Recurse `
  "$EPIC_ROOT" `
  "archived/$EPIC_ID" `
  -Force

Write-Host "Archived: archived/$EPIC_ID"
```

### 7.1.2 Kalibráció a Tech Lead Calibration Instructions alapján

Olvasd be az Epic Review `epic_review.md` Calibration Instructions szekcióját, és hajtsd végre a javasolt globális fejlesztéseket:

**Lehetséges kalibrációs műveletek:**

| Típus | Hova | Példa |
|:------|:-----|:------|
| Template frissítés | `src/agent-system/database/roles/.../templates/` | Új mező hozzáadása |
| Skill frissítés | `src/agent-system/database/roles/.../skills/` | Új skill fájl |
| Standard frissítés | `src/agent-system/database/standards/` | Szabály módosítása |
| Workflow frissítés | `.agents/workflows/` | Ez a mappa |

### 7.1.3 Knowledge Map frissítése

```
src/agent-system/database/knowledge_map.md
```

Frissítsd:
- [ ] Epic státusza → `CLOSED_DONE`
- [ ] Új ADR-ek hozzáadva a térképhez
- [ ] Új/módosított standardok és skillek dokumentálva
- [ ] Tanulságok (Lessons Learned) főbb pontjai kiemelve

### 7.1.4 Definition of Done (7.1)

- [ ] Epic archivált (`archived/<EPIC_ID>/`)
- [ ] Minden calibration instruction végrehajtva
- [ ] `knowledge_map.md` frissítve

---

## Fázis 7.2 — Product Owner: Üzleti Értékelés

### 7.2.1 Epic üzleti értékelése

Értékeld az Epic üzleti eredményét:

- **Goal Alignment:** Teljesítette-e az Epic az eredeti `goal.md`-ben megfogalmazott célokat?
- **Business Value Delivered:** Milyen üzleti értéket teremtett a megvalósítás?
- **User Impact:** Milyen hatással van a végfelhasználókra?

### 7.2.2 Következő Epic javaslatok

A lezárt Epic tapasztalatai alapján:
- Milyen új Epic-ek szükségesek?
- Milyen Backlog tételeket kell átpriorizálni?

### 7.2.3 Product Backlog frissítése

```
product_backlog.md    ← Projekt gyökerében
```

Tevékenységek:
- [ ] Lezárt Epic megjelölve
- [ ] Új Epic javaslatok hozzáadva
- [ ] Prioritások felülvizsgálva

---

## Végső FSM frissítés

- `state.md` frissítés: Epic → `CLOSED_DONE`
- Orchestratornak összefoglaló: `messages/orchestrator/<timestamp>_from-knowledge-steward_epic-closed.md`

---

## Kimenet

| Fájl | Útvonal |
|:-----|:--------|
| Archivált Epic | `archived/<EPIC_ID>/` |
| Frissített Knowledge Map | `src/agent-system/database/knowledge_map.md` |
| Frissített Product Backlog | `product_backlog.md` |

## Az Epic lezárult ✅

Az Orchestrator a következő BACKLOG_READY Epicet indíthatja.
→ **Fázis 0:** `orchestrator-epic-execution.md`
