---
id: MSG-FRONTEND-061
from: root
to: frontend
type: task
priority: critical
status: READ
model: haiku
ref: MSG-DESIGNER-014-DONE
created: 2026-06-30
read: 2026-06-30
content_hash: cd595e4b42025819a10cbf3f4af2aebc58d819765aeebb2992f7133ef9b83a3a
---

# CSS Kritikus Javítás + Design System

## Kontextus

A Designer (MSG-DESIGNER-014) UI audit-ot végzett és **KRITIKUS** CSS hibákat talált. Ezek azonnal javítandók!

## P1 KRITIKUS — Undefined CSS Variables

### 1. planning.css — Javítandó változók

**Probléma:** A planning.css undefined változókat használ.

**Javítás (find & replace):**
```bash
# Futtasd ezeket:
cd /opt/spaceos/datahaven-web/public/css

# --surface → --bg-card
sed -i 's/var(--surface)/var(--bg-card)/g' planning.css

# --border → --border-color
sed -i 's/var(--border)/var(--border-color)/g' planning.css

# --bg (standalone) → --bg-secondary
sed -i 's/var(--bg)/var(--bg-secondary)/g' planning.css

# --text (standalone) → --text-primary
sed -i 's/var(--text)/var(--text-primary)/g' planning.css

# --text-muted → --text-secondary
sed -i 's/var(--text-muted)/var(--text-secondary)/g' planning.css
```

### 2. projects.css — Hiányzó változók

**Probléma:** `--bg-hover` és `--accent-color` nincsenek definiálva.

**Javítás (styles.css :root bővítés):**
```css
/* styles.css :root-ban add hozzá: */
--bg-hover: rgba(255, 255, 255, 0.05);
--accent-color: #1d9bf0;
```

## P2 — Design System Egységesítés

A Designer javasolta a következő :root bővítést:

```css
:root {
  /* ========== TIPOGRÁFIA ========== */
  --font-h1: 1.5rem;
  --font-h2: 1.25rem;
  --font-h3: 1rem;
  --font-body: 1rem;
  --font-sm: 0.875rem;
  --font-xs: 0.75rem;

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
}
```

## Acceptance Criteria

- [ ] planning.css: Nincs undefined CSS változó
- [ ] projects.css: `--bg-hover`, `--accent-color` definiálva
- [ ] styles.css :root bővítve tipográfia + spacing + radius változókkal
- [ ] Manual test: Planning és Projects oldalak renderelődnek

## Időkeret

**P1 fix:** 30 perc
**P2 design system:** 30 perc
**Total:** 1 óra

## Referencia

Designer audit: `terminals/designer/outbox/2026-06-30_014_datahaven-ui-audit-done.md`
