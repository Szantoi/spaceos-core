---
id: IDEA-20260622-006
title: "2. Katalógus-duplikálás gyorsmenu a sorok végén"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-22
---

# 2. Katalógus-duplikálás gyorsmenu a sorok végén

**Komponens:** catalog-manager.jsx
**Típus:** ui-component | state-management
**Prioritás:** medium

A `catalog-manager.jsx` terméklistájában minden sor végén jelenjen meg egy **"⋯" ikonmenü** (hover/click-trigger), amely tartalmazza:
- **Duplikálás** → klónozza az aktuális terméket (név: "Másolat — [eredeti név]"), az új item azonnal szerkesztésre várakozik (inline edit vagy modal)
- **Törlés** → törlésre vár (SoftDelete jelzés, nem permanent)

Ez gyorsabban megengedik a hasonló termékek létrehozását a manuális bejegyzés helyett.

**Kapcsolódó fájlok:**
- catalog-manager.jsx
- app-store.jsx (duplicateProduct action)

---

---
*Automatikusan generálva a JoineryTech prototípusból*
