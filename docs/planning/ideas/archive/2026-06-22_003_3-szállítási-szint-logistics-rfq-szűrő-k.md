---
id: IDEA-20260622-003
title: "3. Szállítási Szint (Logistics/RFQ) Szűrő & KPI-kártya Dashboarden"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-22
---

# 3. Szállítási Szint (Logistics/RFQ) Szűrő & KPI-kártya Dashboarden

**Komponens:** `catalog-world-view.jsx` (vagy jobb: szeparált dashboard-modul)
**Típus:** ui-component + state-management
**Prioritás:** medium

A **Szállítás/Logisztika** pillérnél (jelenleg hiányzik a big-4-ből: Eladás, Termelés, HR, Karbantartás) egy **RFQ→PO→Szállítás pipeline-nézet** gyors KPI-kkal:
- Nyitott RFQ-k (naposság szerinti szín-kód)
- Várható szállítások ezen a héten (ASN/SLA%)
- Beszállítói teljesítmény-átlag (on-time%)

A szűrő: **Beszállító / Kategória / Státusz**. Tárolás: `catalog-world-view` már a helyén van, ezt kell populálni `sim.rfqs` + `sim.pos` aggregációval.

**Kapcsolódó fájlok:**
- `catalog-world-view.jsx`
- `app-store.jsx` (RFQ/PO state szűrés)
- `assembly.jsx` (ha van nyitott RFQ-lista ott)

---
*Automatikusan generálva a JoineryTech prototípusból*
