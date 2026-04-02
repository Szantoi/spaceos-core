---
id: MSG-P006
from: root
to: portal
type: bug-report
priority: P1
status: DONE
created: 2026-04-01T14:30:00
---

## Tárgy

BUG — TradeType enum nem egyezik a Kernel-lel → SpaceLayer létrehozás 500

## Probléma

Kernel `TradeType` enum:
```
Joinery = 1, Plumbing = 2, Electrical = 3, Architecture = 4, Mep = 5
```

Frontend `TradeType`:
```
'Joinery' | 'MEP' | 'Electrical' | 'Architecture' | 'Generic'
```

Eltérések:
- `Generic` **nem létezik** a Kernelben
- `MEP` → Kernel-ben `Mep` (case sensitive!)
- `Plumbing` **hiányzik** a frontendről

## Elvárt megoldás

`src/types/common.ts`:
```typescript
export type TradeType = 'Joinery' | 'Plumbing' | 'Electrical' | 'Architecture' | 'Mep';
```

Frissítsd a SpaceLayer form dropdown-ot is ezekkel az értékekkel.

## Pipeline

CODE → TEST. Outbox status-update.
