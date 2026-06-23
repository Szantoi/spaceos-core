---
id: IDEA-20260622-002
title: "1. Partner Cockpit — Teljesítmény KPI widget (élő szűrés)"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-22
---

# 1. Partner Cockpit — Teljesítmény KPI widget (élő szűrés)

**Komponens:** `page-partner.jsx` / `PartnerCockpit`
**Típus:** ui-component + state-management
**Prioritás:** high

A Partner-kapcsolat nézet "Belső nézet" lapján lévő KPI-kártya (teljesítésszázalék, átlagos átfutási idő, minőségi rata) interaktív szűrhetőséget kap **időszak-választó** (utolsó 30/60/90 nap) és **RFQ-státusz szűrő** (nyitott/teljesített/késedelmes) szerint. Az `sim.partners[partnerId].orders` tömb alapján számított mutatók valós időben frissülnek, a Tailwind progress bar-ok az időszakos teljesítményt szemléltetik.

**Kapcsolódó fájlok:**
- `page-partner.jsx` (PartnerCockpit section)
- `app-store.jsx` (filterPartnerOrdersByPeriod action)
- `data-partners.js` (KPI számítás logika)

---

---
*Automatikusan generálva a JoineryTech prototípusból*
