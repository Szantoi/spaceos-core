# Marveen Inspiráció — SpaceOS Nexus Átalakítás

> **Forrás:** https://github.com/Szotasz/marveen
> **Dátum:** 2026-06-21
> **Cél:** Agent orkesztráció új alapokra helyezése

---

## Marveen Architektúra Összefoglaló

```
┌─────────────────────────────────────────────────────────────────┐
│                         MARVEEN                                 │
│              "AI csapatod, ami fut amíg te alszol"              │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─── Channels Layer ───┐     ┌─── Session Layer ───┐          │
│  │  Telegram/Slack      │     │  tmux agent-*       │          │
│  │  channel-coordinator │────►│  pane-state.ts      │          │
│  │  (backfill fallback) │     │  (idle/busy/error)  │          │
│  └──────────────────────┘     └──────────────────────┘          │
│                                                                 │
│  ┌─── Heartbeat ────────┐     ┌─── Auto-Restart ────┐          │
│  │  Óránként fut        │     │  Napi vagy N óránként│         │
│  │  - OAuth token sync  │     │  - fresh: clean start│         │
│  │  - Keychain read     │     │  - continue: keep ctx│         │
│  │  - Credentials copy  │     │  "Nightly dream"     │         │
│  └──────────────────────┘     └──────────────────────┘          │
│                                                                 │
│  ┌─── launchd/systemd ──┐                                       │
│  │  OS-szintű daemon    │                                       │
│  │  Automatikus restart │                                       │
│  │  ha a process meghal │                                       │
│  └──────────────────────┘                                       │
└─────────────────────────────────────────────────────────────────┘
```

---

## Kulcs Komponensek

### 1. `pane-state.ts` — Session Állapot Detektálás

**7 lehetséges állapot, részletes regex-alapú detektálás:**

```typescript
type PaneState = 'idle' | 'busy' | 'typing' | 'error' | 'unknown';

// Busy indicators (spinner, token counter)
const BUSY_INDICATORS = [
  /⠋|⠙|⠹|⠸|⠼|⠴|⠦|⠧|⠇|⠏/,  // spinner karakterek
  /\(\d+s · ↓\d+/,              // token counter "(5s · ↓123)"
];

// Idle footer
const IDLE_FOOTER_RX = /bypass permissions on/;

// Error patterns
const ERROR_PATTERNS = /API error|rate limit|overloaded/i;
```

**Detektálási sorrend:**
1. Üres pane → `'unknown'`
2. Spinner/token-számláló → `'busy'`
3. Nincs idle footer → `'unknown'`
4. API hiba → `'error'`
5. Paste placeholder → `'busy'`
6. Parkoltatott szöveg → `'typing'`
7. Egyéb → `'idle'`

---

### 2. `auto-restart.ts` — "Nightly Dream Consolidation"

**Cél:** Megakadályozni a context window felpuffadást és erőforrás-szivárgást.

```typescript
// Két ütemezési mód:
type RestartSchedule =
  | { type: 'daily', hour: number }      // pl. 03:00
  | { type: 'interval', hours: number }; // pl. 8 óránként

// Két újraindítási mód:
type RestartMode =
  | 'fresh'    // Teljes újraindítás, context eldobása
  | 'continue'; // Megőrzi a beszélgetést, de friss process
```

**Logika:**
- Naponta 03:00-kor fresh restart
- Vagy N óránként intervallum alapú
- "Az aktuális idő eléri az ütemezett időpontot ÉS nem történt már újraindítás"

---

### 3. `heartbeat.ts` — Óránkénti Monitoring

**Funkciók:**
- OAuth token szinkronizáció (Keychain → .credentials.json)
- Rendszer-státusz összegyűjtése
- Értesítések küldése ha szükséges
- HEARTBEAT_START_HOUR és HEARTBEAT_END_HOUR között fut

**Credential kezelés:**
```typescript
// macOS Keychain-ből olvassa a Claude Code OAuth tokent
const credentials = await readClaudeCodeOauthJson();
// Minden órában frissíti, így a rotált tokenek elérhetők
await writeCredentialsFile(credentials);
```

---

### 4. `channel-coordinator.ts` — Backfill Fallback

**Hibrid modell:**
- A natív Telegram plugin az elsődleges
- Coordinator csak figyelő (5 másodperces tick)
- 2 egymást követő DOWN jel → backfill mód
- Duplikáció megelőzés: offset alapú szűrés

---

## Összehasonlítás: Marveen vs SpaceOS Nexus

| Komponens | Marveen | SpaceOS Nexus (jelenlegi) | TODO |
|---|---|---|---|
| **Session state detection** | `pane-state.ts` - 7 állapot | `watchStuck.ts` - 2-3 pattern | Bővíteni! |
| **Auto-restart** | Napi/óránkénti fresh restart | ❌ Nincs | Implementálni |
| **Heartbeat** | Óránkénti OAuth + monitoring | ❌ Nincs | Implementálni |
| **Channel fallback** | Backfill ha plugin DOWN | ❌ Nincs | Opcionális |
| **OS daemon** | launchd/systemd | Crontab (hiányos!) | Systemd service |
| **Credential sync** | Keychain → file | Env var alapú | Nem szükséges |

---

## Implementációs Terv

### Fázis 1: `pane-state.ts` Bővítés

**Fájl:** `spaceos-nexus/knowledge-service/src/pipeline/paneState.ts`

