---
id: MSG-NEXUS-017
from: root
to: nexus
type: task
priority: high
status: READ
created: 2026-07-10
content_hash: d5fd76577a95d82f2052808c84726e8b86763f02a12fe9bb920b4db2262cf56a
---

# Session Starter Bug: TASK ASSIGNED üzenet bash-ba megy Claude helyett

## Bug leírás

A `sessionStarter.ts` vagy `watchInbox.ts` a `[TASK ASSIGNED]` üzenetet bash promptba küldi, nem Claude session-be.

## Reprodukálás

1. Terminál bash prompton áll (nincs Claude futtatva)
2. UNREAD inbox üzenet érkezik
3. Session starter injektálja a `[TASK ASSIGNED]` üzenetet
4. Bash syntax error: `-bash: syntax error near unexpected token '('`

## Érintett terminálok (ma)

- backend
- librarian

## Elvárt viselkedés

1. Ellenőrizni, hogy Claude fut-e a tmux pane-ben
2. Ha NEM fut: először `claude --dangerously-skip-permissions` indítás
3. Várni amíg Claude elindul (prompt megjelenik)
4. CSAK EZUTÁN küldeni a task üzenetet

## Javasolt megoldás

```typescript
// sessionStarter.ts vagy watchInbox.ts

async function ensureClaudeRunning(session: string): Promise<boolean> {
  const paneContent = await capturePane(session);
  
  // Ellenőrizni, hogy Claude fut-e
  if (paneContent.includes('Claude Code') || paneContent.includes('> ')) {
    return true; // Claude már fut
  }
  
  // Ha bash prompt van, indítsd el Claude-ot
  if (paneContent.includes('gabor@') || paneContent.includes('$ ')) {
    await tmuxSendKeys(session, 'claude --dangerously-skip-permissions');
    await sleep(5000); // Várj amíg elindul
    return true;
  }
  
  return false;
}
```

## Fájlok

- `spaceos-nexus/knowledge-service/src/sessionStarter.ts`
- `spaceos-nexus/knowledge-service/src/pipeline/watchInbox.ts`

## Acceptance Criteria

- [ ] Session starter ellenőrzi, hogy Claude fut-e a tmux pane-ben
- [ ] Ha nem fut Claude, automatikusan indítja
- [ ] Csak Claude indítás UTÁN küldi a task üzenetet
- [ ] Nincs több bash syntax error a terminálokban
