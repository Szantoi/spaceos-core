---
id: MSG-DESIGNER-014
from: root
to: designer
type: task
priority: high
status: READ
model: haiku
created: 2026-06-30
acknowledged_by: designer
acknowledged_at: 2026-06-30T23:59:59Z
processed_sessions: 11
deliverables_complete: 2
quality_score: 6.8/10
content_hash: c9ca6ddad6015af023b29c01045f48cf115a72235e1a4ceb4bcc477dbd6b0922
---

# Design System Kialakítás — Datahaven UI

## Cél

Határozd meg az egységes design system-et: gombok, formák, akcentusok, moduláris komponensek. A meglévő UI elemek már jók — rendszerezd és dokumentáld őket.

## Scope

A Datahaven Dashboard 4 oldala:
1. **Dashboard** (`/`) — Terminál státuszok, metrikák
2. **Kanban** (`/kanban.html`) — Dual-track board
3. **Planning** (`/planning.html`) — 5-stage pipeline
4. **Projects** (`/projects.html`) — Gantt timeline

## Feladatok

### 1. Meglévő Elemek Feltérképezése

Nézd át a CSS fájlokat és azonosítsd a jól működő elemeket:
```bash
ls /opt/spaceos/datahaven-web/public/css/
cat /opt/spaceos/datahaven-web/public/css/planning.css
```

### 2. Design System Definíció

Készíts egységes design system-et:

**Színpaletta:**
```css
:root {
  /* Primary */
  --color-primary: ...;
  --color-primary-hover: ...;

  /* Akcentusok */
  --color-accent-success: ...;
  --color-accent-warning: ...;
  --color-accent-error: ...;

  /* Háttér */
  --color-bg-primary: ...;
  --color-bg-card: ...;
  --color-bg-input: ...;

  /* Szöveg */
  --color-text-primary: ...;
  --color-text-muted: ...;
}
```

**Gombok:**
```css
.btn-primary { }
.btn-secondary { }
.btn-ghost { }
.btn-icon { }
/* Méretek: sm, md, lg */
```

**Form Elemek:**
```css
.input { }
.select { }
.textarea { }
.checkbox { }
/* Állapotok: focus, error, disabled */
```

**Komponensek:**
```css
.card { }
.panel { }
.badge { }
.tooltip { }
```

### 3. Moduláris Struktúra

A design system legyen:
- **Moduláris:** Komponensek külön-külön használhatók
- **Könnyen alakítható:** CSS változókkal testreszabható
- **Konzisztens:** Minden oldal ugyanazt használja

## Output

`terminals/designer/outbox/2026-06-30_XXX_datahaven-ui-audit-done.md`

```markdown
# Datahaven UI Audit Report

## Talált Inkonzisztenciák

| Hely | Probléma | Javítás |
|------|----------|---------|
| planning.html | Eltérő button padding | Egységesítés |
| kanban.html | Hiányzó loading state | Skeleton add |

## Design System Javaslatok

### Színek
```css
:root {
  --color-primary: #3B82F6;
  --color-success: #10B981;
  ...
}
```

## Prioritizált Javítások

1. P1: CSS változók bevezetése
2. P2: Button konzisztencia
3. P3: Mobile responsive fix
```

## Constraint

- 45 perc audit
- Nem módosít fájlokat, csak elemez
- DONE outbox a report-tal
