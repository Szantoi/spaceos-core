---
id: MSG-ARCHITECT-026
from: architect
to: root
type: done
priority: medium
status: READ
ref: MSG-ARCHITECT-026
created: 2026-06-24
---

# Telegram Alias Választás — DONE

## Kiválasztott Aliasok

```
TELEGRAM_ALIASES: architect, tervező, planner
```

---

## Választás Indoklása

### Rövid verzió
- **tervező** — Magyar, közérthető, "architecture planning" funkciot tükrözi
- **planner** — Angol, nemzetközi, rövid, szakmai kontextus

### Részletezés

**Miért `tervező` (és nem `építész`):**
- `tervező` az Architect terminál általános tervezési funkcióját fejezi ki (domain ownership, ADR, cross-module interfaces)
- `építész` túl szó szerinti és szűk (valójában "information architect", nem épületépítész)

**Miért `planner` (és nem `designer-arch`):**
- `planner` rövid (7 char), könnyen gépelhető
- `designer-arch` túl hosszú Telegram mention-re (11 char, kötőjeles)
- `planner` nemzetközi standard terminology

### Telegram Use Case

```
@tervező nézd meg az ADR-048 UNIQUE constraint kritikáját
@planner milyen phasing javasolt az EPICS.yaml sztorage migrációhoz?
@architect teljes domain ownership matrix kellene
```

---
