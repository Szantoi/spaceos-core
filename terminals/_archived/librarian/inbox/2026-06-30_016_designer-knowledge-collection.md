---
id: MSG-LIBRARIAN-016
from: root
to: librarian
type: task
priority: high
status: READ
model: haiku
created: 2026-06-30
content_hash: 7b19cce982d8b17241f6b093e3a6caa7923384a096a6aca931977f6ca2e6e28a
---

# Designer Tudásbázis Gyűjtés

## Cél

Gyűjts design és UX dokumentációt a Designer (Vízió) terminál számára, amelyből tanulhat és javíthatja a tudását.

## Feladat

### 1. Meglévő Design Dokumentáció Keresése

Keresd meg a codebase-ben:
```bash
# Design/UX dokumentumok
find /opt/spaceos/docs -name "*design*" -o -name "*ux*" -o -name "*ui*" 2>/dev/null
find /opt/spaceos/docs -name "*style*" -o -name "*component*" 2>/dev/null
```

### 2. Hasznos Külső Források Listázása

Készíts egy reading list-et:

**Design System Referenciák:**
- Tailwind CSS dokumentáció
- Shadcn/ui patterns
- Radix UI accessibility

**UX Best Practices:**
- Nielsen Norman Group (nngroup.com)
- Laws of UX (lawsofux.com)
- Refactoring UI (refactoringui.com)

**Industrial/Dashboard UI:**
- Dashboard design patterns
- Data visualization best practices
- Dark theme design

### 3. SpaceOS Specifikus Kontextus

Gyűjtsd össze:
- Datahaven CSS struktúra elemzés
- Meglévő komponens minták
- Színpaletta és tipográfia használat

## Output

`docs/knowledge/by-role/DESIGNER_READING_LIST.md`

```markdown
# Designer (Vízió) Tudásbázis

## SpaceOS Belső Dokumentáció

| Fájl | Tartalom |
|------|----------|
| docs/design/... | ... |

## Külső Források — Reading List

### Design Systems
- [Tailwind CSS](https://tailwindcss.com/docs)
- ...

### UX Patterns
- [Laws of UX](https://lawsofux.com)
- ...

### Dashboard Design
- ...

## Datahaven Specifikus

### CSS Struktúra
```
public/css/
├── planning.css
├── kanban.css
├── projects.css
└── ...
```

### Használt Színek
- Primary: #3B82F6
- ...
```

## Constraint

- 30 perc gyűjtés
- Fókusz: gyakorlati, használható források
- DONE outbox amikor kész
