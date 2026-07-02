"use strict";
// watchMonitor.ts - Trigger monitor terminal for health checks
// Runs every 5th nightwatch cycle (10 minutes)
// The monitor terminal runs in cold mode: starts, checks health, writes report, terminates
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || (function () {
    var ownKeys = function(o) {
        ownKeys = Object.getOwnPropertyNames || function (o) {
            var ar = [];
            for (var k in o) if (Object.prototype.hasOwnProperty.call(o, k)) ar[ar.length] = k;
            return ar;
        };
        return ownKeys(o);
    };
    return function (mod) {
        if (mod && mod.__esModule) return mod;
        var result = {};
        if (mod != null) for (var k = ownKeys(mod), i = 0; i < k.length; i++) if (k[i] !== "default") __createBinding(result, mod, k[i]);
        __setModuleDefault(result, mod);
        return result;
    };
})();
Object.defineProperty(exports, "__esModule", { value: true });
exports.watchMonitor = watchMonitor;
exports.resetMonitorCycle = resetMonitorCycle;
const fs = __importStar(require("fs/promises"));
const path = __importStar(require("path"));
const child_process_1 = require("child_process");
const common_1 = require("./common");
// ADR-053: Mode #4 Program-Awareness (2026-07-02)
const modeDetection_1 = require("../conductor/modeDetection");
const epicManager_1 = require("../conductor/epicManager");
const TERMINALS_DIR = '/opt/spaceos/terminals';
const MONITOR_INBOX = path.join(TERMINALS_DIR, 'monitor', 'inbox');
const TMUX_SOCKET = '/tmp/spaceos.tmux';
const CYCLE_STATE_FILE = '/opt/spaceos/logs/dispatcher/.monitor-cycle-state';
// Track cycles to run every 5th one (10 minutes with 2-min interval)
// Persisted to file to survive service restarts
let cycleCount = 0;
// ─── Persistent Cycle Counter Helpers ──────────────────────────────────────
/**
 * Load cycle count from persistent file
 * Returns 0 if file doesn't exist or is invalid
 */
async function loadCycleCount() {
    try {
        const content = await fs.readFile(CYCLE_STATE_FILE, 'utf-8');
        const count = parseInt(content.trim(), 10);
        return isNaN(count) ? 0 : count;
    }
    catch {
        return 0;
    }
}
/**
 * Save cycle count to persistent file
 */
async function saveCycleCount(count) {
    try {
        await fs.writeFile(CYCLE_STATE_FILE, String(count), 'utf-8');
    }
    catch (error) {
        console.error('[watchMonitor] Failed to save cycle count:', error);
    }
}
// ─── ADR-053: Mode-Aware Health Check Builder ──────────────────────────────
/**
 * Build mode-aware health check prompt for Monitor
 * Generates different check lists based on operation mode:
 * - structured_program: Epic, checkpoint, Conductor on-program checks
 * - planning_pipeline: Planning queue and idea generation checks
 * - manual: Default general checks
 */
