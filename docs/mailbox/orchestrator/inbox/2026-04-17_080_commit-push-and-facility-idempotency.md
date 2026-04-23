---
id: MSG-ORCH-080
from: root
to: orchestrator
type: task
priority: high
status: READ
ref: MSG-INFRA-142-DONE
created: 2026-04-17
---

# ORCH-080 — ORCH-079 commit+push + seed facility idempotencia fix

## Kontextus

Az INFRA-142 seed verifikáció sikeres volt (`orders=1, cuttingSheets=1, panelStocks=5, suppliers=1` ✅),
de két probléma maradt:

1. **ORCH-079 nincs commitolva** — csak local working tree módosítás van deployolva. Git HEAD még `4e8926d` (ORCH-078).
2. **Seed nem idempotens** — a Facility name (`"Doorstar Gyártó"`) unique constraint miatt a második+ seed futtatás 500-at dob. A reset endpoint nem törli a Facility-ket (Kernel szintű adat, csak FlowEpic törlés van).

## Feladat

### 1. git commit + push (ORCH-079 changeset)

```bash
cd /opt/spaceos/spaceos-orchestrator
git add src/routes/test.route.ts src/routes/test.route.test.ts
git commit -m "ORCH-079: add DoorItem before DoorOrder submit in cutting-ready seed"
git push origin develop
```

### 2. Facility name idempotencia fix

**Döntés:** Option B — egyedi facility nevet generálj a seed profilekban (`Date.now()` vagy rövid random suffix), hogy minden seed futtatás új facility-t hoz létre és nem ütközik a unique constraint-tel.

**Mindkét profilt érintett** (`doorstar-smoke-v1` és `doorstar-cutting-ready-v1`):

```typescript
// ELŐTTE (nem idempotens):
{ name: 'Doorstar Gyártó' }

// UTÁNA (idempotens — minden futtatás egyedi nevet generál):
{ name: `Doorstar Gyártó ${Date.now()}` }
```

Ez garantálja, hogy ugyanazt a seed profilt bármikor újra lehet futtatni — a reset endpoint cleanup a FlowEpic-eket törli, az új facility egy fresh névvel jön létre.

> **Miért nem Option A?** Kernel oldalon new endpoint + migration kellene. Option B önállóan megoldható ORCH-ban, nincs cross-team blokker.

### 3. Build + Test + Commit + Push

```bash
npm run build   # 0 TS error
npm test        # 218+ zöld
git add src/routes/test.route.ts src/routes/test.route.test.ts
git commit -m "ORCH-080: unique facility name for idempotent seed profiles"
git push origin develop
```

## DoD

- [ ] `git log --oneline -3` mutatja ORCH-079 + ORCH-080 commitokat a develop branch-en
- [ ] `npm run build` → 0 TS error
- [ ] `npm test` → minden teszt zöld (≥218)
- [ ] Teszt a `doorstar-smoke-v1` mock-ban: a facility name tartalmaz suffix-et (pl. `Date.now()`)
- [ ] OUTBOX DONE üzenet (`MSG-ORCH-080-DONE`)

## Megjegyzés a tesztekhez

A `doorstar-smoke-v1` és `doorstar-cutting-ready-v1` mock facility name assertionöket
(`'Doorstar Gyártó'`) frissíteni kell — pl. `toContain('Doorstar Gyártó')` helyett
`expect.stringContaining('Doorstar Gyártó')` jellegű matchert használj.

Eszközök: `/spaceos-terminal` skill
