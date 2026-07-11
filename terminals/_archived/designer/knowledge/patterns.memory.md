# Designer — Patterns Memory

> **TTL:** 14 nap (WARM) | **Frissítve:** 2026-06-30

---

## UX Design Principles — Core Reference

**Teljes dokumentum:** `docs/knowledge/patterns/UX_DESIGN_PRINCIPLES.md`

### 1. Mobile-First, Egykezes Használat

- Minden UI tervezése **mobil nézetből indul**
- Touch target: **minimum 44×44px**
- Fontos gombok a **hüvelykujj zónájában** (képernyő alsó harmada)
- Swipe gestures ahol releváns
- Kerüld a hover-only interakciókat

### 2. PC Felület: Informatív, De Nem Zsúfolt

- Desktop verzió **bővíti** a mobilt, nem duplikálja
- Hierarchikus információ megjelenítés
- Sidebar/panel layout, de mindig **egy fő akció** per képernyő
- Whitespace használata

### 3. Single-Screen Focus

- **"Csak az jelenjen meg, ami az aktuális munkához kell"**
- Progresszív felfedés (progressive disclosure)
- Max **7±2 elem** egyszerre látható
- Context-aware UI: szerep és feladat határozza meg a tartalmat

### 4. Dark-First, Ipari Esztétika

- **Sötét téma alapértelmezett**
- WCAG 2.1 AA kontraszt: 4.5:1 normál szöveg
- Státusz színek:
  - ✅ Zöld: OK, sikeres, online
  - ⚠️ Sárga/Narancs: Figyelmeztetés
  - ❌ Piros: Hiba, kritikus, offline

### 5. Arculat Konzisztencia

- Design tokenek: `var(--color-primary)` ✅, hard-coded ❌
- Komponens újrafelhasználás
- Egységes ikonográfia és tipográfia

---

## UI Review Checklist (gyors)

- [ ] Touch target ≥ 44×44px
- [ ] CTA-k hüvelykujj zónában
- [ ] Max 7±2 elem látható
- [ ] WCAG 2.1 AA kontraszt
- [ ] Design tokenek használva

---

## Referenciák

- `docs/knowledge/patterns/UX_DESIGN_PRINCIPLES.md` — Teljes spec
- `docs/joinerytech/ui.jsx` — JoineryTech UI minták
- `terminals/designer/outbox/2026-06-30_014_datahaven-design-system-done.md` — Design System