function buildModeAwareHealthCheckPrompt() {
    try {
        const mode = (0, modeDetection_1.detectOperationMode)();
        let prompt = `# Scheduled Health Check — Mode-Aware

**Operációs mód:** \`${mode}\`

---

`;
        if (mode === 'structured_program') {
            const activeEpic = (0, epicManager_1.loadActiveEpic)();
            prompt += `## 🎯 Mode #4 Structured Program Health Checks

### 1. **Epic Status**
\`\`\`
- [ ] EPICS.yaml létezik és olvasható
- [ ] Active epic jelenlévő: ${activeEpic ? `✅ ${activeEpic.id}` : '❌ MISSING'}
- [ ] Checkpoint COUNT: ${activeEpic?.checkpoints?.length || 0}
- [ ] Progress: ${activeEpic ? (0, epicManager_1.getEpicProgress)(activeEpic) + '%' : 'N/A'}
\`\`\`

### 2. **Checkpoint Status** (KRITIKUS!)
\`\`\`
${activeEpic && activeEpic.checkpoints
                ? activeEpic.checkpoints
                    .map((cp, idx) => {
                    const status = cp.status === 'done'
                        ? '✅'
                        : cp.status === 'pending'
                            ? '⏳ PENDING'
                            : '⚠️ ' + String(cp.status).toUpperCase();
                    return `- [ ] ${status} ${cp.id}: ${cp.name}`;
                })
                    .join('\n')
                : '- [ ] Nincs checkpoint'}
\`\`\`

### 3. **Conductor On-Program Check** (FONTOS!)
\`\`\`
- [ ] Conductor terminál fut-e? (tmux: spaceos-conductor)
- [ ] Recent tasks match epic? (CHECK outbox DONE)
- [ ] Conductor <30 min idle-e MUNKA NÉLKÜL?
- [ ] Ha idle + munka: Conductor inbox message ("Folytatható munka észlelve")
\`\`\`

### 4. **BLOCKED Messages Check** (FIGYELJ!)
\`\`\`
- [ ] BLOCKED count <20
- [ ] BLOCKED messages <24h old
- [ ] Kritikus BLOCKED-ok felderítve? (pl. MSG-BACKEND-119)
\`\`\`

### 5. **Nightwatch Activity** (ALAPVETÕ)
\`\`\`
- [ ] Nightwatch script lefutott <2h
- [ ] logs/dispatcher/pipeline.log frissül
- [ ] logs/dispatcher/nightwatch.log frissül
\`\`\`

### ❌ NE ELLENÕRIZZ (Mode #4-ben irreleváns):
\`\`\`
- ❌ Planning queue (disabled)
- ❌ Idea scan progress (disabled)
- ❌ Consensus documents (disabled)
\`\`\`

---

**Output:** Írj outbox összefoglalót. Ha probléma: BLOCKED hosszú óta vagy Conductor idle + munka, küldj Root inbox-ot.
`;
        }
        else if (mode === 'planning_pipeline') {
            prompt += `## 🧠 Mode #2/#3 Planning Pipeline Health Checks

### 1. **Planning Queue**
\`\`\`
- [ ] docs/planning/queue/ — hány item?
- [ ] Selected → Debate → Consensus szövegek feldolgozva?
- [ ] Consensus docs írva?
\`\`\`

### 2. **Idea Generation**
\`\`\`
- [ ] docs/planning/ideas/ — új ideák létrejöttek?
- [ ] Scan script futott <30 min?
- [ ] Idea count növekedett az utolsó check óta?
\`\`\`

### 3. **Pipeline Activity**
\`\`\`
- [ ] logs/dispatcher/pipeline.log frissül
- [ ] plan-debate.sh, plan-consensus.sh fut?
- [ ] Konszenzus generálódott az utolsó óra alatt?
\`\`\`

### 4. **Queue Processing**
\`\`\`
- [ ] Conductor queue-t feldolgozza?
- [ ] Conductor idle-e túl sokáig? (>30 min)
- [ ] Termináloknak megvan-e a munka?
\`\`\`

---

**Output:** Írj outbox összefoglalót. Ha probléma (queue stuck, idea scan fail, pipeline error), küldj Root inbox-ot.
`;
        }
        else {
            prompt += `## 🎯 Manual Mode Health Checks (Default)

### 1. **Terminál Status**
\`\`\`
- [ ] Melyik terminál fut? (tmux sessions list)
- [ ] Melyik idle?
- [ ] Hány UNREAD inbox üzenet? (terminálonként)
\`\`\`

### 2. **Outbox Status** (KRITIKUS!)
\`\`\`
- [ ] BLOCKED üzenetek? (kritikus!)
- [ ] DONE üzenetek feldolgozva?
- [ ] Unread outbox? (check status fájlok)
\`\`\`

### 3. **Service Health**
\`\`\`
- [ ] Knowledge Service (3456) up?
- [ ] Datahaven (3457) up?
- [ ] logs/dispatcher/ exist?
\`\`\`

### 4. **General Activity**
\`\`\`
- [ ] Nightwatch <2h ago?
- [ ] Pipeline log frissül?
- [ ] Hibák a log-okban?
\`\`\`

---

**Output:** Írj outbox összefoglalót. Ha probléma (service DOWN, BLOCKED, >5 UNREAD, Conductor idle), küldj Root inbox-ot.
`;
        }
        prompt += `
---

**Session mode:** Cold — fejezd be a session-t DONE után.
**Mode Note:** ADR-053 mode-aware health checks aktívak.
`;
        return prompt;
    }
    catch (error) {
        console.error('[watchMonitor] Failed to build mode-aware health check:', error);
        // Fallback to generic health check
        return `# Scheduled Health Check — Fallback (Mode Detection Error)

Nem sikerült az operációs mód meghatározása. Alapértelmezett ellenőrzés futhat.

Lásd: logs/dispatcher/nightwatch.log az err-ért.

---
**Session mode:** Cold — fejezd be a session-t DONE után.
`;
    }
}
async function watchMonitor() {
    // Load persistent cycle count
    cycleCount = await loadCycleCount();
    cycleCount++;
    await saveCycleCount(cycleCount);
    // Only run every 5th cycle (10 minutes)
    if (cycleCount % 5 !== 0) {
        await (0, common_1.log)(`[watchMonitor] Cycle ${cycleCount}/5 - skipping (persistent)`);
        return { triggered: false, reason: `Skipping (cycle ${cycleCount}/5)` };
    }
    await (0, common_1.log)(`[watchMonitor] Cycle ${cycleCount} - checking triggers (5th cycle!)`);
    // Skip if monitor session already running
    try {
        (0, child_process_1.execSync)(`tmux -S ${TMUX_SOCKET} has-session -t spaceos-monitor 2>/dev/null`, { stdio: 'pipe' });
        return { triggered: false, reason: 'Monitor session already running' };
    }
    catch {
        // Session not running - good, continue
    }
    // Skip if last check was <5 minutes ago (prevent spam)
    try {
        const files = await fs.readdir(MONITOR_INBOX);
        if (files.length > 0) {
            const lastFile = files.sort().pop();
            const lastFilePath = path.join(MONITOR_INBOX, lastFile);
            const stat = await fs.stat(lastFilePath);
            const diffSeconds = (Date.now() - stat.mtimeMs) / 1000;
            if (diffSeconds < 300) {
                return { triggered: false, reason: `Last check was ${Math.round(diffSeconds)}s ago (min 300s)` };
            }
        }
    }
    catch {
        // No files or error - continue
    }
    // Create inbox trigger message
    const date = new Date().toISOString().split('T')[0];
    const timestamp = new Date().toISOString().split('T')[1].split('.')[0];
    // Get next message number
    let num = 1;
    try {
        const files = await fs.readdir(MONITOR_INBOX);
        const todayFiles = files.filter(f => f.startsWith(date));
        num = todayFiles.length + 1;
    }
    catch {
        // Directory might not exist
        await fs.mkdir(MONITOR_INBOX, { recursive: true });
    }
    const messageId = `MSG-MONITOR-${String(num).padStart(3, '0')}`;
    const filename = `${date}_${String(num).padStart(3, '0')}_scheduled-health-check.md`;
    const filepath = path.join(MONITOR_INBOX, filename);
    // ADR-053: Use mode-aware health check prompt
    const modeAwarePrompt = buildModeAwareHealthCheckPrompt();
    const content = `---
id: ${messageId}
from: nightwatch
to: monitor
type: task
priority: low
status: UNREAD
model: haiku
created: ${date}
---

${modeAwarePrompt}

---
**Timestamp:** ${timestamp}
**Scheduled by:** nightwatch.sh (5-cycle interval)
`;
    await fs.writeFile(filepath, content, 'utf-8');
    await (0, common_1.log)(`[Monitor] Health check triggered (mode-aware): ${filename}`);
    return { triggered: true, reason: 'Mode-aware health check scheduled', messageId };
}
// Reset cycle count (useful for testing)
async function resetMonitorCycle() {
    cycleCount = 0;
    await saveCycleCount(0);
}
