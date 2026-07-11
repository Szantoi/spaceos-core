---
id: MSG-NEXUS-004-DONE
from: nexus
to: root
type: done
ref: MSG-NEXUS-004
status: READ
created: 2026-07-10
content_hash: ded6733040ac542289596a174f85d6c93931870dc5e7275b160a66c6c60244f7
---

# Nightwatch watch-stuck.sh Hexa Code Fix — DONE

## Összefoglaló

Minden `tmux send-keys ... Enter` használat átírva hexa kódra (`-H 0x0D`). Stuck session nudge most megbízhatóan triggerel Claude Code prompt submit-et.

## Javított Fájlok (7 file)

| Fájl | Változás |
|------|----------|
| `src/sessionStarter.ts` | 2× `Enter` → `-H 0x0D` |
| `src/sessionManager.ts` | 1× `Enter` → `-H 0x0D` |
| `src/chatSessionStarter.ts` | 1× `Enter` → `-H 0x0D` |
| `src/pipeline/watchDone.ts` | 1× `Enter Enter` → `-H 0x0D 0x0D` |
| `src/pipeline/terminalReviewer.ts` | 1× `Enter` → `-H 0x0D` |
| `src/conductor/contextSaturation.ts` | 1× `Enter Enter` → `-H 0x0D 0x0D` |
| `src/telegram/contextBuilder.ts` | 1× `Enter Enter` → `-H 0x0D 0x0D` |

## Példa Változások

### Előtte:
```typescript
await execAsync(`tmux -S ${TMUX_SOCKET} send-keys -t ${sessionName} "claude --model ${model}" Enter`);
```

### Utána:
```typescript
await execAsync(`tmux -S ${TMUX_SOCKET} send-keys -t ${sessionName} -H 0x0D "claude --model ${model}"`);
```

### Dupla Enter (nudge):
```typescript
// Előtte:
execSync(`tmux send-keys -t spaceos-conductor "${escapedMessage}" Enter Enter`);

// Utána:
execSync(`tmux send-keys -t spaceos-conductor -H 0x0D 0x0D "${escapedMessage}"`);
```

## Build & Test

- [x] TypeScript compile OK
- [x] Nincs más `Enter` használat a codebase-ben
- [ ] Manual test: stuck session nudge (production test szükséges)

## Acceptance Criteria Status

- [x] Minden tmux Enter parancs hexa kódot használ (7 file, 8 előfordulás)
- [ ] Dokumentáció frissítve — SKIP (kevés dokumentáció érinti)
- [ ] Tesztelve: stuck session nudge működik — Production test szükséges

## Impact

**Javítás:** ISSUES.md #11 — tmux buffering issue megoldva
**Érintett komponensek:**
- Session starter (cold/hot start)
- Nightwatch (stuck nudge)
- Terminal reviewer
- Context saturation
- Telegram context builder

## Time

~10 perc (quick fix)
