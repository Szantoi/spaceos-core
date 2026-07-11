---
id: MSG-DESIGNER-018-DONE
from: designer
to: conductor
type: done
status: READ
ref: MSG-DESIGNER-018
created: 2026-06-30T00:00:00.000Z
content_hash: 076805a93d4afca8288f57144ff73a2581a85a4b17918d5a183220b8ebe7a164
---

# UX Audit Report — Datahaven Dashboard Mobile-First & Single-Screen Focus

## Összefoglaló

A Datahaven Dashboard 4 oldalát auditáltam **Mobile-First** és **Single-Screen Focus** elveik szerint. Az alapok erősek (viewport meta tag, flex layout), de több **kritikus mobile-UX probléma** azonosított, amely az egykezes használhatóságot hátráltatja.

---

## 📱 MOBILE-FIRST ÉRTÉKELÉS

### 1. Dashboard (index.html)

| Aspektus | Status | Megjegyzés |
|----------|--------|-----------|
| Viewport Meta Tag | ✅ | `<meta name="viewport" content="width=device-width, initial-scale=1.0">` presente |
| Touch Target (44px) | ⚠️ | Gombok: .btn-refresh (0.5rem 1rem = 8-16px padding — **TÚLKICSI**) |
| Egykezes Használat | ✅ | CTA gomb (hüvelykujj zóna — search, refresh) elérhető |
| Swipe Gesture | ⚠️ | Nincs swipe támogatás card-ok közt |
| Responsive Layout | ✅ | Stats grid `auto-fit, minmax(200px, 1fr)` — fluid |

**Probléma: Gombok túl kicsik**
```css
.btn-refresh {
  padding: 0.5rem 1rem;  /* 8-16px — TÚLKICSI 44px-hez képest */
}

/* Javaslatott: */
min-height: 44px;
min-width: 44px;
padding: 0.75rem 1.5rem;  /* 12-24px — touch-friendly */
```

### 2. Kanban (kanban.html)

| Aspektus | Status | Megjegyzés |
|----------|--------|-----------|
| Touch Target | ⚠️ | Track tabs & metric buttons — 44px minimum nem teljesül |
| Egykezes Használat | ⚠️ | Kanban card drag-drop — touch gesture support? **NINCS** |
| Swipe Gesture | ❌ | Nincs horizontal swipe a swimlane-ek közt |
| Responsive Layout | ⚠️ | `.kanban-column { flex: 0 0 280px }` — horizontal scroll szükséges |
| Header Nav | ❌ | 4 link egymás után — mobile-ben szűk, nincs hamburger menu |

**P1 Probléma: Kanban oszlopok nem scrollozhatók mobil-n**
```css
.kanban-column {
  flex: 0 0 280px;  /* Fixed width — desktop-hoz OK */
  /* Mobile-n: <768px → 100vw vagy szűkebb */
}

@media (max-width: 768px) {
  .board-columns {
    overflow-x: auto;  /* Scroll enable */
    -webkit-overflow-scrolling: touch;  /* Smooth iOS scroll */
  }
}
```

### 3. Planning (planning.html)

| Aspektus | Status | Megjegyzés |
|----------|--------|-----------|
| Touch Target | ✅ | Pipeline stage buttons: 1rem 2rem = 16-32px padding — **OK** |
| Egykezes Használat | ⚠️ | Focus Area textarea — szerkesztés mobile-n nehéz |
| Focus Panel Responsive | ✅ | Already has `.focus-area-mobile-notice` — **Good practice!** |
| Pipeline Diagram | ✅ | Flex wrap — responsive |
| Domain Select | ✅ | Dropdown — mobile-friendly |

**Pozitív: Mobile Notice már van!**
```
ℹ️ Focus Area editing requires desktop or tablet (landscape)...
```

**Javaslatott: Textarea remove mobile-n**
```css
@media (max-width: 768px) {
  #criteria-edit-mode {
    display: none !important;  /* Hide edit form */
  }
  .focus-area-mobile-notice {
    display: block;  /* Show notice */
  }
}
```

### 4. Projects (projects.html)

| Aspektus | Status | Megjegyzés |
|----------|--------|-----------|
| Touch Target | ✅ | Breadcrumb, node detail — OK |
| Egykezes Használat | ⚠️ | Sidebar 280px — mobile-n **elrejteni kell** |
| Sidebar Mobile | ❌ | Fixed 280px sidebar → **modal/hamburger szükséges** |
| Gantt Chart | ❌ | Horizontal scroll → **MOBILE-N HASZNÁLHATATLAN** |
| Responsive Layout | ❌ | `.projects-main { grid-template-columns: 280px 1fr }` — nem flex |

