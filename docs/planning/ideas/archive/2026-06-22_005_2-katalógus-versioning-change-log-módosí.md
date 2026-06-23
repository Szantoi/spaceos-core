---
id: IDEA-20260622-005
title: "2. Katalógus versioning & Change-log módosítások"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-22
---

# 2. Katalógus versioning & Change-log módosítások

**Komponens:** `catalog-manager.jsx`  
**Típus:** state-management  
**Prioritás:** medium

A `catalog-manager.jsx` jelenleg az aktuális termékkonfiguráció kezelésére fókuszál. Új feature: minden termékkatalógus-módosítás (`price`, `availability`, `description`) egy `auditLog[]` tömbbe kerül (`{timestamp, userId, field, oldValue, newValue, reason}`). A `CatalogAudit` aloldal (Beállítások → Katalógus) ezt az előzményeket jeleníti meg szűrökkel. Az ötlet egyszerű, 1-2 óra: az `updateProduct` action duplexen írjon az `audit`-ba is, egy táblázat-lap pedig olvaszon belőle.

**Kapcsolódó fájlok:**
- `catalog-manager.jsx`
- `data-catalog.js` (store audit[])
- `app-store.jsx` (updateProduct + audit commit)

---

---
*Automatikusan generálva a JoineryTech prototípusból*
