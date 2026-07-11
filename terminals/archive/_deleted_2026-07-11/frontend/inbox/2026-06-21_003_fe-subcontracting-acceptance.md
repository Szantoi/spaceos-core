---
id: MSG-FRONTEND-003
from: conductor
to: frontend
type: task
priority: medium
status: DONE
model: sonnet
created: 2026-06-21
content_hash: 52129d3a8e413fc2d8a504155f5fd304fe51ba1950747eca72f18319932dadba
---

# FE-PROC-003: Bérmunka Partner-oldali Elfogadás UI

## Feladat: Bérmunka elfogadás felület a beszállítói portálon

**Prioritás:** MEDIUM
**Típus:** Feature
**Backlog:** PROJECT_STATUS.md 6.2 - #5

---

## Kontextus

A PROJECT_STATUS.md 6.2 backlog szerint a következő lánc-záró feature a **Bérmunka partner-oldali elfogadás** - a bérmunkás partner elfogadja vagy elutasítja a bérmunka megrendelést.

---

## Előzetes lépések

**FONTOS:** Először kutasd fel a meglévő infrastruktúrát!

1. **Backend API keresése:**
   ```bash
   grep -r "subcontract\|bermunka\|outsource" /opt/spaceos/backend/ --include="*.cs"
   ```

2. **Meglévő UI keresése:**
   ```bash
   grep -r "subcontract\|bermunka\|outsource" /opt/spaceos/portal/ --include="*.tsx"
   ```

3. **Ha nincs backend API:** Jelezd BLOCKED státusszal és várd meg a backend implementációt

---

## Elvárt funkciók (ha van backend)

1. **Bérmunka megrendelések listája** a partner portálon
   - Pending elfogadásra váró megrendelések
   - Elfogadott/Elutasított history

2. **Elfogadás/Elutasítás flow**
   - Megrendelés részletek megtekintése
   - Elfogadás gomb + confirmation
   - Elutasítás gomb + indoklás mező

3. **Státusz vizualizáció**
   - Pending (sárga)
   - Accepted (zöld)
   - Rejected (piros)

---

## Definition of Done

- [ ] Kutatás elvégezve (backend API létezik?)
- [ ] Ha BLOCKED: outbox üzenet a blocker-rel
- [ ] Ha OK: Bérmunka lista implementálva
- [ ] Ha OK: Elfogadás/Elutasítás flow
- [ ] `pnpm build` sikeres
- [ ] Nincs TypeScript hiba

---

## Referenciák

- PROJECT_STATUS.md 6.2 szekció
- Beszállítói portál: `src/pages/SupplierPortalPage.tsx` (minta)
