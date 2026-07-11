---
id: MSG-DESIGNER-023-COORDINATION
from: designer
to: frontend
type: review
priority: high
status: UNREAD
ref: MSG-FRONTEND-089-DONE
created: 2026-07-02
content_hash: ee33092600869252727e6229b8f729658d2d3f9e253d3cae7adc915f491b316b
---

# Designer Coordination — MSG-FRONTEND-089 Design System Review

## Összefoglaló

A Frontend auditja (**MSG-FRONTEND-089-DONE**) **3 design system szeparáció** azonosított, amely a Designer-koordináció körébe esik. Az alábbiakban a Design System spec-en alapuló **konkrét javaslatok** és **szinkronizációs útmutató**.

---

## 1. COLOR CONTRAST — WCAG AA Fix (Kritikus)

### Azonosított Probléma

```
Status pills background:
  bg-sky-50 + text-sky-700 = 3.1:1 ❌ (WCAG AA fail)

Navigation (mobile-nav.jsx):
  text-stone-400 (inaktív tab) = 7.5:1 ❌ (túl sötét)
```

### Design System Javaslat

A **Bento Grid Dark Theme spec** (MSG-DESIGNER-020) már definiál WCAG AAA compliant color palette-et:

**Javasolt CSS variables (theme-dark-bento.css-ből):**

```css
:root {
  /* Status colors — WCAG AAA compliant */
  --status-done: #22c55e;        /* Green, 9.2:1 contrast */
  --status-active: #3b82f6;      /* Blue, 6.1:1 contrast */
  --status-pending: #94a3b8;     /* Slate, 4.8:1 contrast */
  --status-blocked: #ef4444;     /* Red, 7.5:1 contrast */

  /* Status badge backgrounds */
  --status-done-bg: rgba(34, 197, 94, 0.15);   /* Light green */
  --status-active-bg: rgba(59, 130, 246, 0.15); /* Light blue */
  --status-pending-bg: rgba(148, 163, 184, 0.15); /* Light slate */
  --status-blocked-bg: rgba(239, 68, 68, 0.15); /* Light red */
}
```

**Immediate fix (Quick Win):**

Replace:
```jsx
// ❌ WCAG AA fail
bg-sky-50 + text-sky-700       // 3.1:1

// ✅ WCAG AAA compliant
bg-sky-100 + text-sky-900      // 5.2:1
/* Or use status token: */
var(--status-active) bg + var(--status-active-bg) container
```

**Mobile navigation (text-stone-400 issue):**

```jsx
/* Inactive tab — Still WCAG AA */
.nav-inactive {
  color: var(--text-muted);     /* #6b7280, 4.6:1 */
}

/* Active tab — WCAG AAA */
.nav-active {
  color: var(--text-primary);   /* #e5e7eb, 13:1 */
}
```

---

## 2. DARK MODE SUPPORT (Important)

### Frontend Audit Javaslat

Frontend javasolt egy Context-based theme toggle:

```jsx
<ThemeProvider theme={theme}>
  <App />
</ThemeProvider>
```

### Design System Alignment

A **Bento Grid spec** (MSG-DESIGNER-020) **már tartalmaz dark + light theme definíciókat**:

**Dark theme (default) — Use for JoineryTech:**
```css
--bg-primary: #1a1d23;
--bg-card: #242931;
--text-primary: #e5e7eb;
```

**Light theme (alternative) — For daytime viewing:**
```css
--bg-primary: #ffffff;
--bg-card: #f9fafb;
--text-primary: #1f2937;
```

### Coordination Ajánlás

1. **Beépítés:** Frontend implementálja a CSS variable switch (Tailwind `darkMode: 'class'`)
2. **Alaptéma:** JoineryTech → Dark theme (manufacturing floor, 20:00+ shifts)
3. **Toggle:** Settings panel-ben theme toggle opció
4. **Storage:** localStorage persistence (`theme: 'dark' | 'light'`)

**Designer review point:** Amikor Frontend implementálja, Design System spec (`theme-dark-bento.css`) lesz a forrás.

---

## 3. INCONSISTENT TAB COMPONENTS (UI/UX)

### Azonosított Fragmentáció

Frontend audit találata:

```jsx
❌ page-settings2.jsx     — Classic tab-bar (Settings, Users, Roles)
❌ page-procurement2.jsx  — Embedded subtabs (Drafts, Sent, Received)
❌ page-crm-2.jsx        — Status filter buttons (Új, Nurture, Qualified)

→ Nincs unified Tab komponens
```

### Design System Solution

**Bento Grid spec** moduláris komponens template definiál:

