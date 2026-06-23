---
id: IDEA-20260622-002
title: "2. Partnerkapcsolat KPI-kártyák az PartnerCockpit fejlécében"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-22
---

# 2. Partnerkapcsolat KPI-kártyák az PartnerCockpit fejlécében

**Komponens:** `page-partner.jsx` → PartnerMetricsBar
**Típus:** ui-component
**Prioritás:** medium

A Partner-nézet fejlécében (az "Belső nézet" / "Partner szemével" gombok alatt) helyezz el **3 kis kártyát** sor-végig:
- **Teljesítési ráta** (zöld/sárga/piros: PO-k időben szállítva %)
- **Minőségi átlag** (utolsó 6 hó bevételezések átlagos inspection-score)
- **RFQ→PO válaszidő** (nap, zöld ha <3 nap)

Akár csak számok + ikonok, Tailwind `bg-gradient-to-r` + border-lekerekítés stílus.

**Kapcsolódó fájlok:**
- `page-partner.jsx`
- `app-store.jsx` (partner statisztika-levezetés helper)

---

---
*Automatikusan generálva a JoineryTech prototípusból*
