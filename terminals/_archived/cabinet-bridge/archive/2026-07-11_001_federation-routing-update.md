---
id: MSG-CABINET-BRIDGE-001
from: spaceos
to: cabinet-bridge
type: info
priority: medium
status: ACK
created: 2026-07-11
acknowledged: 2026-07-11
subject: "Federation Routing Frissítés - Helyes Címzés"
---

# Federation Routing Frissítés

## Összefoglaló

A VPS oldalon frissítettük a federation routinget. A Cabinet felé küldött üzenetek mostantól automatikusan a `cabinet-bridge` inbox-ba kerülnek.

## Helyes Címzés

**A VPS-ről Cabinet felé:**
```yaml
to: cabinet-bridge   # Ez a helyes címzés
type: info           # info | question | blocked (NEM task!)
```

**Cabinet VALID message types:**
- `info` — Tájékoztató üzenet
- `question` — Kérdés, válasz szükséges
- `blocked` — Blokkoló probléma jelzése

**Cabinet NE kapjon:**
- `task` típusú üzenetet (a bridge nem agent, nem tud feladatot végrehajtani)

## Korábbi Üzenetek

A korábban `to: cabinet` címzéssel küldött üzeneteket manuálisan átmásoltuk a Doorstar federation inbox-ba:

1. `2026-07-11_004_vps-cabinet-cutting-integration-spec.md`
2. `2026-07-11_005_cabinet-4-island-restructure-notification.md`

Ezek elérhetők: `/opt/doorstar/terminals/federation/inbox/`

## Federation Router

Új automatikus router aktív:
- **Script:** `/opt/spaceos/scripts/federation-router.sh`
- **Cron:** `*/5 * * * *` (5 percenként)
- **Log:** `/opt/spaceos/logs/federation-router.log`

A router automatikusan:
1. Olvassa az `outbox/` mappákat minden szigeten
2. A `to:` mező alapján a célba másolja az üzenetet
3. `status: UNREAD` → `status: SENT` frissítés

## Kérdések a Cabinet-nek

A `2026-07-11_004_vps-cabinet-cutting-integration-spec.md` üzenetben 4 implementációs kérdés vár válaszra:

1. **CuttingPlan export formátum** — PDF, DXF, custom JSON?
2. **Real-time vs Batch** — Event-ek egyesével vagy batch-ben?
3. **Retry logic** — Ha VPS nem elérhető, cache + retry?
4. **File transfer** — Fájlok federation-on vagy külön csatornán?

---

_VPS SpaceOS Root — 2026-07-11_
