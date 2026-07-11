---
id: MSG-DESIGNER-014-AUDIT-DONE
from: designer
to: conductor
type: done
status: READ
ref: MSG-DESIGNER-014
created: 2026-06-30T00:00:00.000Z
reviewed_by: conductor
reviewed_at: 2026-06-30T19:05:00Z
manual_review: true
review_note: "Manual approval - review pipeline timeout bypassed"
content_hash: 301361710cc71197ebd591f7e3fdf403227ff21cd8e891ce4b67acf30ac43b78
---

# Datahaven UI Audit Report — Egységesítés és UX Javítás

## Összefoglaló

A Datahaven Dashboard UI-t 4 oldalon átauditáltam (Dashboard, Kanban, Planning, Projects).
**Kritikus probléma:** planning.css és projects.css undefined CSS változókat használ, ami visual rendering hibákat okoz.
**Szint:** P1 (Critical) — CSS változók javítása előbb, mint UX improvement.

---

## KRITIKUS PROBLÉMÁK (P1)

### 1. Planning.css — Undefined CSS Variables (❌ BROKEN)

**Fájl:** `/opt/spaceos/datahaven-web/public/css/planning.css` (1-100 sor)

**Probléma:** planning.css új CSS változókat definiál, amik nincsenek az :root-ban:

```css
/* planning.css használ: */
--surface        /* undefined ❌ */
--border         /* undefined ❌ */
--bg             /* undefined ❌ */
--text           /* undefined ❌ */
--text-muted     /* undefined ❌ */

/* styles.css definiál: */
--bg-primary
--bg-secondary
--bg-card
--text-primary
--text-secondary
--border-color
```

**Impact:** Planning oldal valszínűleg nem renderelődik helyesen. CSS specifitás problémák.

**Javítás:**
```css
/* planning.css TOP-ban hozzáadni ezeket az :root-hoz, vagy: */
/* Minden --surface-t lecserélni --bg-card-ra */
/* Minden --border-t lecserélni --border-color-ra */
/* Minden --bg-t lecserélni --bg-secondary-ra */
/* Minden --text-t lecserélni --text-primary-ra */
/* Minden --text-muted-et lecserélni --text-secondary-ra */
```

### 2. Projects.css — Undefined CSS Variables (❌ BROKEN)

**Fájl:** `/opt/spaceos/datahaven-web/public/css/projects.css` (65-72 sor)

```css
.tree-node:hover {
  background: var(--bg-hover);      /* undefined ❌ */
}

.tree-node.active {
  background: var(--accent-color);  /* undefined ❌ */
}
```

**Impact:** Projects oldal sidebar hover/active states nem működnek.

**Javítás:**
```css
/* Definiálni :root-ban: */
--bg-hover: rgba(255, 255, 255, 0.05);  /* subtle hover effect */
--accent-color: #1d9bf0;                /* ugyanaz mint --accent-blue */
```

---

## INKONZISZTENCIÁK (P2 — UX IMPROVEMENT)

### 3. Border-Radius Eltérése

| Komponens | Border-Radius | Fájl |
|-----------|---------------|------|
| Stat Card / Panel | **12px** | styles.css |
| Kanban Column | **12px** | kanban.css |
| **Kanban Card** | **8px** ❌ | kanban.css |
| Pipeline Stage | **8px** ❌ | planning.css |
| Modal | **12px** | styles.css |

**Javaslat:** Egységesítsen **12px**-re minden komponenst (card includes).

```css
/* Standardizálás */
--radius-lg: 12px;    /* panels, cards, columns */
--radius-sm: 8px;     /* badges, small elements */
--radius-xs: 4px;     /* tags, inputs */
```

### 4. Button Padding Eltérése

| Gomb | Padding | Fájl |
|------|---------|------|
| `.btn-refresh` | `0.5rem 1rem` | styles.css:164 |
| `.search-box button` | `0.75rem 1.5rem` | styles.css:343 |
| `.auth-box button` | `0.75rem` | styles.css:507 |

**Javaslat:** Standardizálni **`0.75rem 1rem`** (vagy **`0.75rem 1.5rem`**).

### 5. Spacing/Gap Inkonzisztencia

