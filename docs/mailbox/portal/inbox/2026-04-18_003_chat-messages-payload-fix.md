---
id: MSG-PORTAL-003
from: root
to: portal
type: task
priority: high
status: READ
ref: MSG-TESTER-011
created: 2026-04-18
---

# BUG-005c — Chat messages payload üres tömb

A BUG-005a (SSE URL) és BUG-005b (chunk format) fix után (`906f414` LIVE) a TESTER reteszten:

```json
// Tényleges request body (helytelen):
{ "messages": [], "context": { "tenantId": "6b14afc9-..." } }

// Elvárt:
{ "messages": [{ "role": "user", "content": "Szia, ez egy teszt" }], "context": { ... } }
```

**Diagnózis:** A Chat komponens `handleSend` (vagy hasonló) nem fűzi hozzá a textarea tartalmát a `messages` tömbhöz küldés előtt. A textarea state és a messages state szinkronizációja hibás.

## Vizsgálandó

`design-portal/apps/joinerytech/src/` — Chat komponens:

1. **`handleSend` / `onSubmit`** — hogyan épül fel a POST body? A textarea értéke (`inputValue` vagy `message` state) belekerül-e a `messages` tömbbe?
2. **`useStreamingChat.ts`** — a `sendMessage(content)` függvény: az argumentum tényleg bekerül-e a body-ba?
3. Tipikus hiba: a messages state frissítés (`setMessages([...messages, newMsg])`) és a fetch hívás sorrendje — ha a fetch a régi (üres) messages-t használja.

## Elvárt fix

A textarea tartalma (`user` role-lal) kerüljön bele a küldött `messages` tömbbe:
```typescript
// Valami ilyesmi:
const userMessage = { role: 'user', content: inputValue };
sendMessage([...messages, userMessage]);
```

## DoD

- [ ] `POST /bff/chat/stream` body: `messages` tömb tartalmazza a user üzenetet
- [ ] Chat streaming válasz megjelenik (nem 422)
- [ ] `npm test` → ≥309 pass
- [ ] INFRA deploy szükséges a fix után

---

*Skill: `/spaceos-terminal`*