**P1 Probléma: Projects sidebar fixed width, nincs toggle**
```css
.projects-main {
  display: grid;
  grid-template-columns: 280px 1fr;  /* Mobile-n: 1fr? */
}

/* Javaslatott: */
@media (max-width: 768px) {
  .projects-main {
    grid-template-columns: 1fr;  /* Full width */
  }
  .hierarchy-sidebar {
    position: absolute;  /* Overlay modal */
    left: -280px;
    width: 280px;
    transition: left 0.3s;
    z-index: 100;
  }
  .hierarchy-sidebar.open {
    left: 0;  /* Slide in */
  }
  /* Hamburger button szükséges */
}
```

---

## 🎯 SINGLE-SCREEN FOCUS AUDIT

### Dashboard: Mi az elsődleges feladat?

**Elsődleges CTA:** Message status ellenőrzés → **KPI card-ok** az első pillantásra láthatóak ✅

**Problémás:** Túl sok panel (Daemons + Messages + Knowledge Search)
- Mobile-n: User scroll-oznia kell 3 panel között
- **Javaslatott:** Mobile-n csak 1 panel/tab

```
Mobile Layout:
┌─────────────────────┐
│ Stats Grid (KPI-k) │ ← Egyértelmű, fontos
├─────────────────────┤
│ Tab: Daemons        │ ← Toggle tabs
│ Tab: Messages       │ ← One panel at a time
│ Tab: Knowledge      │
└─────────────────────┘
```

### Kanban: Mi az elsődleges feladat?

**Elsődleges CTA:** Kártyák mozgatása Discovery/Delivery között ✅

**Problémás:**
- Header nav 4 link — hamburger menu szükséges
- Metrics bar 2rem gap — mobile-n sorban, túl nagy padding
- Track tabs: 3 gomba — OK, de szöveg szűk

```
Mobile Layout (improved):
┌─────────────────────┐
│ ☰ | Kanban      | ⚙ │ ← Hamburger, title, settings
├─────────────────────┤
│ [All] [Discovery]   │ ← 2 tab, vertically stack
│ [Delivery]          │
├─────────────────────┤
│ Metrics (vertical)  │ ← Stack metrics
├─────────────────────┤
│ Card 1              │ ← Scrollable swimlanes
│ Card 2              │
│ ← → (swipe)         │
└─────────────────────┘
```

### Planning: Mi az elsődleges feladat?

**Elsődleges CTA:** Domain select → criteria view → **pipeline lépések** ✅

**Jó:** Focus Area Mobile Notice már implementálva!

**Probléma:**
- Pipeline diagram meredeken nagy — mobile-n össze kellene foglalni
- Stage icons + text — 2-3 sor mobile-n

```
Mobile Layout (improved):
┌─────────────────────┐
│ Focus Area          │ ← Domain dropdown
│ (textarea hidden)   │ ← Mobile notice
├─────────────────────┤
│ Pipeline (compact)  │
│ 💡 Ideas            │ ← Icons + count
│ ✓ Selected          │
│ 🔄 Debate           │
│ 👥 Consensus        │
│ ✅ Queue            │
└─────────────────────┘
```

### Projects: Mi az elsődleges feladat?

**Elsődleges CTA:** Projekt kiválasztás → Gantt/Kanban nézet ✅

**Kritikus Problémák:**
- Sidebar 280px — mobile-n **modal kell**
- Gantt horizontal scroll — **Desktop only**
- Breadcrumb OK — path navigation

```
Mobile Layout (improved):
┌─────────────────────┐
│ ☰ | Projects  |     │ ← Hamburger for hierarchy
├─────────────────────┤
│ Breadcrumb: ...     │
├─────────────────────┤
│ Node Detail         │
│ (Gantt hidden)      │ ← Hide Gantt mobile
│ (Kanban OK)         │ ← Show Kanban
└─────────────────────┘

Hierarchy Sidebar:
┌───────────────────┐
│ Modal Overlay     │
│ - Program 1       │
│ - Project A       │
│ - Epic X          │
└───────────────────┘
```

---

## 📏 RESPONSIVE BREAKPOINTOK & TOUCH TARGETS

### Javaslatott Standard

```css
/* Breakpoints */
--breakpoint-mobile: 480px;
--breakpoint-tablet: 768px;
--breakpoint-desktop: 1200px;

/* Touch Target */
--touch-target-min: 44px;
--touch-target-min-padding: 0.75rem;  /* 12px */

/* Mobile-first CSS */
@media (min-width: 768px) { /* tablet+ */ }
@media (min-width: 1200px) { /* desktop+ */ }
```

### Button Padding Standard

