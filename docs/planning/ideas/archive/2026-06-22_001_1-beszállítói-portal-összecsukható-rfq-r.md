---
id: IDEA-20260622-001
title: "1. Beszállítói portal összecsukható RFQ-részletek panel"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-22
---

# 1. Beszállítói portal összecsukható RFQ-részletek panel

**Komponens:** `page-supplier.jsx` → SupplierQuoteCard alkomponens
**Típus:** ui-component
**Prioritás:** high

A Supplier Portal RFQ-listájában jelenleg csak egy sor mutatja a megrendelést. Valósíts meg egy **"Részletek"** gomb → accordion/collapsible panel, amely az alábbi adatokat jeleníti meg inline (a sor alatt):
- Anyagtípus + mennyiség
- Szállítási dátum + helyszín
- Előzetes jellegzetességek (felület-kezelés, tolerancia)
- **„Ajánlat beadása"** gomb a panel alján

Az accordion záródjon, ha más RFQ-ra kattintsz.

**Kapcsolódó fájlok:**
- `page-supplier.jsx`
- `data-rfq.js` (sim.quotes szimulációs adatok)

---

---
*Automatikusan generálva a JoineryTech prototípusból*
