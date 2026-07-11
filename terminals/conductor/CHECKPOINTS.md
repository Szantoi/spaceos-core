# Conductor Checkpoints

> Stratégiai döntési pontok és deadline-ok

---

## Q2 2026 Checkpoints

### 📅 Június 30, 2026 — Doorstar Soft Launch GO/NO-GO

**Döntés:** Q3 Cutting Expansion implementáció indítása

**Értékelési szempontok:**
- Doorstar Soft Launch sikeres volt-e?
- Kritikus bugok vannak-e production-ben?
- Backend/Frontend terminálok kapacitása elérhető-e Q3 Week 1-től?

**HA GO:**
1. Backend inbox kiadása (MSG-030-032):
   - MSG-BACKEND-030: Track A — Customer Portal Quote API + Email (4 nap)
   - MSG-BACKEND-031: Track B — Pricing Rule Engine (3 nap)
   - MSG-BACKEND-032: Track C — ShopFloor Machine Queue (2 nap)

2. Frontend inbox kiadása (MSG-018-020):
   - MSG-FRONTEND-018: Track A — Public Quote Form + Status Tracking (4 nap)
   - MSG-FRONTEND-019: Track B — Trade World Pricing Integration (3 nap)
   - MSG-FRONTEND-020: Track C — ShopFloor API Integration (2 nap)

3. Track végrehajtási sorrend koordinálása:
   - **Szekvenciális:** A DONE → B start → B DONE → C start (ha erőforrás szűk)
   - **Párhuzamos:** A+B párhuzamosan, C utána (ha erőforrás van)

4. Root értesítése: GO döntés + timeline konfirmálás

**HA NO-GO:**
1. Q3 Doorstar stabilizációra fókusz
2. 2. ügyfél expansion tolva Q4-re
3. Root értesítése: NO-GO döntés + új timeline (Q4)

**Ref:**
- Root approval: MSG-CONDUCTOR-022
- Proposal: MSG-CONDUCTOR-029
- Tracks spec: docs/planning/proposals/q3-cutting-expansion.md

---

## Q3 2026 Checkpoints

### 📅 Szeptember 30, 2026 — Q4 Research Assistant Pilot GO/NO-GO

**Döntés:** Q4 Research Assistant feature pilot implementáció

**Értékelési szempontok:**
- Doorstar Q3 production metrics (scan failure rate, KPI data gaps)
- QR ASN Tracking Phase 2 production deployment sikeres volt-e?
- Van-e valós adat ami indokolja a Research Assistant feature-t?

**HA GO:**
1. Frontend inbox kiadása:
   - Research Assistant feature-specific triggers implementation (~2 nap)
2. Backend inbox kiadása:
   - Research triggers API + YAML config + Haiku integration (~2 nap)
3. Q4 Week 1-2 pilot implementáció

**HA NO-GO:**
1. Research Assistant tolva H1 2027-re
2. Q4 helyette más prioritás (Doorstar stabilizáció, feature refinement)
3. Root értesítése: NO-GO döntés + új timeline (H1 2027)

**Ref:**
- Root approval: MSG-CONDUCTOR-021
- Consensus: docs/planning/archive/2026-06-22_0037_consensus.md

---

_Last updated: 2026-06-23 02:45_
