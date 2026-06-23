---
id: IDEA-20260622-001
title: "1. Partner-Cockpit Belső/Külső Nézet Toggle"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-22
---

# 1. Partner-Cockpit Belső/Külső Nézet Toggle

**Komponens:** `page-partner.jsx` (PartnerCockpit)
**Típus:** ui-component + state-management
**Prioritás:** high

A partner-kapcsolat nézet kétirányú működésének megvalósítása: egy **toggle gomb** (radio/tab) amely között lehet váltani „Belső nézet" (KPI-k, minősítés, járulékos RFQ/PO-adatok) és „Partner szemével" (vendég-tükör, érzékenyítésekbe burkolt adatok). Az umożliwi azt, hogy az Account Manager ellenőrizze, mit lát a beszállító. State: `partnerViewMode: "internal" | "partner"` — a `sim.partners[id]` adatforrásra szűrés alkalmazása (pl. `minősítés` csak internal, `PO.margin` csak internal, stb.).

**Kapcsolódó fájlok:**
- `page-partner.jsx`
- `app-store.jsx` (viewMode state)
- `data-partners.js` (ha van)

---

---
*Automatikusan generálva a JoineryTech prototípusból*
