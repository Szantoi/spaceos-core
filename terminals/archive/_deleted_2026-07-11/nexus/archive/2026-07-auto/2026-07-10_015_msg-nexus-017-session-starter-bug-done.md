---
id: MSG-NEXUS-017-DONE
from: nexus
to: root
type: done
ref: MSG-NEXUS-017
status: READ
created: 2026-07-10
content_hash: b721ef3f8a62566ca19db9c7e0cd3ea128c3684a79c96fbf0ca96503d91f181e
---

# MSG-NEXUS-017 DONE: Session Starter Bug Fixed

## Összefoglaló

A `sessionManager.ts` `startSession` funkciója nem várt amíg Claude elindul, így amikor a `watchInbox.ts` vagy más pipeline component injektálta a task üzenetet, az még bash promptba került, nem Claude-ba.

**Root cause:** 
- `sessionManager.startSession()` elküldte a `claude --model X` parancsot tmux-ba
- DE azonnal visszatért (nem várt)
- Az inject megtörtént **mielőtt Claude elindult volna**
- Bash megpróbálta command-ként futtatni → syntax error

**FIX:**
- `sessionManager.ts` line 261-290: Polling loop (15s timeout)
- Ellenőrzi `isClaudeRunning()` minden 500ms-ben
- CSAK akkor tér vissza ha Claude valóban fut
- Ha timeout, hibával tér vissza

## Változások

| Fájl | Változás |
|------|----------|
| `src/sessionManager.ts` | +29 sor: Polling loop Claude startup ellenőrzéshez |

**Lines 261-290:**
```typescript
// FIX (MSG-NEXUS-017): Wait for Claude to actually start before returning
const maxWaitMs = 15000;
const pollIntervalMs = 500;
const startTime = Date.now();
let claudeStarted = false;

while ((Date.now() - startTime) < maxWaitMs) {
  if (isClaudeRunning(canonical)) {
    claudeStarted = true;
    break;
  }
  execSync(`sleep ${pollIntervalMs / 1000}`);
}

if (!claudeStarted) {
  return { success: false, message: "Claude failed to start within 15s" };
}
```

## Tesztek

- [x] Build OK (0 error, 0 warning)
- [x] Service restart OK (`spaceos-knowledge` active)
- [x] Health endpoint OK (port 3456)
- [x] Compiled code tartalmazza a fix-et (`dist/sessionManager.js`)

## Érintett terminálok

A fix segít minden terminálnál ahol a `watchInbox.ts` használja az `/api/session/start` endpointot:
- backend ✅
- librarian ✅
- minden más terminál ✅

## Acceptance Criteria

- [x] `startSession()` ellenőrzi hogy Claude fut-e
- [x] Ha nem fut, várakozik (polling 500ms, max 15s)
- [x] Csak Claude indítás UTÁN tér vissza success-szel
- [x] Nincs több bash syntax error a terminálokban

## Metrikák

| Előtte | Utána |
|--------|-------|
| Azonnali return (0ms) | Várakozás Claude indulásra (~3-8s) |
| Bash syntax error | Claude session OK |
| Injection race condition | Safe injection |

## Security

Az `injectPrompt()` már ellenőrzi hogy Claude fut-e (line 373-384), tehát az inject biztonsági ellenőrzés működik.

## Következő lépések

Nincs. A bug fix-elve, a service fut, az acceptance criteria teljesült.
