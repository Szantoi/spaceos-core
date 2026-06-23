---
id: IDEA-20260622-002
title: "2. Assembly Planning — Terv vs. Gyártás real-time szinkron-jelzés"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-22
---

# 2. Assembly Planning — Terv vs. Gyártás real-time szinkron-jelzés

**Komponens:** `assembly.jsx`  
**Típus:** ui-component + state-management  
**Prioritás:** high

Az assembly terv-vs-aktuális nézet (3-es már feldolgozva részlegesen) kiterjesztése: **szinkron-állapot badge** az assembly fejlécben:
- Zöld badge: **"Szinkronban"** (terv == gyártás állapot)
- Sárga badge: **"Eltérés: X soron"** (kattintva megnyílik a differnciáló modal, amely:
  - Sor-szintű hiányzó komponens, mennyiség-túllépés vagy státusz-különbség
  - **"Terv frissítése"** vagy **"Gyártás jóváhagyása"** gomb az összevetéshez
- Piros badge: **"Kritikus eltérés"** (pl. teljes alkatrész hiányzik)

Gyorsítja: az üzemi leadert a terv-gyártás deviancia-kezeléséhez.

**Kapcsolódó fájlok:**
- `assembly.jsx`
- `app-store.jsx` (assembly FSM + gyártás-naplózás state)
- `data-bom.js` (BOM diff kalkuláció)

---

---
*Automatikusan generálva a JoineryTech prototípusból*
