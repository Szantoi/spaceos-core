---
id: MSG-DESIGNER-014-DONE
from: designer
to: conductor
type: done
status: READ
ref: MSG-DESIGNER-014
created: 2026-06-30T00:00:00.000Z
reviewed_by: conductor
reviewed_at: 2026-06-30T19:04:00Z
---

# Datahaven Design System — Egységes Komponens Katalógus

## Összefoglaló

A Datahaven Dashboard meglévő UI elemeit rendszereztem és dokumentáltam. Az alapok erősek (CSS variables, sötét téma, konzisztens spacing) — egy moduláris design system spec-et készítettem, amely azonnal implementálható.

---

## 📦 DESIGN SYSTEM FOUNDATION

### Szín Paletta (Tested & Working)

Jelenleg működő rendszer (**styles.css** :root):

```css
:root {
  /* ========== HÁTTÉR ========== */
  --bg-primary: #0f1419;        /* Oldal háttér */
  --bg-secondary: #1a1f26;      /* Sidebar, hover states */
  --bg-card: #242b33;           /* Card, panel, modal háttér */
  --bg-hover: rgba(255, 255, 255, 0.05);  /* Subtle hover (NEW) */

  /* ========== SZÖVEG ========== */
  --text-primary: #e7e9ea;      /* Főszöveg */
  --text-secondary: #8b98a5;    /* Másodlagos szöveg, placeholder */
  --text-muted: #8b98a5;        /* Alias for consistency */

  /* ========== AKCENTUSOK ========== */
  --accent-blue: #1d9bf0;       /* Primary action, links */
  --accent-green: #00ba7c;      /* Success, online status */
  --accent-yellow: #ffd400;     /* Warning, pending */
  --accent-red: #f4212e;        /* Error, critical, offline */
  --accent-purple: #7856ff;     /* Info, highlights */

  /* ========== SEGÉD ========== */
  --border-color: #2f3336;      /* Borders, dividers */
  --shadow: 0 4px 12px rgba(0, 0, 0, 0.4);  /* Drop shadow */
}
```

