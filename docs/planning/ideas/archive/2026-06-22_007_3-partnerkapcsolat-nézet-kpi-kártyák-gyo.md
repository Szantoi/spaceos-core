---
id: IDEA-20260622-007
title: "3. Partnerkapcsolat Nézet — KPI-Kártyák Gyorsáttekintő"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-22
---

# 3. Partnerkapcsolat Nézet — KPI-Kártyák Gyorsáttekintő

**Komponens:** `page-partner.jsx` (PartnerCockpit)
**Típus:** ui-component + state-management
**Prioritás:** medium

A partner-profil tetejére add meg a **4 kártyás KPI-sort** (Tailwind grid, `grid-cols-2 sm:grid-cols-4`):
- **Aktív PO-k** (nyitott rendelések száma)
- **Átlagos Teljesítési Idő** (nap, a lezárt PO-k alapján)
- **Min. Ciklusidő** (%), számított érték az `acknowledgePO` / `markPOShipped` között
- **Elmúlt 90 nap Teljesítés** (%), lezárt sikeres PO-k aránya

Az értékeket az `app-store.jsx`-ben egy `calculatePartnerKPI(partnerId)` függvénnyel számolja ki. Tailwind `bg-green-50`, `bg-yellow-50`, `bg-red-50` háttérrel jelöld a státuszt (zöld = jó, sárga = figyelmeztetés, piros = kritikus).

**Kapcsolódó fájlok:**
- `page-partner.jsx`
- `app-store.jsx`
- `data-partners.js`

---
*Automatikusan generálva a JoineryTech prototípusból*
