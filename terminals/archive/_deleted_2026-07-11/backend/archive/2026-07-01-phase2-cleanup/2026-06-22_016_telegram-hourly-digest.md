---
id: MSG-BACKEND-016
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-CONDUCTOR-007
created: 2026-06-22
content_hash: d23d0f806bff0c91ae04f94b0f7b7701ab873dfd9415796f091b9a0ce914198d
---

# Telegram Hourly Digest Implementation

## Kontextus

Az autonóm fejlesztési folyamatok átláthatóságának növelése érdekében óránkénti összefoglaló üzenetet kell küldeni Telegramra.

## Feladat

Implementálj egy hourly digest rendszert, amely óránként automatikusan összefoglalót küld a SpaceOS autonomous dev aktivitásról.

## Specifikáció

### Üzenet formátum

```
📊 SpaceOS Hourly Digest (HH:00)
━━━━━━━━━━━━━━━━━━━━━━━
🤖 Autonomous cycles: X (Y skipped)
✅ Tasks completed: Z
⏳ Tasks in progress: N
🚨 Blockers: M

Terminals:
• conductor: X tasks dispatched
• backend: Y DONE
• frontend: status

Next cycle: HH:MM
```

### Implementáció részletek

**Új fájl:** `spaceos-nexus/knowledge-service/src/pipeline/hourlyDigest.ts`

**Scheduler:**
- Cron expression: `0 * * * *` (minden óra 0. percében)
- Integrálás: `src/server.ts`-be

**Adatforrások:**
- `/api/autonomous/status` endpoint
- `/api/dashboard` endpoint
- Session history: `logs/sessions/`

**Telegram küldés:**
- Használd a meglévő `telegram()` helper függvényt
- Token: `.datahaven-token` fájlból olvasva

### Komponensek

1. **hourlyDigest.ts:**
   ```typescript
   export interface DigestData {
     timestamp: Date;
     autonomousCycles: { total: number; skipped: number };
     tasksCompleted: number;
     tasksInProgress: number;
     blockers: number;
     terminals: Record<string, string>;
     nextCycle: Date;
   }

   export async function collectDigestData(): Promise<DigestData>
   export function formatDigestMessage(data: DigestData): string
   export async function sendHourlyDigest(): Promise<void>
   ```

2. **Scheduler integráció (`src/server.ts`):**
   ```typescript
   import cron from 'node-cron';
   import { sendHourlyDigest } from './pipeline/hourlyDigest';

   cron.schedule('0 * * * *', sendHourlyDigest);
   ```

3. **Unit testek:** `src/pipeline/__tests__/hourlyDigest.test.ts`

## Acceptance Criteria

- [ ] Óránként (0. percben) automatikusan fut
- [ ] Telegram üzenet formátum pontos
- [ ] Autonomous cycle statisztikák helyesek
- [ ] Session history aggregáció működik
- [ ] Terminál státuszok frissek
- [ ] Unit testek lefedik a collect/format/send funkciókat

## Megjegyzések

- Az `/api/autonomous/status` endpoint már létezik (`src/routes/autonomous.ts`)
- A `telegram()` helper már elérhető (`src/utils/telegram.ts`)
- Hibakezelés: ha Telegram API fail, logold de ne crasheljen a scheduler

## Definition of Done

1. Kód implementálva
2. Unit testek írva és átmennek
3. Manuális teszt: egy digest üzenet sikeresen elküldve
4. DONE outbox üzenet küldése a tesztelt üzenet screenshot-jával
