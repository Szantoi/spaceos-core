# AutonomousDev mode ütközik a manuális Conductor irányítással

**Dátum:** 2026-07-02
**Prioritás:** 🟡 High
**Komponens:** spaceos-nexus/knowledge-service (pipeline/autonomousDev.ts)
**Státusz:** Workaround applied

## Probléma

Az **AutonomousDev pipeline** folyamatosan újraindította a Conductor terminált 30 percenként, **felülírva a manuális irányítást** és inbox üzeneteket.

### Tünetek

1. Conductor session-ök indulnak, dolgoznak, majd lezárulnak
2. Manuális inbox üzenetek nem kerülnek feldolgozásra
3. A Conductor mindig ugyanazt az "autonomous prompt"-ot kapja:
   ```
   Focus file: /opt/spaceos/docs/tasks/new/Datahaven_UI_Focus_Flow_Editor_Architecture_v1.md
   Cold start: true
   ```
4. Nightwatch log mutatja az ismétlődő indításokat:
   ```
   04:14:43 [AutonomousDev] Cycle 22: Conductor started with optimized prompt
   04:40:43 [AutonomousDev] Cycle 1: Conductor started with optimized prompt
   05:10:13 [AutonomousDev] Cycle 2: Conductor started with optimized prompt
   ```

### Következmény

- **Manuális koordináció lehetetlenné válik**
- Root nem tud feladatokat kiosztani a Conductor-nak
- Inbox üzenetek feldolgozatlanok maradnak
- A system "autonomous" módba kerül user beavatkozás nélkül

## Gyökérok

Az AutonomousDev mode **nem érzékeli**, hogy:
1. Van-e UNREAD inbox üzenet a Conductor-nak
2. Fut-e már manuális Conductor session
3. Aktív-e a "manual control mode"

**Jelenlegi logika:**
```typescript
if (config.skipIfBusy && hasSession(SESSIONS.conductor)) {
  // Skip if session already exists
}
```

**Probléma:** A `hasSession` csak azt nézi, hogy létezik-e tmux session, **nem azt**, hogy manuális vagy autonomous session-e!

## Workaround (Applied)

```bash
curl -X POST http://localhost:3456/api/autonomous/stop
# → {"success":true,"message":"Scheduler stopped"}
```

**Mellékhatás:** Az autonomous mode **teljesen** leáll, nincs background fejlesztés.

## Javasolt javítás

### 1. Control Mode Awareness

```typescript
// src/pipeline/autonomousDev.ts
async function shouldStartCycle(): Promise<boolean> {
  // Check control mode
  const controlMode = await getControlMode(); // manual | autonomous | hybrid
  if (controlMode === 'manual') {
    log('[AutonomousDev] Skipping cycle: manual control mode active');
    return false;
  }

  // Check for UNREAD inbox messages
  const unreadCount = await getUnreadInboxCount('conductor');
  if (unreadCount > 0) {
    log(`[AutonomousDev] Skipping cycle: ${unreadCount} UNREAD inbox messages`);
    return false;
  }

  // Existing skipIfBusy check
  if (config.skipIfBusy && hasSession(SESSIONS.conductor)) {
    return false;
  }

  return true;
}
```

### 2. Hybrid Mode

Új control mode: **hybrid** (autonomous + manual együtt)
- Autonomous csak akkor indít, ha:
  - Nincs UNREAD inbox
  - Nincs aktív manuális session (last 30 perc)
  - Focus queue üres

### 3. Session Tagging

Minden Conductor session kapjon egy tag-et:
```typescript
interface SessionMetadata {
  sessionId: string;
  startedBy: 'autonomous' | 'manual' | 'nightwatch' | 'root';
  startedAt: string;
  task?: string;
}
```

**Benefit:** `hasSession` meg tudja különböztetni a manuális session-öket.

## API Improvements

### Új endpoint: `/api/control/mode`

```typescript
// GET /api/control/mode
{
  "mode": "manual" | "autonomous" | "hybrid",
  "setBy": "root",
  "setAt": "2026-07-02T07:15:00Z"
}

// POST /api/control/mode
{
  "mode": "manual",
  "reason": "Root manual coordination needed for JoineryTech"
}
```

### Conductor session metadata

```typescript
// GET /api/session/conductor
{
  "active": true,
  "startedBy": "manual",
  "startedAt": "2026-07-02T07:12:50Z",
  "currentTask": "MSG-CONDUCTOR-062",
  "canInterrupt": false  // Manual sessions cannot be interrupted
}
```

## Testing Checklist

- [ ] Manual mode: autonomous does NOT start Conductor
- [ ] Autonomous mode: starts Conductor every N minutes
- [ ] Hybrid mode: autonomous skips if manual inbox exists
- [ ] Session tagging: `startedBy` correctly logged
- [ ] API: `/api/control/mode` works (GET/POST)

## Priority

**HIGH** - Ez blokkolja a manuális koordinációt, ami kritikus a Root terminál számára.

## Next Steps

1. Implement control mode awareness
2. Add session tagging
3. Create `/api/control/mode` endpoint
4. Update AutonomousDev to respect manual control
5. Add integration tests

## Related

- Nightwatch pipeline coordination
- Root terminal manual control workflow
- Conductor inbox processing priority
