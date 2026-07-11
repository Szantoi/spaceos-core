---
id: MSG-ARCHITECT-033
from: root
to: architect
type: task
priority: high
status: READ
model: sonnet
created: 2026-06-30
content_hash: a002bb26fd97c1f9e45bfda4e3ae5e46256a6efcfa8dfe0d4a0860c7916c45a0
---

# CQRS Handler Generator Template Design

Tervezd meg a CQRS Handler Generator template struktúráját a SpaceOS codegen toolchain-hez (ADR-050 Phase 5).

## Kontextus

A `docs/knowledge/patterns/CODEGEN_TOOLCHAIN_PATTERN.md` dokumentálja az eddigi codegen rendszert. Most a .NET CQRS handler generátort kell megtervezni.

## Követelmények

1. **Query Handler template** - MediatR IRequest/IRequestHandler pattern
2. **Command Handler template** - ugyanaz, de command típusú
3. **Batch-kompatibilis** - Haiku klónok párhuzamosan tudják használni

## Elvárt output

Készíts egy `docs/architecture/decisions/ADR-051-cqrs-handler-generator.md` dokumentumot:

1. Template struktúra (Handlebars vagy string template)
2. Input paraméterek (name, type, response, repository, stb.)
3. Generált fájlok listája
4. Példa input → output

## Referencia

Meglévő CQRS handlerek:
- `backend/spaceos-modules/spaceos-modules-procurement/` 
- `backend/spaceos-kernel/src/SpaceOS.Kernel.Application/`

Meglévő codegen:
- `scripts/codegen/generate-hook.sh` - React hook generator minta
- `spaceos-nexus/knowledge-service/src/codegen/codegenEngine.ts`

## Acceptance Criteria

- [ ] ADR-051 dokumentum elkészült
- [ ] Query és Command template definiálva
- [ ] Példa input/output dokumentálva
- [ ] Batch dispatch kompatibilis design
