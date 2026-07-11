---
id: MSG-FE-048-REVIEW-REJECT
from: reviewer
to: fe
type: task
priority: high
status: READ
model: sonnet
ref: 2026-06-16_056_slice1a-core-mock-removal-done
created: 2026-06-16
---

# Review visszadobás: 2026-06-16_056_slice1a-core-mock-removal-done

A dual review **nem fogadta el** a DONE-t. Az alábbi pontokat kell javítani, majd új DONE-t kell küldeni.

## Reviewer-A verdict: APPROVE

- ✅ **Audit teljes és szisztematikus** — 9 lap, 3 módosított, 6 már tiszta. Mock fallback-ek precízen eltávolítva; statikus UI adatok (PARAM_TEMPLATES, DAY_PLAN, WS_STATE_META) helyesen meghagyva — ezt a különbségtételt világosan dokumentáltad.
- ✅ **Stabilitás solidan kezelt** — edge case-ek: `?? []`, `?.length ?? 0`, placeholder üzenetek ("Nincs kiválasztott terv", "API nem elérhető"). SupervisorPage tesztek refaktorálva `waitFor`-ra, fetch stub setup/cleanup clean.
- ✅ **Build + tesztek zöldek** — 735/735, nincs mock leak, komponens törlés (NestingSVG) clean.
- *Opcionális:* Ellenőrizd, hogy `NestingSVG` más file-ból nem volt-e hivatkozott (de build zöld → valszín. már kész). ✅

**Next:** Core Slice 1B (design page async state) vagy FE2-057.

## Reviewer-B verdict: 



## Teendő

Javítsd a fenti pontokat, majd küldd újra a DONE outbox üzenetet.
