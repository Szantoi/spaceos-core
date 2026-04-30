---
id: MSG-PORTAL-012
from: root
to: portal
type: task
priority: high
status: READ
ref: SPRINT5
created: 2026-04-17
---

# PORTAL-012 — Doorstar design tokens + brand rendszer

## Kontextus

A `spaceos-doorstar-portal` jelenleg Vite starter template-alapú megjelenéssel fut.
Soft Launch GO után az első valódi Doorstar felhasználók ezt fogják látni.
Sprint 5-ben Doorstar-specifikus design tokeneket és brand rendszert kell felépíteni.

## Tudásbázis referencia

- `docs/knowledge/context/PORTAL_CONTEXT.md` — terminál kontextus
- `docs/SpaceOS_Architecture_QA_20260417.md` — branding döntések háttere

## Feladat

### 1. Tailwind design tokens (`tailwind.config.ts`)

```typescript
theme: {
  extend: {
    colors: {
      primary: {
        DEFAULT: '#3B5AD8',
        hover:   '#2D46B0',
        light:   '#EEF1FC',
      },
      secondary: { DEFAULT: '#1E293B' },
      accent:    { DEFAULT: '#F59E0B' },
      surface:   { DEFAULT: '#F8FAFC' },
    },
    fontFamily: {
      sans: ['Inter', 'system-ui', 'sans-serif'],
    },
  },
}
```

### 2. Inter font betöltés

`index.html`-be Google Fonts link, vagy lokális font fájl `public/fonts/`-ba.

### 3. Doorstar logó placeholder

`public/logo.svg` — ha nincs végleges logó: „D" monogram SVG az elsődleges színnel.
Navbar-ba és favicon-ba bekötve.

### 4. Komponensek frissítése

A meglévő komponensek (AppLayout, nav, gombok, badge-ek) használják az új tokeneket.
Inline hardkódolt színek (`bg-blue-600`, `text-gray-800`, stb.) cseréje `bg-primary`, `text-secondary` stb.-re.

### 5. App.css tisztítás

A Vite starter template maradványok (`var(--accent)`, `.hero`, `.counter` stb.) eltávolítása.

## Build gate

```bash
pnpm test
# 0 fail, min 306 pass (nincs regresszió)

pnpm build
# 0 error, 0 warning
```

## DONE feltételek

- [ ] Tailwind tokens: primary / secondary / accent / surface
- [ ] Inter font betöltve
- [ ] Logó placeholder: `public/logo.svg` + navbar + favicon
- [ ] Komponensek frissítve (nincs hardkódolt szín)
- [ ] App.css tisztítva
- [ ] Tesztszám ≥ 306
- [ ] Commit hash
- [ ] OUTBOX DONE

## Skill

Használd a `/spaceos-terminal` skillt. Sub-agent **engedélyezett**.
