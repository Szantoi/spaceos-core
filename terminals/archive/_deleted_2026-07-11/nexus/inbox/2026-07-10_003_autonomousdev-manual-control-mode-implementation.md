---
id: MSG-NEXUS-003
from: root
to: nexus
type: task
priority: high
status: PROCESSED
model: sonnet
created: 2026-07-10
content_hash: 99d0fe25d5e7be3ee7a0283894d1d92ea49061e37a119152df2d3b841f656320
---

# AutonomousDev Manual Control Mode Implementation

## Kontextus

Az ISSUES.md #9 szerint az AutonomousDev és a manuális kontroll ütközik — ha Root inbox üzenetet küld, az AutonomousDev 30 perc múlva felülírja.

**Jelenlegi workaround:** `curl -X POST http://localhost:3456/api/autonomous/stop`

## Feladat

Implementáld a control mode awareness-t az AutonomousDev-ben:

### 1. Control Mode Típusok
```typescript
type ControlMode = 'manual' | 'autonomous' | 'hybrid';
```

### 2. Logika
- **manual**: AutonomousDev NEM indít session-öket
- **autonomous**: Normál működés (jelenlegi)
- **hybrid**: Skip-el ha UNREAD inbox van

### 3. API Endpoints
```
GET  /api/control/mode → { mode: 'manual' | 'autonomous' | 'hybrid' }
POST /api/control/mode → { mode: '...' }
```

### 4. Session Metadata
- `startedBy: 'manual' | 'autonomous'` mező hozzáadása

## Érintett Fájlok
- `spaceos-nexus/knowledge-service/src/pipeline/autonomousDev.ts`
- `spaceos-nexus/knowledge-service/src/sessionStarter.ts`
- Új: `src/interfaces/http/routes/control.routes.ts`

## Acceptance Criteria
- [ ] Manual mode: autonomous NEM indít session-t
- [ ] Hybrid mode: skip ha UNREAD inbox van
- [ ] Session metadata tartalmazza `startedBy` mezőt
- [ ] `/api/control/mode` GET/POST működik
- [ ] Unit tesztek (>90% coverage)

## Acceptance Criteria

- [ ] Manual mode: autonomous NEM indít session-t
- [ ] Hybrid mode: skip ha UNREAD inbox van
- [ ] Session metadata tartalmazza startedBy mezőt
- [ ] /api/control/mode GET/POST működik
- [ ] Unit tesztek (>90% coverage)
