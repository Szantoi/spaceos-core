---
id: MSG-KERNEL-108
from: root
to: kernel
type: task
priority: medium
status: READ
model: sonnet
created: 2026-06-16
---

# Kernel — DESIGN_MEMORY zárolt ADR-ek

## Tájékoztató

A VPS-en megjelent a DESIGN_MEMORY dokumentum a claude.ai projektből:

```
docs/knowledge/architecture/DESIGN_MEMORY.md
```

Ez az irányadó architektúrális döntés-katalógus. A Kernelt közvetlenül érintő zárolt döntések:

## ADR-024 — IParametricProduct

A Kernel **nem tudja és nem kell tudnia**, hogy mi egy termék konkrétan.
- A Kernel `IParametricProduct` interfészen dolgozik
- Joinery, MEP, Cutting Driver-ek implementálják az interfészt
- Kernel kód soha nem importál Joinery-specifikus típusokat

Ha jön olyan task ahol Kernel kódba kerülne termékspecifikus logika → BLOCKED outbox, kérd a Root döntését.

## ADR-039 — Cross-modul kommunikáció

- Kernel nem hív közvetlenül Joinery/Cutting/Inventory DB-t
- Modulok csak publikus API-n keresztül kommunikálnak
- Ha egy feature cross-modul adatot igényel → Orchestrator BFF koordinál

## ADR-018/019/020 — Immutability & Audit

- Audit eventek: nincs UPDATE, csak INSERT (append-only)
- Minden tenant adat RLS-sel izolált
- Ezek nem változtathatók meg jövőbeli feladatokban sem

## Nincs teendő

Ez tájékoztató — nem kell DONE outbox. A következő feladatoknál tartsd szem előtt.
