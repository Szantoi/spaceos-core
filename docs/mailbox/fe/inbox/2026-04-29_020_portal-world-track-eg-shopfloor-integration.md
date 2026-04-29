---
id: MSG-FE-020
from: root
to: fe
type: task
priority: high
status: UNREAD
ref: MSG-FE-019-DONE
created: 2026-04-29
---

# FE-020 — Portal World Track E+G: Shop Floor + Integration (Day 14–18)

> **Tervdok:** `docs/tasks/active/SpaceOS_Portal_World_Architecture_v4_final.md` — Section 9, 10
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **Előfeltétel:** FE-019 ✅ (205 teszt, Track B+C+D) + ORCH-085 ✅ (254 teszt, Track F BFF)
> **Ez az UTOLSÓ Portal World task!**
> **Használhatsz sub-agent-eket** ha szükséges

---

## Track E: Shop Floor (/w/shopfloor/) (~4 nap)

### ShopFloorPinGate

- PIN belépés form (`/w/shopfloor/login`)
- `/bff/shopfloor/pin/login` hívás
- Sikeres PIN → shop floor dashboard redirect
- chrome: 'none' (kiosk mode — nincs sidebar/header)

### PinEntry component

- 4-8 digit PIN input (nagy gombok, touch-friendly)
- Machine ID megjelenítés
- Error handling (rossz PIN → hibaüzenet)

### TaskList (/w/shopfloor/tasks)

- Aktív feladatok listája az adott gépen
- Manufacturing task kártyák (EdgeBanding / CNC)
- Státusz badge + prioritás szín

### TaskDetail (/w/shopfloor/task/:id)

- Task részletek (anyag, méret, gép, worker)
- Státusz transition gombok: Start → Complete / Fail
- `/bff/shopfloor/task/:id/status` polling (10s refresh)

---

## Track G: Integration (~2 nap)

### Full flow smoke tesztek

- Login → WorldHome → Sales → OrderDetail → Production → CuttingPlan → Manufacturing
- Shop Floor: PIN login → TaskList → TaskDetail → Complete

### Lighthouse audit

- Target: ≥90 Performance, Accessibility, Best Practices
- Ha <90: azonosítsd a bottleneck-et (img optimize, lazy load, stb.)

### Bundle budget audit

- `pnpm size-limit` — 350KB brotli limit ellenőrzés
- Ha túllépi: code splitting vagy lazy import javítás

### CONTRACT_ISSUES drain

- Minden nyitott CI-bejegyzés ellenőrzése
- Ha van BFF dependency: jelezd a DONE-ban

---

## Tesztek (40+)

**Shop Floor (25+):** PinEntry, PIN login/logout, TaskList, TaskDetail, status polling, kiosk mode
**Integration (15+):** full flow smoke, bundle audit, Lighthouse mock

## Definition of Done

- [ ] Shop Floor: PinGate + PinEntry + TaskList + TaskDetail
- [ ] Kiosk mode (chrome: 'none')
- [ ] Status polling (10s refresh)
- [ ] Integration smoke tesztek
- [ ] Bundle budget ≤ 350KB
- [ ] `pnpm build` 0 error
- [ ] `pnpm test` ≥ 245 pass (205 + 40 új)
- [ ] `pnpm lint` 0 error
- [ ] Outbox DONE — jelezze ha Portal World DEPLOY-ra kész