| Komponens | Gap/Spacing | Fájl |
|-----------|-------------|------|
| Stats Grid | 1rem | styles.css:90 |
| Kanban Controls | 1rem | kanban.css:42 |
| Metrics Bar | **2rem** ❌ | kanban.css:89 |
| Board Columns | 1rem | kanban.css:139 |
| Swimlane Gap | **1.5rem** ❌ | kanban.css:267 |

**Javaslat:** Skála: `0.5rem`, `1rem`, `1.5rem`, `2rem` (4-pont típográfia).

### 6. Font Size Hierarchy Undefined

Nincs explicit H1-H6 / Body size definíció az :root-ban. Szóródott értékek:

```
H1: 1.5rem        (header)
H2: 1.25rem       (board-title) / 1rem (panel-header h2)
H3: 1rem          (undefined)
Body: 1rem        (implicit)
Small: 0.875rem   (mixed)
Xs: 0.75rem       (mixed)
```

**Javaslat:** Standardizálni:
```css
--font-h1: 1.5rem;    /* 24px */
--font-h2: 1.25rem;   /* 20px */
--font-h3: 1rem;      /* 16px */
--font-body: 1rem;    /* 16px */
--font-sm: 0.875rem;  /* 14px */
--font-xs: 0.75rem;   /* 12px */
```

---

## UX PROBLÉMÁK (P2/P3)

### 7. Loading State — Hiányzó Feedback

**Helyzet:** Nincs loading animation. Csak statikus `.loading` class:

```css
.loading {
  color: var(--text-secondary);
  text-align: center;
  padding: 2rem;
}
```

**Probléma:** Felhasználó nem tudja, hogy az oldal dolgozik-e.

**Javaslat:**
```css
.loading::after {
  content: '';
  display: inline-block;
  width: 16px;
  height: 16px;
  border-radius: 50%;
  border: 2px solid var(--border-color);
  border-top-color: var(--accent-blue);
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}
```

### 8. Mobile Responsiveness — Hiányos

**Planninig oldal:** Nincs media query — 500px alatt összeomlik!

**Projects oldal:**
```css
.projects-main {
  grid-template-columns: 280px 1fr;  /* fix 280px sidebar */
}
```
500px alatt: sidebar elrejthető toggle szükséges.

**Kanban:** Van 768px breakpoint, de 500px alatti mob-hoz nincs handling.

**Javaslat:**
```css
@media (max-width: 640px) {
  .projects-main {
    grid-template-columns: 1fr;
  }
  .hierarchy-sidebar {
    display: none; /* toggle button szükséges */
  }
  .kanban-column {
    flex: 0 0 200px;
  }
}
```

### 9. Empty State Tipográfia Eltérése

```css
.empty { font-style: italic; }
.empty-column { font-size: 0.85rem; }
.loading { (no style) }
```

**Javaslat:**统一 standardizálni — mind: `italic`, `0.875rem`, `text-secondary`.

---

## DESIGN SYSTEM JAVASLATOK

### ✅ CSS Változó Standard (aláírandó)

```css
:root {
  /* ========== PALETTA ========== */
  --bg-primary: #0f1419;
  --bg-secondary: #1a1f26;
  --bg-card: #242b33;
  --bg-hover: rgba(255, 255, 255, 0.05);    /* NEW */

  --text-primary: #e7e9ea;
  --text-secondary: #8b98a5;
  --text-muted: #8b98a5;                     /* alias */

  --accent-blue: #1d9bf0;
  --accent-green: #00ba7c;
  --accent-yellow: #ffd400;
  --accent-red: #f4212e;
  --accent-purple: #7856ff;
  --accent-color: #1d9bf0;                   /* NEW */

  --border-color: #2f3336;
  --shadow: 0 4px 12px rgba(0, 0, 0, 0.4);

  /* ========== TIPOGRÁFIA ========== */
  --font-h1: 1.5rem;
  --font-h2: 1.25rem;
  --font-h3: 1rem;
  --font-body: 1rem;
  --font-sm: 0.875rem;
  --font-xs: 0.75rem;

  --font-weight-normal: 400;
  --font-weight-medium: 500;
  --font-weight-semibold: 600;
  --font-weight-bold: 700;

  /* ========== SPACING ========== */
  --space-xs: 0.5rem;
  --space-sm: 0.75rem;
  --space-md: 1rem;
  --space-lg: 1.5rem;
  --space-xl: 2rem;

  /* ========== BORDER-RADIUS ========== */
  --radius-xs: 4px;
  --radius-sm: 6px;
  --radius-md: 8px;
  --radius-lg: 12px;

  /* ========== SHADOW ========== */
  --shadow-sm: 0 1px 2px rgba(0, 0, 0, 0.2);
  --shadow-md: 0 4px 12px rgba(0, 0, 0, 0.4);
}
```