**Theme:** Dark mode (Twitter-inspired palette)
**Contrast:** WCAG 2.1 AA compliant (#e7e9ea on #0f1419)

---

## 🎨 KOMPONENS KATALÓGUS

### 1. GOMBÓK

#### Jelenlegi Implementáció

| Button Type | CSS Class | Padding | Border-Radius | Font-Size | Use Case |
|---|---|---|---|---|---|
| Primary CTA | `.btn-primary` | **0.75rem 1.5rem** | 6px | 1rem | Submit, accept |
| Secondary | `.btn-refresh` | **0.5rem 1rem** | 6px | 0.875rem | Refresh, filter |
| Ghost | (inline link) | — | — | varies | Cancel, close |
| Icon Button | (implicit) | **0.75rem** | 6px | 1.5rem | X close, menu |

**Meghatározott gombok:**
- `.btn-refresh` (styles.css:160-173) ✅
- `.search-box button` (styles.css:339-352) — primary style
- `.auth-box button` (styles.css:502-512) — full-width variant

**Standardizálási szükség:** Button padding egységesítés (0.5rem vs 0.75rem).

#### Design System Javaslat

```css
/* Standardizált gombok */
.btn-primary {
  background: var(--accent-blue);
  color: white;
  border: none;
  padding: 0.75rem 1.5rem;
  border-radius: 6px;
  font-weight: 500;
  font-size: 1rem;
  cursor: pointer;
  transition: opacity 0.2s;
}

.btn-primary:hover {
  opacity: 0.9;
}

.btn-primary:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.btn-secondary {
  background: var(--bg-secondary);
  color: var(--text-primary);
  border: 1px solid var(--border-color);
  padding: 0.75rem 1rem;
  border-radius: 6px;
  font-weight: 500;
  font-size: 0.875rem;
  cursor: pointer;
  transition: background 0.2s;
}

.btn-secondary:hover {
  background: var(--bg-card);
}

.btn-ghost {
  background: transparent;
  border: none;
  color: var(--text-secondary);
  cursor: pointer;
  text-decoration: underline;
}

.btn-ghost:hover {
  color: var(--text-primary);
}

/* Méretek */
.btn-sm {
  padding: 0.5rem 1rem;
  font-size: 0.75rem;
}

.btn-lg {
  padding: 1rem 2rem;
  font-size: 1.125rem;
}

/* Icon button */
.btn-icon {
  width: 40px;
  height: 40px;
  padding: 0;
  border-radius: 6px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--bg-secondary);
  border: 1px solid var(--border-color);
  color: var(--text-secondary);
  font-size: 1.25rem;
  cursor: pointer;
}

.btn-icon:hover {
  background: var(--bg-card);
  color: var(--text-primary);
}
```

---

### 2. FORM ELEMEK

#### Jelenlegi Implementáció

| Elem | CSS Class | Padding | Border-Radius | Font-Size |
|---|---|---|---|---|
| Input | `.search-box input` | 0.75rem 1rem | 6px | 1rem |
| Select | `.terminal-filter` | 0.5rem 1rem | 6px | 1rem |
| Textarea | (implicit) | 1rem | 6px | 0.875rem |

**Működő input stílus** (styles.css:325-337):
```css
.search-box input {
  flex: 1;
  background: var(--bg-secondary);
  border: 1px solid var(--border-color);
  border-radius: 6px;
  padding: 0.75rem 1rem;
  color: var(--text-primary);
  font-size: 1rem;
}

.search-box input::placeholder {
  color: var(--text-secondary);
}
```

#### Design System Javaslat

```css
/* Input, Select, Textarea base */
.input,
.select,
.textarea {
  background: var(--bg-secondary);
  color: var(--text-primary);
  border: 1px solid var(--border-color);
  border-radius: 6px;
  padding: 0.75rem 1rem;
  font-family: inherit;
  font-size: 1rem;
  transition: border-color 0.2s, box-shadow 0.2s;
}

.input::placeholder,
.select::placeholder,
.textarea::placeholder {
  color: var(--text-secondary);
}

.input:focus,
.select:focus,
.textarea:focus {
  outline: none;
  border-color: var(--accent-blue);
  box-shadow: 0 0 0 3px rgba(29, 155, 240, 0.1);
}

.input:disabled,
.select:disabled,
.textarea:disabled {
  background: var(--bg-card);
  opacity: 0.5;
  cursor: not-allowed;
}

/* Error state */
.input.error {
  border-color: var(--accent-red);
}

.input.error:focus {
  box-shadow: 0 0 0 3px rgba(244, 33, 46, 0.1);
}

/* Textarea override */
.textarea {
  resize: vertical;
  min-height: 100px;
}

/* Checkbox, Radio */
.checkbox,
.radio {
  appearance: none;
  width: 20px;
  height: 20px;
  border: 2px solid var(--border-color);
  border-radius: 4px;
  background: var(--bg-card);
  cursor: pointer;
  transition: all 0.2s;
}

.checkbox:checked,
.radio:checked {
  background: var(--accent-blue);
  border-color: var(--accent-blue);
}

.radio {
  border-radius: 50%;
}
```

---

### 3. KÁRTYA ÉS PANEL KOMPONENSEK

#### Jelenlegi Implementáció

| Komponens | CSS Class | Padding | Border-Radius | Border |
|---|---|---|---|---|
| Stat Card | `.stat-card` | 1.5rem | 12px | 1px solid --border-color |
| Panel | `.panel` | — | 12px | 1px solid --border-color |
| Kanban Card | `.kanban-card` | 1rem | **8px** ❌ | 1px solid --border-color |
| Modal | `.modal-content` | — | 12px | none |

**Megtalált inkonzisztencia:** Kanban card 8px, összes többi 12px.

#### Design System Javaslat

```css
/* Unified Card/Panel */
.card,
.panel {
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: 12px;
  overflow: hidden;
}

.card {
  padding: 1.5rem;
}

.card-header {
  padding: 1.5rem;
  border-bottom: 1px solid var(--border-color);
}

.card-content {
  padding: 1.5rem;
}

.card-footer {
  padding: 1.5rem;
  border-top: 1px solid var(--border-color);
}

/* Stat Card variant */
.stat-card {
  text-align: center;
}

.stat-value {
  font-size: 2.5rem;
  font-weight: 700;
  margin-bottom: 0.5rem;
}

.stat-value.primary { color: var(--accent-blue); }
.stat-value.success { color: var(--accent-green); }
.stat-value.warning { color: var(--accent-yellow); }
.stat-value.error { color: var(--accent-red); }

.stat-label {
  color: var(--text-secondary);
  font-size: 0.875rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

/* Badge */
.badge {
  display: inline-block;
  padding: 0.25rem 0.75rem;
  border-radius: 12px;
  font-size: 0.75rem;
  font-weight: 600;
  text-transform: uppercase;
  background: var(--bg-secondary);
  color: var(--text-secondary);
}

.badge.primary { background: rgba(29, 155, 240, 0.2); color: var(--accent-blue); }
.badge.success { background: rgba(0, 186, 124, 0.2); color: var(--accent-green); }
.badge.warning { background: rgba(255, 212, 0, 0.2); color: var(--accent-yellow); }
.badge.error { background: rgba(244, 33, 46, 0.2); color: var(--accent-red); }
```

---

### 4. TIPOGRÁFIA

#### Megtalált Szabályok

| Elem | Font-Size | Font-Weight | Use |
|---|---|---|---|
| H1 | **1.5rem** | 700 | Header, page title |
| H2 | **1.25rem** | 600 | Panel header, section title |
| H3 | **1rem** | 600 | Subsection |
| Body | **1rem** | 400 | Normal text |
| Small | **0.875rem** | 400 | Metadata, label |
| XS | **0.75rem** | 600 | Badge, tag |

#### Design System Javaslat

```css
/* Typography tokens */
:root {
  --font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;

  /* Font sizes */
  --font-h1: 1.5rem;     /* 24px */
  --font-h2: 1.25rem;    /* 20px */
  --font-h3: 1rem;       /* 16px */
  --font-body: 1rem;     /* 16px */
  --font-sm: 0.875rem;   /* 14px */
  --font-xs: 0.75rem;    /* 12px */

  /* Font weights */
  --weight-normal: 400;
  --weight-medium: 500;
  --weight-semibold: 600;
  --weight-bold: 700;

  /* Line heights */
  --line-height-tight: 1.2;
  --line-height-normal: 1.5;
  --line-height-relaxed: 1.75;
}

h1 { font: var(--weight-bold) var(--font-h1) / var(--line-height-tight) var(--font-family); }
h2 { font: var(--weight-semibold) var(--font-h2) / var(--line-height-tight) var(--font-family); }
h3 { font: var(--weight-semibold) var(--font-h3) / var(--line-height-tight) var(--font-family); }

body { font: var(--weight-normal) var(--font-body) / var(--line-height-normal) var(--font-family); }

.text-small { font-size: var(--font-sm); }
.text-xs { font-size: var(--font-xs); font-weight: var(--weight-semibold); }
.text-muted { color: var(--text-secondary); }
```

---

### 5. SPACING ÉS LAYOUT

#### Spacing Scale (Consistency Found)

```css
:root {
  --space-xs: 0.5rem;    /* 8px */
  --space-sm: 0.75rem;   /* 12px */
  --space-md: 1rem;      /* 16px */
  --space-lg: 1.5rem;    /* 24px */
  --space-xl: 2rem;      /* 32px */
  --space-2xl: 3rem;     /* 48px */
}
```

**Jelenlegi użycie:**
- Grid gap: `1rem`, `1.5rem`, `2rem` ✅
- Padding: `0.75rem`, `1rem`, `1.5rem`, `2rem` ✅
- Margin: `1rem`, `1.5rem`, `2rem` ✅

**Problémás:** swimlane gap `1.5rem` vs kanban gap `1rem` — mind egységesíteni kell.

---

### 6. BORDER-RADIUS STANDARD

```css
:root {
  --radius-xs: 4px;      /* tags, small elements */
  --radius-sm: 6px;      /* buttons, inputs */
  --radius-md: 8px;      /* kanban cards (fix) */
  --radius-lg: 12px;     /* panels, cards, modals */
}
```

**Standardizálás szükséges:**
- Kanban card: `8px` → **`12px`** (consistency with panels)
- Pipeline stage: `8px` → **`12px`**

---

## 📋 MODULÁRIS KOMPONENS TEMPLATE

Minden komponens így néz ki:

```
KOMPONENS: [Name]
├── HTML Structure
├── CSS Class
├── Props/Variants
├── Accessibility
├── States (default, hover, focus, active, disabled)
└── Code Example
```

### Példa: Primary Button

```html
<button class="btn btn-primary">
  Save Changes
</button>

<button class="btn btn-primary btn-lg">
  Large Button
</button>

<button class="btn btn-primary" disabled>
  Disabled
</button>
```

```css
.btn {
  font-family: inherit;
  border: none;
  border-radius: var(--radius-sm);
  cursor: pointer;
  transition: all 0.2s;
  font-weight: var(--weight-medium);
}

.btn-primary {
  background: var(--accent-blue);
  color: white;
  padding: 0.75rem 1.5rem;
  font-size: 1rem;
}

.btn-primary:hover {
  opacity: 0.9;
}

.btn-primary:focus {
  outline: none;
  box-shadow: 0 0 0 3px rgba(29, 155, 240, 0.3);
}

.btn-primary:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.btn-lg {
  padding: 1rem 2rem;
  font-size: 1.125rem;
}
```

---

## ✅ IMPLEMENTÁCIÓS CHECKLIST

| Feladat | Status | Priority |
|---------|--------|----------|
| CSS variables standard (finalizálva) | ✅ Done | P0 |
| Button variants (primary, secondary, ghost, icon) | ✅ Spec ready | P1 |
| Input/Form elements standardization | ✅ Spec ready | P1 |
| Card/Panel/Badge components | ✅ Spec ready | P1 |
| Typography scale finalized | ✅ Spec ready | P2 |
| Spacing/Border-radius standardization | ✅ Spec ready | P2 |
| Kanban card border-radius fix (8px→12px) | ⏳ Pending | P2 |
| Modal component refinement | ✅ Spec ready | P3 |
| Accessibility (ARIA, keyboard nav) | ⏳ Pending | P3 |

---

## 🚀 NEXT STEPS

### Immediate (Frontend)
1. **Apply CSS variables standard** (styles.css) — 30 min
2. **Create component CSS file** (components.css) — 1.5h
3. **Update existing HTML** to use standardized classes — 2h

### Short-term
1. **Storybook setup** (component showcase) — 4h
2. **Accessibility audit** (WCAG 2.1 AA) — 2h
3. **Mobile responsive refinement** — 2h

### Long-term
1. **Design token JSON export** (for code generation)
2. **Figma ↔ CSS sync** workflow
3. **Dark/Light theme toggle**

---

## 📊 DESIGN SYSTEM QUALITY SCORE

| Aspektus | Status | Score |
|----------|--------|-------|
| **Color Consistency** | ✅ Excellent | 9/10 |
| **Component Modularity** | ⚠️ Good | 7/10 |
| **Documentation** | ⏳ In progress | 5/10 |
| **Accessibility** | ⚠️ Partial | 6/10 |
| **Mobile Responsiveness** | ⚠️ Partial | 6/10 |
| **Performance** | ✅ Good | 8/10 |

**Overall:** 6.8/10 → **Solid foundation, ready to systemize**

---

## CONCLUSION

A Datahaven Dashboard meglévő UI elemei **erős alapot** nyújtanak. Az audit eredményei + design system spec segítségével a Frontend csapat egy **production-ready moduláris komponens könyvtárat** építhet.

**Ajánlás:** Frontend kezdje a button/input standardizálással, majd progresszíven frissítse a komponenseket.

**Session:** 45 perc ✅
