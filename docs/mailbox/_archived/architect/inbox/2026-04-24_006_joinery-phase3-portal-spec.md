---
id: MSG-ARCH-006
from: root
to: architect
type: task
priority: high
status: READ
created: 2026-04-24
---

# ARCH-006 — Joinery Phase 3 Architecture (Portal integráció)

> Joinery Phase 1+2 DEPLOYED (387 teszt). Phase 3: a Doorstar portálon batch PDF letöltés + anyaglista gombok.
> **Output:** `docs/architecture/SpaceOS_Joinery_Phase3_Architecture_v1.md`

## Kontextus

**Joinery Phase 2 (LIVE):**
- GyartasilapBatch FSM (Pending → Generating → Ready/Failed)
- AnyaglistaPdfBuilder (QuestPDF)
- MinIO WORM storage (COMPLIANCE 365 nap)
- 5 endpoint: batch create, get, download ZIP, anyaglista create, download

**Portal (LIVE):**
- `portal.joinerytech.hu` — Doorstar Portal (React 18, standalone)
- `joinerytech.hu` — Design Portal (Turborepo)
- BFF: Orchestrator (:3000) proxy → Joinery (:5002)

**Ami hiányzik:**
A portálon nincs gomb/UI a batch PDF + anyaglista letöltéshez. A Joinery API endpoint-ok LIVE-ak, de a frontend nem hívja őket.

## Amit a tervdoknak tartalmaznia KELL

1. **Melyik portálon?** — portal.joinerytech.hu (Doorstar) és/vagy joinerytech.hu (Design Portal)?
2. **UI wireframe** — hol jelenjen meg a gomb? Rendelés részletek oldalon? Külön "Dokumentumok" oldal?
3. **BFF route** — kell-e új Orchestrator proxy route? (pl. `/bff/joinery/batch/*`)
4. **Auth** — melyik Keycloak role férhet hozzá?
5. **UX flow** — batch generálás aszinkron (FSM) → polling/SSE az állapothoz?
6. **Presigned URL** — MinIO presigned URL-t a frontend kapja közvetlenül, vagy BFF proxy-n megy?
7. **Fázisolás** — mi Phase 3 MVP, mi Phase 3.5?
8. **Effort becslés**

## Referenciák

- Joinery Phase 2 DONE: `docs/mailbox/joinery/outbox/2026-04-20_051_phase2-batch-anyaglista-done.md`
- Joinery API: `docs/knowledge/context/JOINERY_CONTEXT.md`
- Doorstar Portal: `spaceos-doorstar-portal/CLAUDE.md`
- Orchestrator BFF routes: `docs/knowledge/context/ORCH_CONTEXT.md`

## Definition of Done

- [ ] `docs/architecture/SpaceOS_Joinery_Phase3_Architecture_v1.md` létrehozva
- [ ] Fenti 8 pont lefedve
- [ ] Outbox response küldve
