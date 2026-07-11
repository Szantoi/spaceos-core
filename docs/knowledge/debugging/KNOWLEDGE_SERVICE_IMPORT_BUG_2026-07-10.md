# Knowledge Service Import Path Bug Fix (2026-07-10)

**Task:** MSG-ROOT-048 (related)
**Priority:** CRITICAL
**Status:** FIXED
**Date:** 2026-07-10

---

## Problem Statement

**Knowledge service crash loop** preventing all MCP tools from working, including session management, inbox processing, and DONE pipeline.

### Evidence

```bash
$ sudo systemctl status spaceos-knowledge
Active: activating (auto-restart) (Result: exit-code)
Process: ExecStart=/usr/bin/npx ts-node src/server.ts (code=exited, status=1/FAILURE)

$ npx ts-node src/server.ts
Error: Cannot find module './conversationManager.js'
Require stack:
- /opt/spaceos/spaceos-nexus/knowledge-service/src/telegram/contextBuilder.ts
```

---

## Root Cause

**File:** `spaceos-nexus/knowledge-service/src/telegram/contextBuilder.ts:23`

```typescript
import { ConversationMessage, Conversation, getConversationMessages } from './conversationManager.js';
//                                                                                              ^^^
//                                                                                     BUG: .js extension
```

### Why It Fails

1. **tsconfig.json** uses `"module": "commonjs"`
2. **ts-node** compiles TypeScript on-the-fly
3. CommonJS require() doesn't resolve `.js` extensions for `.ts` files
4. Node.js looks for literal `conversationManager.js` file (doesn't exist in src/)

---

## Fix Applied

```typescript
// BEFORE (broken)
import { ... } from './conversationManager.js';

// AFTER (fixed)
import { ... } from './conversationManager';
```

---

## Prevention Patterns

### 1. ESLint rule for import extensions

```json
{
  "rules": {
    "import/extensions": ["error", "never", { "json": "always" }]
  }
}
```

### 2. TypeScript strict module resolution

```json
{
  "compilerOptions": {
    "moduleResolution": "node",
    "allowImportingTsExtensions": false
  }
}
```

### 3. Pre-commit build check

```bash
npm run build || exit 1
```

---

## Secondary Issue: messageRegistry.db Empty

### Problem

After the service crashed, `messageRegistry.db` was created as 0-byte file:

```bash
$ ls -la data/message-registry.db
-rw-r----- 1 gabor gabor 0 Jul  8 13:29 message-registry.db
```

### Why

1. Service crashed before `initSchema()` completed
2. `better-sqlite3` creates empty file on first access
3. Service kept crash-looping, never initializing tables

### Fix

```bash
rm data/message-registry.db
sudo systemctl restart spaceos-knowledge
# Service recreates with proper schema
```

### Result

```bash
$ sqlite3 data/message_registry.db "SELECT COUNT(*) FROM messages;"
1313
```

---

## Related Files

- `spaceos-nexus/knowledge-service/src/telegram/contextBuilder.ts` (fixed)
- `spaceos-nexus/knowledge-service/src/messageRegistry.ts` (schema definition)
- `spaceos-nexus/knowledge-service/data/message_registry.db` (recreated)

---

## Impact Timeline

| Time | Event |
|------|-------|
| 2026-07-08 13:29 | Service crashed, empty DB created |
| 2026-07-08 - 2026-07-10 | DONE pipeline stopped (33+ hours) |
| 2026-07-10 07:23 | Fix applied, service restored |
| 2026-07-10 07:23 | messageRegistry backfilled (1313 messages) |

---

_Knowledge captured by: Root terminal_
_Date: 2026-07-10_
_Ref: MSG-ROOT-048, knowledge-service recovery_