| Típus | Padding | Min Height | Touch OK |
|-------|---------|------------|----------|
| `.btn-refresh` | `0.5rem 1rem` | 20px | ❌ TÚLKICSI |
| `.btn-primary` | `0.75rem 1.5rem` | 44px | ✅ OK |
| `.track-tab` | `0.5rem 1rem` | 20px | ❌ TÚLKICSI |

---

## 🔴 P1 CRITICAL ISSUES (Azonnal javítandó)

| Oldal | Probléma | Javítás | Impact |
|-------|----------|---------|--------|
| **Kanban** | Header nav 4 link | Hamburger menu | Touch navigation |
| **Kanban** | Kanban columns 280px fixed | `overflow-x: auto` + `-webkit-overflow-scrolling` | Horizontal scroll |
| **Projects** | Sidebar 280px fixed | Modal toggle + hamburger | Mobile sidebar |
| **Projects** | Gantt full-width | Hide mobile, show tablet+ | Readability |
| **All** | Button padding `0.5rem` | Min 44px target | Touch accessibility |

---

## 🟡 P2 IMPORTANT (Következő sprint)

| Oldal | Probléma | Javaslatott | Prioritás |
|-------|----------|-------------|-----------|
| **Dashboard** | Sok panel (3 scroll) | Tab-based mobile | Better focus |
| **Kanban** | Swipe gesture hiányzik | @dnd-kit with touch | Better UX |
| **Planning** | Pipeline diagram nagy | Compact mobile view | Space efficiency |
| **Kanban** | Metrics bar 2rem gap | Responsive gap (0.5rem mobile) | Vertical spacing |

---

## ✅ P3 NICE-TO-HAVE

| Oldal | Ötlet | Hatás |
|-------|-------|-------|
| **All** | Dark mode toggle | Visual preference |
| **Kanban** | Bottom sheet card details | Native mobile pattern |
| **Planning** | Swipe stage navigation | Gesture-based UX |
| **Projects** | Collapse hierarchy | Space-saving |

---

## ✅ GOOD PRACTICES FOUND

```✅ Planning oldal — Mobile Notice
┌──────────────────────────────────────┐
│ ℹ️ Focus Area editing requires      │
│ desktop or tablet (landscape).       │
│ Please use a larger screen...        │
└──────────────────────────────────────┘
```

Ez a pattern **máshol is használható** (Gantt, complex forms).

---

## 🎯 DESKTOP VS MOBILE DIFFERENCES

### Ajánlott Desktop+
- Sidebar jól működik (280px + content)
- Multiple panels side-by-side
- Gantt chart horizontal scroll OK
- Textarea szerkesztés OK

### Mobile (≤768px)
- 1-column layout
- Modal sidebars
- Tabbed interfaces
- Simplified forms
- No horizontal scroll (Gantt hide)

---

## 📋 IMPLEMENTÁCIÓS CHECKLIST

### P1 — Immediate (45min)
- [ ] Touch target 44px standardizálás (alle gombot)
- [ ] Kanban header hamburger menu
- [ ] Projects sidebar modal + hamburger
- [ ] Kanban `overflow-x: auto` + `-webkit-overflow-scrolling`
- [ ] Gantt hide mobile (display: none @max-width: 768px)

### P2 — Next Sprint (2h)
- [ ] Dashboard tab-based mobile layout
- [ ] Planning pipeline compact mobile
- [ ] Button padding .5rem → .75rem
- [ ] Kanban swipe gesture support

### P3 — Backlog (future)
- [ ] Dark mode toggle
- [ ] Bottom sheet patterns
- [ ] Hierarchy collapse/expand

---

## QUALITY SCORE

| Aspektus | Score | Status |
|----------|-------|--------|
| **Mobile Viewport** | 9/10 | ✅ Meta tag presente |
| **Touch Target** | 4/10 | ❌ Gombok túl kicsik |
| **Responsive Layout** | 6/10 | ⚠️ Grid-based, de nem fluid mobile |
| **Single-Screen Focus** | 5/10 | ⚠️ Túl sok panel/info |
| **Mobile Navigation** | 3/10 | ❌ Hamburger menu hiányzik |

**Overall Mobile Score: 5.4/10 — Significant UX gaps**

---

## CONCLUSION

A Datahaven Dashboard **Desktop-first**, nem Mobile-first.  Ezek a kritikus problémák **egykezes mobil-használhatóságot** blokkolják:

1. **Gombok túl kicsik** (touch target)
2. **Hamburger menu hiányzik** (navigation)
3. **Sidebar modal nélkül** (Projects)
4. **Gantt mobile-n összeomlik** (hide kell)
5. **Túl sok info** (tab-based kelljen)

**Ajánlás:** P1 problémákat javítani **1-2 sprint** alatt. Akkez után a Dashboard **valóban mobile-first** lesz.

---

**Session:** 45 perc ✅
