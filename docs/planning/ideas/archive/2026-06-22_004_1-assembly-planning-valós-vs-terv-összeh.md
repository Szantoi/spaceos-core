---
id: IDEA-20260622-004
title: "1. Assembly Planning — Valós vs. Terv összehasonlítás görgetővel"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-22
---

# 1. Assembly Planning — Valós vs. Terv összehasonlítás görgetővel

**Komponens:** assembly.jsx  
**Típus:** ui-component + state-management  
**Prioritás:** high

Az `assembly.jsx` jelenleg csak a tervet mutatja. Vegyél fel egy **"Valós gyártás"** tab-ot, amely az ugyanazon assembly megrendeléshez rendelt tényleges bevételezéseket (`receivedParts`) és gyártási naplókat (`productionLog`) hasonlítja össze a tervvel. Egy **split-view** (terv balra / valós jobbra) vagy sorba rendezett kártyák, ahol a eltérés (mennyiség, dátum, költség) piros/zöld highlight-tal jelenik meg.

**Kapcsolódó fájlok:**
- assembly.jsx
- app-store.jsx (receivedParts, productionLog lekérdezés)

---

---
*Automatikusan generálva a JoineryTech prototípusból*