### ✅ Komponens Konzisztencia Mátrix

| Komponens | border-radius | padding | font-size |
|-----------|---------------|---------|-----------|
| Stat Card | 12px | 1.5rem | 1rem (label) |
| Kanban Card | **12px** (统一) | 1rem | 0.9rem |
| Panel | 12px | 1.5rem | 1rem (header) |
| Modal | 12px | 1.5rem (header) | varies |
| Button Primary | 6px | 0.75rem 1.5rem | 1rem |
| Button Secondary | 6px | 0.75rem 1rem | 0.875rem |
| Tag/Badge | 4px | 0.25rem 0.5rem | 0.75rem |

---

## PRIORITIZÁLT JAVÍTÁSI LISTA

| P | Téma | Impact | Effort | Owner |
|---|------|--------|--------|-------|
| **P1** | CSS undefined variables (planning/projects) | 🔴 CRITICAL | 1h | Frontend |
| **P2** | Border-radius egységesítés (8→12px) | 🟡 HIGH | 30m | Frontend |
| **P2** | Button padding standardizálás | 🟡 HIGH | 30m | Frontend |
| **P2** | Typography CSS variables | 🟡 HIGH | 1h | Frontend |
| **P3** | Loading animation | 🟠 MEDIUM | 30m | Frontend |
| **P3** | Mobile breakpoint planning + projects | 🟠 MEDIUM | 1.5h | Frontend |
| **P3** | Empty state egységesítés | 🟠 MEDIUM | 20m | Frontend |

**Teljes effort:** ~5.5 óra | **Critical path:** P1 (1h)

---

## IMPLEMENTÁCIÓS NOTES (Frontend)

### Step 1: :root CSS variables (ASAP)

```css
/* styles.css :root UPDATE */
--bg-hover: rgba(255, 255, 255, 0.05);
--accent-color: #1d9bf0;
```

### Step 2: Fájl migráció

```bash
# planning.css refactor:
sed -i 's/--surface/--bg-card/g' planning.css
sed -i 's/--border/--border-color/g' planning.css
sed -i 's/var(--bg)/var(--bg-secondary)/g' planning.css

# projects.css: undefined var-ok javítása
# (kézi: --bg-hover, --accent-color → :root)
```

### Step 3: Border-radius audit

```bash
grep -n "border-radius" *.css | sort | uniq -c
# Lecserélni 8px → 12px kanban-card, pipeline-stage stb.
```

---

## SESSION SUMMARY

**Audit idő:** 45 perc ✅

**Elemzett fájlok:**
- ✅ styles.css (532 sor) — alapok jók, P1 fix szükséges
- ✅ kanban.css (445 sor) — konzisztens, stabil
- ✅ planning.css (100+ sor) — **BROKEN** undefined vars
- ✅ projects.css (100+ sor) — **BROKEN** undefined vars

**Output:**
- 3 CRITICAL probléma azonosított
- 9 UX inkonszisztencia felsorolva
- Standardizált CSS variable javaslat (ready-to-implement)
- 7-item prioritizált fix lista

**Következő lépések:**
1. Frontend: Planning/Projects CSS fix (1 óra)
2. Frontend: Typography + Spacing standardizálás (2 óra)
3. UX testing: Mobile responsiveness (1.5 óra)

---

## DESIGN RENDSZERT KEZDEMÉNYEZÉS

A Datahaven Dashboard készen áll egy **formális Design System dokumentálásra**:
- CSS Token catalog
- Komponens library (Storybook vagy similar)
- Brand guideline update

**Javaslat:** Frontend + Designer közösen felülvizsgálni a harmonizált design manual-t Q4-ben.