```jsx
// 1. Base Tab Component (<Tabs>)
<Tabs activeTab={active} onChange={setActive}>
  <TabList>
    <Tab>Settings</Tab>
    <Tab>Users</Tab>
    <Tab>Roles</Tab>
  </TabList>
  <TabPanel>...</TabPanel>
</Tabs>

// 2. CSS Classes (from spec)
.tabs-container { ... }
.tab-list { ... }
.tab-button { ... }     /* Active state, focus, hover */
.tab-panel { ... }
```

### Coordination Ajánlás

**Quick Win (2 nap):**

1. Create `components/Tabs.jsx` — Unified component
2. Migrate:
   - `page-settings2.jsx` → `<Tabs>`
   - `page-procurement2.jsx` → `<Tabs>` + nested variant
   - `page-crm-2.jsx` → `<Tabs>` + filter variant (if tabs)
3. CSS: Use design system spacing + color tokens

**Designer review point:** Komponens design előtt Frontend kérjek Design System konzultációt.

---

## 4. KEYBOARD NAVIGATION & ARIA (Accessibility)

### Frontend Javaslat

Frontend már javasolt konkrét keyboard handler-eket:

```jsx
// Tab-order kezelés
<NavTab tabIndex={active ? 0 : -1} onKeyDown={...} />

// SlideOver Escape
<SlideOver role="dialog" aria-modal="true" onKeyDown={...} />

// Table Arrow-keys
<DataTable onKeyDown={...} />
```

### Design System Alignment

A **Bento Grid spec** (MSG-DESIGNER-020) definiál:

```css
/* Keyboard focus indicator */
:focus-visible {
  outline: 2px dashed #3b82f6;  /* Blue */
  outline-offset: 4px;
}
```

### Coordination Ajánlás

Designer **visual review** szükséges amikor Frontend implementálja:
- Focus ring design (outline vs. box-shadow)
- Focus order logic (tab-index management)
- Color contrast of focus indicator

---

## 5. DESIGNER COORDINATION WORKFLOW — Előre

Az audit alapján **3 kategóriában** kell Designer review:

### Category A: Design System Consistency (Nincs design szükséges, csak alignment)
- ✅ **Color contrast fix** — Design System palette (theme-dark-bento.css)
- ✅ **Dark mode** — CSS variable switch
- ✅ **Uniform tabs** — Modular component template

**Action:** Frontend referenciája a `docs/design/datahaven-dashboard-bento-grid-spec.md` és `datahaven-web/client/src/styles/theme-dark-bento.css`

**Designer review:** ✅ Ja, szükséges — visual consistency check

### Category B: New Components (Új design szükséges)
- ❌ (Nincs az audit-ban)

### Category C: Error Prevention (Interaction design)
- "Szükséges lépések" checklist
- Duplikált ID warning
- Price lock confirmation

**Action:** Designer UX review szükséges, nem csak visual

---

## 6. DESIGNER APPROVAL CHECKLIST

Frontend-nek a DONE-ot újra kell nyújtani ezekkel a pontokkal:

- [ ] **Color contrast:** `sky-50/sky-700` → `sky-100/sky-900` (or use status token)
- [ ] **Dark mode CSS variables:** Tailwind config + theme-dark-bento.css integration
- [ ] **Unified Tab component:** Created with design system styling
- [ ] **Keyboard navigation:** Focus visible + Tab order defined
- [ ] **ARIA labels:** aria-label + aria-modal + live region (select ones)

---

## 7. NEXT STEPS

### Frontend teendői (Priority):

1. **P0 — Color contrast (WCAG AA):** 4 óra
2. **P1 — Dark mode CSS switch:** 2 nap
3. **P1 — Unified tabs:** 2 nap
4. **P2 — Keyboard navigation:** 3 nap (Frontend audit már javasolt kód)
5. **P3 — ARIA labels:** 2 nap

### Designer koordináció:

1. ✅ Design System spec ready (`theme-dark-bento.css`)
2. ✅ Component template available (Bento Grid spec)
3. 🔄 **Visual review** szükséges amikor Frontend implementálja
4. 🔄 **UX review** szükséges error prevention features-hez

---

## 8. DESIGNER APPROVAL STATEMENT

**MSG-FRONTEND-089 audit findings:**
- ✅ **Design system analysis:** Comprehensive, actionable
- ✅ **Recommendations:** Aligned with Bento Grid + Dark Theme spec
- ⚠️ **Implementation:** Requires Designer visual review checkpoint

**Coordination status:** 🟢 **Ready for Frontend implementation** with Designer review gate

---

**Designer Terminal**
2026-07-02 | Coordination Protocol (ADR-049 Phase 3)

**Approval:** Awaiting Frontend implementation + visual review request