```typescript
export type PaneState = 'idle' | 'busy' | 'typing' | 'error' | 'unknown';

const SPINNER_RX = /⠋|⠙|⠹|⠸|⠼|⠴|⠦|⠧|⠇|⠏/;
const TOKEN_COUNTER_RX = /\(\d+s · ↓\d+/;
const IDLE_FOOTER_RX = /bypass permissions on|shift\+tab to cycle/;
const ERROR_RX = /API error|rate limit|overloaded|Error:|ECONNREFUSED/i;
const PASTE_PLACEHOLDER_RX = /\[Pasted text #\d+\]/;

export async function detectPaneState(session: string): Promise<PaneState> {
  const output = await capturePane(session, 30);

  if (!output || output.trim().length < 10) return 'unknown';

  // Busy indicators (last 10 lines)
  const lastLines = output.split('\n').slice(-10).join('\n');
  if (SPINNER_RX.test(lastLines)) return 'busy';
  if (TOKEN_COUNTER_RX.test(lastLines)) return 'busy';

  // Error state
  if (ERROR_RX.test(lastLines)) return 'error';

  // Paste placeholder (temporary busy)
  if (PASTE_PLACEHOLDER_RX.test(lastLines)) return 'busy';

  // Idle check
  if (IDLE_FOOTER_RX.test(lastLines)) return 'idle';

  return 'unknown';
}
```

---

### Fázis 2: Auto-Restart Implementálás

**Fájl:** `spaceos-nexus/knowledge-service/src/pipeline/autoRestart.ts`

```typescript
interface RestartConfig {
  enabled: boolean;
  schedule: 'daily' | 'interval';
  hour?: number;        // daily: 03
  intervalHours?: number; // interval: 8
  mode: 'fresh' | 'continue';
}

const DEFAULT_CONFIG: RestartConfig = {
  enabled: true,
  schedule: 'daily',
  hour: 3,
  mode: 'fresh',
};

export async function checkAndRestart(session: string, config = DEFAULT_CONFIG): Promise<boolean> {
  if (!config.enabled) return false;

  const now = new Date();
  const lastRestart = await getState(`${session}_last_restart`);

  if (config.schedule === 'daily') {
    if (now.getHours() === config.hour && !isToday(lastRestart)) {
      await restartSession(session, config.mode);
      await setState(`${session}_last_restart`, now.toISOString());
      return true;
    }
  }

  return false;
}
```

---

### Fázis 3: Heartbeat Implementálás

**Fájl:** `spaceos-nexus/knowledge-service/src/pipeline/heartbeat.ts`

```typescript
interface HeartbeatResult {
  timestamp: string;
  terminals: TerminalHealth[];
  alerts: Alert[];
}

export async function runHeartbeat(): Promise<HeartbeatResult> {
  const terminals = await checkAllTerminals();
  const alerts: Alert[] = [];

  for (const t of terminals) {
    if (t.state === 'error') {
      alerts.push({ terminal: t.name, type: 'error', message: t.errorMessage });
    }
    if (t.state === 'idle' && t.idleMinutes > 30 && t.hasUnreadInbox) {
      alerts.push({ terminal: t.name, type: 'stuck', message: 'Idle with unread inbox' });
    }
  }

  if (alerts.length > 0) {
    await sendTelegramAlerts(alerts);
  }

  return { timestamp: new Date().toISOString(), terminals, alerts };
}

// Óránként futtatás
export function startHeartbeatScheduler(intervalMs = 3600000): void {
  setInterval(runHeartbeat, intervalMs);
}
```

---

### Fázis 4: Systemd Service

**Fájl:** `/etc/systemd/system/spaceos-nexus.service`

```ini
[Unit]
Description=SpaceOS Nexus Knowledge Service
After=network.target chromadb.service

[Service]
Type=simple
User=spaceos
WorkingDirectory=/opt/spaceos/spaceos-nexus/knowledge-service
ExecStart=/usr/bin/node dist/server.js
Restart=always
RestartSec=10
Environment=NODE_ENV=production
Environment=ENABLE_NIGHTWATCH=true
Environment=ENABLE_HEARTBEAT=true
Environment=ENABLE_AUTO_RESTART=true

[Install]
WantedBy=multi-user.target
```

---

## Prioritás

1. **P0 (kritikus):** `pane-state.ts` bővítés — jelenleg nem detektálja megfelelően az állapotokat
2. **P1 (fontos):** Auto-restart — megakadályozza a context felpuffadást
3. **P2 (hasznos):** Heartbeat — proaktív monitoring és alerting
4. **P3 (nice-to-have):** Systemd service — production-ready daemon

---

## Referenciák

- **Marveen GitHub:** https://github.com/Szotasz/marveen
- **Kulcs fájlok:**
  - `src/pane-state.ts` — session állapot detektálás
  - `src/auto-restart.ts` — automatikus újraindítás
  - `src/heartbeat.ts` — óránkénti monitoring
  - `src/channel-coordinator.ts` — channel fallback
- **SpaceOS Nexus jelenlegi:**
  - `src/pipeline/watchStuck.ts` — egyszerűsített stuck detektálás
  - `src/pipeline/watchPriority.ts` — priority session indítás
  - `src/pipeline/nightwatch.ts` — 2 percenkénti dispatcher

---

*Utolsó frissítés: 2026-06-21*
