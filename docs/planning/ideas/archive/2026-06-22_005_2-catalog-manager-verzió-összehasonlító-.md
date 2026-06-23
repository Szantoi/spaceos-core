---
id: IDEA-20260622-005
title: "2. Catalog Manager — Verzió-összehasonlító (diff) nézet"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-22
---

# 2. Catalog Manager — Verzió-összehasonlító (diff) nézet

**Komponens:** catalog-manager.jsx  
**Típus:** ui-component  
**Prioritás:** medium

A `catalog-manager.jsx`-ben egy új **"Verzió-históia"** panel, amely a kiválasztott termékkonfiguráció összes mentett verzióját listázza (dátum, szerző, nagyobb változások szöveges summary). Kattintásra egy **modal/side-panel** nyílik, amely az aktuális és az előző verzió között egy vizuális **diff** mutat (mely mezők változtak, régi vs. új érték, jelenlegi árhatás). Segít nyomon követni a termék-evolúciót és visszaállítani, ha szükséges.

**Kapcsolódó fájlok:**
- catalog-manager.jsx
- app-store.jsx (productVersions entitás vagy kiterjesztés)

---

---
*Automatikusan generálva a JoineryTech prototípusból*
