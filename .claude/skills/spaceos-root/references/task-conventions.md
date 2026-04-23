# Task fájl konvenciók

## Fájlnév

```
<EPIC-ID>_<slug>.md
```

Példák:
```
KERNEL-067_flowepic-close-fsm-fix.md
ORCH-060_proof-route-path-fix.md
E2E-014_36-proof-chain.md
INFRA-071_orch-deploy-b7b4581.md
BATCH-0-CLEANUP_legacy-fails.md
```

Epic ID = terminál prefix + sorszám, pl. `KERNEL-067`, `E2E-014`, `INFRA-071`

## Task fájl frontmatter

```yaml
---
id: EPIC-ID
title: Feladat emberi neve
status: new | active | archive
priority: critical | high | medium | low
assignee: kernel | orchestrator | e2e | infra | portal | joinery | abstractions | ~
epic: epic-slug
blocked_by: <mi blokkolja, vagy ~>
created: YYYY-MM-DD
updated: YYYY-MM-DD
docs:
  - docs/mailbox/<terminál>/inbox/YYYY-MM-DD_NNN_slug.md
---

Egy-két mondatos összefoglaló.
```

## Lifecycle parancsok

### new → active (feladat kiadásakor)

```bash
mv docs/tasks/new/<fájl>.md docs/tasks/active/<fájl>.md
```

Ezzel párhuzamosan: inbox üzenet megírása és README.md frissítése.

### active → archive (DONE elfogadásakor)

```bash
mv docs/tasks/active/<fájl>.md docs/tasks/archive/<fájl>.md
```

Ezzel párhuzamosan: README.md és Codebase_Status.md frissítése.

## README.md frissítési minta

```markdown
### 🔴 Active (vár)
- `KERNEL-067_flowepic-close-fsm-fix.md` — 🟠 FlowEpic /close 500 fix · MSG-KERNEL-067

### 🟡 New
- `E2E-015_37-tools-chain.md` — 37-tools.chain.test.ts · blokkoló: E2E-014 DONE

### ✅ Archive
- `ORCH-060_proof-route-path-fix.md` — proof route path fix · commit b7b4581 · 183 teszt ✅ (2026-04-14)
```

Ikon konvenciók:
- 🔴 critical priority
- 🟠 high priority
- 🟡 medium / new task
- ✅ archive

## Epic ID sorszámozás terminálonként

| Terminál | Prefix | Aktuális max |
|---|---|---|
| Kernel | KERNEL- | 067 |
| Orchestrator | ORCH- | 060 |
| Infra | INFRA- | 071 |
| E2E | E2E- | 014 |
| Portal | PORTAL- | — |
| Joinery | JOINERY- | V2 |
| Abstractions | ABSTRACTIONS- | — |
| Batch cleanup | BATCH-0-CLEANUP | — |

> Ez a táblázat gyors tájékoztató — az aktuális max mindig `ls docs/tasks/archive/ | grep <PREFIX>` alapján ellenőrizendő.

## Visszadobás esetén

Task fájl **marad `active/`-ban** — csak a `updated:` dátumot frissítsd és adj hozzá megjegyzést:

```yaml
---
...
updated: YYYY-MM-DD
---

Visszadobva YYYY-MM-DD: <hiánylista röviden>. Új inbox: MSG-<TERMINAL>-<NNN>.
```

## BATCH-0-CLEANUP epics

Speciális epic-ek meglévő kód javítására, amelyek nem blokkolják az aktív Batch-et:

```
BATCH-0-CLEANUP_legacy-fails.md   ← master task (new/)
  CLEANUP-01: FlowEpic /close FSM  → KERNEL-067 (active)
  CLEANUP-02: Node register 500    → KERNEL-068 (new)
  CLEANUP-03: TenantSummary count  → KERNEL-069 (new)
```

Aktiválás: Batch 3 (E2E-014, 015, 016) lezárása után.
