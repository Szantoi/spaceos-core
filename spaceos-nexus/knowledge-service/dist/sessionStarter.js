"use strict";
// Session Starter - Starts tmux Claude sessions for terminals
// Called directly by InboxWatcher when new UNREAD message is detected
//
// Inspired by Marveen (https://github.com/Szotasz/marveen):
// - If session NOT running → start it
// - If session IS running → INJECT the inbox message via tmux send-keys
//
// 2026-06-24: Optimized to use messageRegistry DB for model detection
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
exports.getInjectedMessages = getInjectedMessages;
exports.buildEscalatedPrompt = buildEscalatedPrompt;
exports.terminateColdSession = terminateColdSession;
exports.startTerminalSession = startTerminalSession;
exports.startWorkSession = startWorkSession;
exports.generateWorkerId = generateWorkerId;
exports.startParallelWorkSession = startParallelWorkSession;
exports.spawnRawWorkers = spawnRawWorkers;
exports.collectRawResults = collectRawResults;
const child_process_1 = require("child_process");
const util_1 = require("util");
const fs_1 = require("fs");
const path = __importStar(require("path"));
const terminalsConfig = __importStar(require("./config/terminals"));
const terminals_1 = require("./config/terminals");
// ADR-046 Track B - session lifecycle hooks integrated
const sessionHooks_1 = require("./sessionHooks");
const messageRegistry_1 = require("./messageRegistry");
// ADR-053: Mode #4 Program-Awareness (2026-07-02)
const modeDetection_1 = require("./conductor/modeDetection");
const epicManager_1 = require("./conductor/epicManager");
const execAsync = (0, util_1.promisify)(child_process_1.exec);
// ─── Auto-INJECTED Status Helper ─────────────────────────────────────────────
// Marks inbox message as INJECTED in both filesystem and registry
// This prevents re-injection loops while preserving audit trail
async function markAsInjected(terminal, messageId) {
    const inboxPath = terminalsConfig.getInboxPath(terminal);
    try {
        const files = await fs_1.promises.readdir(inboxPath);
        for (const file of files.filter(f => f.endsWith('.md'))) {
            const filePath = path.join(inboxPath, file);
            const content = await fs_1.promises.readFile(filePath, 'utf-8');
            if (content.includes(`id: ${messageId}`) && content.includes('status: UNREAD')) {
                // Update file: UNREAD → INJECTED
                const newContent = content.replace('status: UNREAD', 'status: INJECTED\ninjected: ' + new Date().toISOString().split('T')[0]);
                await fs_1.promises.writeFile(filePath, newContent, 'utf-8');
                // Update registry
                await (0, messageRegistry_1.updateStatus)(messageId, 'INJECTED', 'sessionStarter', 'Task injected to terminal');
                console.log(`[SessionStarter] Marked ${messageId} as INJECTED`);
                return;
            }
        }
    }
    catch (err) {
        console.warn(`[SessionStarter] Failed to mark ${messageId} as INJECTED:`, err);
    }
}
// ─── Escalated Re-Inject Helper (2026-07-01) ─────────────────────────────────
// When INJECTED message has no response after timeout, send detailed instructions
// This is called by watchIdle when it detects an unresponsive INJECTED message
async function getInjectedMessages(terminal) {
    const inboxPath = terminalsConfig.getInboxPath(terminal);
    const results = [];
    try {
        const files = await fs_1.promises.readdir(inboxPath);
        for (const file of files.filter(f => f.endsWith('.md'))) {
            const filePath = path.join(inboxPath, file);
            const content = await fs_1.promises.readFile(filePath, 'utf-8');
            if (content.includes('status: INJECTED')) {
                const idMatch = content.match(/^id:\s*(.+)$/m);
                const injectedMatch = content.match(/^injected:\s*(.+)$/m);
                if (idMatch) {
                    results.push({
                        messageId: idMatch[1].trim(),
                        injectedAt: injectedMatch ? injectedMatch[1].trim() : '',
                    });
                }
            }
        }
    }
    catch {
        // Ignore errors
    }
    return results;
}
// Build escalated prompt with detailed expected behavior
function buildEscalatedPrompt(terminal, messageId) {
    return `⚠️ [ESCALATED TASK REMINDER] ${messageId}

A feladat ${messageId} korábban kiosztásra került, de nem érkezett válasz.

## ELVÁRT VISELKEDÉS

1. **AZONNAL** hívd meg:
   \`\`\`
   mcp__spaceos-knowledge__ack_task
     terminal: "${terminal}"
     message_id: "${messageId}"
   \`\`\`

2. **Dolgozd fel a feladatot** a leírás szerint

3. **Ha KÉSZ vagy**, hívd:
   \`\`\`
   mcp__spaceos-knowledge__complete_task
     terminal: "${terminal}"
     message_id: "${messageId}"
   \`\`\`

4. **Ha BLOKKOLT vagy**, írd ki:
   "BLOCKED: [ok leírása]"

## HA NEM ÉRTED A FELADATOT

Olvasd be újra:
- CLAUDE.md — terminál szabályok
- A feladat részleteiért: mcp__spaceos-knowledge__fetch_task

---
**Ez egy automatikus eszkaláció.** Ha 5 percen belül nincs válasz, a monitor beavatkozik.`;
}
// ─── ADR-053: Mode #4 Program-Awareness Context Builder ──────────────────────
/**
 * Build mode-aware context for Conductor session
 * Detects operation mode and injects appropriate guidance:
 * - 'structured_program': Load active epic, check checkpoint completion
 * - 'planning_pipeline': Queue processing logic
 * - 'manual': Wait for inbox messages
 */
function buildModeAwarenessContext() {
    try {
        const mode = (0, modeDetection_1.detectOperationMode)();
        const modeDesc = (0, modeDetection_1.getModeDescription)(mode);
        let context = `## 🔧 MODE #4 Program-Awareness (ADR-053)

**Jelenlegi mód:** \`${mode}\`
${modeDesc}

---

`;
        if (mode === 'structured_program') {
            const activeEpic = (0, epicManager_1.loadActiveEpic)();
            if (activeEpic) {
                context += `### 📋 AKTÍV EPIC

**Epic:** \`${activeEpic.id}\` — ${activeEpic.name}
**Státusz:** \`${activeEpic.status}\`
**Target Date:** ${activeEpic.target_date || 'nincs'}

#### Checkpointek:
`;
                if (activeEpic.checkpoints && activeEpic.checkpoints.length > 0) {
                    activeEpic.checkpoints.forEach((cp, idx) => {
                        const indicator = cp.status === 'done' ? '✅' : cp.status === 'pending' ? '⏳' : '⚠️';
                        context += `${idx + 1}. ${indicator} \`${cp.id}\` (${cp.status}): ${cp.name}\n`;
                        context += `   Condition: \`${cp.condition}\`\n`;
                    });
                }
                const nextCheckpoint = (0, epicManager_1.getNextCheckpoint)(activeEpic);
                if (nextCheckpoint) {
                    context += `\n#### 🎯 NEXT PENDING CHECKPOINT
\`${nextCheckpoint.id}\`: ${nextCheckpoint.name}
Condition: \`${nextCheckpoint.condition}\`

**TEENDŐ:** Ellenőrizd a checkpoint feltételét, és ha teljesül, jelezd meg.
\`\`\`
mcp__spaceos-knowledge__fetch_task
  terminal: "conductor"
  message_id: "[aktuális feladat ID]"
\`\`\`
`;
                }
                else {
                    context += `\n**✅ Összes checkpoint teljesült!**
Az epic kész a lezárásra.
`;
                }
            }
            else {
                context += `❌ **Nincs aktív epic** az EPICS.yaml-ban!\n`;
            }
            context += `
---
**Mode #4 Note:** A review system dependency eltávolítva.
Checkpoint-alapú haladás követés a feladat helyett.
Lásd: ADR-053, MSG-BACKEND-120

`;
        }
        else if (mode === 'planning_pipeline') {
            context += `### 🧠 Planning Pipeline Mód

A rendszer ideagenerálás + tervezési pipeline módban működik.

**Teendő:**
1. Az ideatezaurusz feldolgozása
2. Szövegek vitázása (A vs B)
3. Konszenzus elérése
4. Feladatok queuebe töltése

Lásd: docs/planning/ mappában a jelenlegi fázist.

---

`;
        }
        else {
            context += `### 🎯 Manuális Mód (Default)

A rendszer manuális feladatkezelésre vár.

**Teendő:**
1. Inbox üzenetek feldolgozása
2. Feladatok végrehajtása
3. DONE/BLOCKED üzenetek írása

Az automatikus pipeline szünetel.

---

`;
        }
        return context;
    }
    catch (error) {
        console.error('[SessionStarter] Failed to build mode awareness context:', error);
        return `## ⚠️ MODE DETECTION ERROR

Nem sikerült az operációs mód meghatározása. Alapértelmezett (manual) módra visszaállás.
Lásd: logs/conductor/

`;
    }
}
// All config values from centralized config
const TMUX_SOCKET = terminalsConfig.getTmuxSocket();
const TERMINALS_DIR = terminalsConfig.getTerminalsRoot();
/**
 * Extract content from an inbox message file
 * Used for both domain knowledge detection and task prompt building
 */
async function extractInboxContent(terminal, messageId) {
    const inboxPath = terminalsConfig.getInboxPath(terminal);
    try {
        const files = await fs_1.promises.readdir(inboxPath);
        for (const file of files) {
            if (!file.endsWith('.md'))
                continue;
            const filePath = path.join(inboxPath, file);
            const content = await fs_1.promises.readFile(filePath, 'utf-8');
            if (content.includes(`id: ${messageId}`)) {
                // Extract message content (after "## Message" header)
                const msgMatch = content.match(/## Message\s*\n\n([\s\S]*?)(?:\n---|\n##|$)/);
                const messageContent = msgMatch ? msgMatch[1].trim() : '';
                // Check for Telegram metadata
                const isTelegram = content.includes('from: telegram');
                const chatMatch = content.match(/telegram_chat_id:\s*(\d+)/);
                const convMatch = content.match(/telegram_conversation_id:\s*(\d+)/);
                return {
                    rawContent: content,
                    messageContent,
                    isTelegram,
                    telegramChatId: chatMatch ? chatMatch[1] : '',
                    telegramConversationId: convMatch ? convMatch[1] : '',
                };
            }
        }
    }
    catch {
        // Inbox read failed
    }
    return null;
}
// ─── Cold Start Mode Session Termination ───────────────────────────────────
/**
 * Terminate a cold start session after task completion
 * 1. Save MEMORY.md summary before killing
 * 2. Handle session end (persist to DB)
 * 3. Kill tmux session
 */
async function terminateColdSession(terminal, taskId, summary, endReason = 'done') {
    const sessionName = `spaceos-${terminal}`;
    const sessionMode = (0, terminals_1.getSessionMode)(terminal);
    // Only terminate if cold mode
    if (sessionMode !== 'cold') {
        console.log(`[SessionStarter] ${terminal} is in continuous mode, not terminating`);
        return {
            success: true,
            message: `${terminal} in continuous mode, session kept alive`,
        };
    }
    console.log(`[SessionStarter] Terminating cold session: ${sessionName}`);
    try {
        // 1. Handle session end (saves to DB + creates hot memory)
        await (0, sessionHooks_1.handleSessionEnd)({
            terminal,
            endReason,
            taskId,
            summary,
            hadCorrections: false,
        });
        console.log(`[SessionStarter] Session end handled for ${terminal}`);
        // 2. Save summary to MEMORY.md
        const memoryPath = path.join(TERMINALS_DIR, terminal, 'MEMORY.md');
        const timestamp = new Date().toISOString();
        const memoryEntry = `
## ${endReason.toUpperCase()}: ${taskId} (${timestamp})

${summary}

---
`;
        try {
            // Append to MEMORY.md
            await fs_1.promises.appendFile(memoryPath, memoryEntry, 'utf-8');
            console.log(`[SessionStarter] MEMORY.md updated for ${terminal}`);
        }
        catch (err) {
            console.warn(`[SessionStarter] Failed to update MEMORY.md for ${terminal}:`, err);
        }
        // 3. Wait a bit for any pending output
        await new Promise(resolve => setTimeout(resolve, 1000));
        // 4. Kill tmux session
        try {
            (0, child_process_1.execSync)(`tmux -S ${TMUX_SOCKET} kill-session -t ${sessionName}`, { timeout: 5000 });
            console.log(`[SessionStarter] ✓ Killed cold session ${sessionName}`);
        }
        catch {
            // Try default socket
            try {
                (0, child_process_1.execSync)(`tmux kill-session -t ${sessionName}`, { timeout: 5000 });
                console.log(`[SessionStarter] ✓ Killed cold session ${sessionName} (default socket)`);
            }
            catch {
                console.log(`[SessionStarter] Session ${sessionName} already terminated`);
            }
        }
        // 5. Send Telegram notification
        const telegramToken = process.env.TELEGRAM_TOKEN;
        const telegramChatId = process.env.TELEGRAM_CHAT_ID;
        if (telegramToken && telegramChatId) {
            const emoji = endReason === 'done' ? '✅' : '🚫';
            const message = `${emoji} *${terminal.toUpperCase()} cold session ended*\nTask: \`${taskId}\`\nReason: ${endReason}`;
            await execAsync(`curl -s -X POST "https://api.telegram.org/bot${telegramToken}/sendMessage" -d chat_id="${telegramChatId}" --data-urlencode "text=${message}" -d parse_mode="Markdown" -o /dev/null`);
        }
        return {
            success: true,
            message: `Cold session ${sessionName} terminated (${endReason})`,
        };
    }
    catch (error) {
        const msg = error instanceof Error ? error.message : String(error);
        console.error(`[SessionStarter] Failed to terminate cold session ${sessionName}:`, msg);
        return {
            success: false,
            message: `Failed to terminate ${sessionName}: ${msg}`,
        };
    }
}
/**
 * Execute a tmux command, trying configured socket first, then default socket.
 * This handles the case where sessions might be running on either socket.
 */
function tmuxExec(command, sessionName, opts = {}) {
    const timeout = opts.timeout || 5000;
    // First try configured socket
    try {
        return (0, child_process_1.execSync)(`tmux -S ${TMUX_SOCKET} ${command} -t ${sessionName}`, {
            timeout,
            encoding: 'utf-8'
        });
    }
    catch {
        // Fall through to default socket
    }
    // Fallback: try default tmux socket
    return (0, child_process_1.execSync)(`tmux ${command} -t ${sessionName}`, {
        timeout,
        encoding: 'utf-8'
    });
}
/**
 * Send keys to a session, trying both sockets
 * Uses -H 0d (hex carriage return) for Enter to avoid bracketed paste mode issues
 */
function tmuxSendKeys(sessionName, keys, literal = false) {
    const safeKeys = keys.replace(/'/g, "'\\''");
    // Special handling for Enter key - use hex code to avoid bracketed paste mode
    const isEnter = keys === 'Enter';
    const cmdSuffix = isEnter
        ? '-H 0d'
        : literal
            ? `-l '${safeKeys}'`
            : keys;
    // First try configured socket
    try {
        (0, child_process_1.execSync)(`tmux -S ${TMUX_SOCKET} send-keys -t ${sessionName} ${cmdSuffix}`, { timeout: 5000 });
        return;
    }
    catch {
        // Fall through to default socket
    }
    // Fallback: try default socket
    (0, child_process_1.execSync)(`tmux send-keys -t ${sessionName} ${cmdSuffix}`, { timeout: 5000 });
}
// Chunk size for tmux send-keys (avoid terminal buffer overflow)
const CHUNK_SIZE = 80;
// Max retry attempts for stuck prompt (Marveen pattern)
const SUBMIT_RETRY_MAX_ATTEMPTS = 4;
// Delay between retries (ms string for sleep)
const SUBMIT_RETRY_POLL_MS = '0.3';
// Delay between chunks to avoid paste detection
const CHUNK_DELAY_MS = '0.03';
// Pasted text placeholder regex (Marveen pattern)
// When several chunks coalesce, tmux bracketed-paste detector may create a stub
const PENDING_PASTE_RX = /\[Pasted text\s*#\s*\d/;
// Get allowed terminals from config
function isValidTerminal(terminal) {
    return terminalsConfig.getTerminal(terminal) !== null;
}
// Get workdir from config
function getWorkdir(terminal) {
    const sessionWorkdirs = terminalsConfig.getSessionWorkdirs();
    const sessionName = `spaceos-${terminal}`;
    return sessionWorkdirs[sessionName] || TERMINALS_DIR;
}
// Check if priority terminal (managed by watch-priority.sh)
function isPriorityTerminal(terminal) {
    return terminalsConfig.getPriorityTerminals().includes(terminal);
}
// Get model from inbox frontmatter - now uses DB query (O(1) instead of O(n) file reads)
async function getInboxModel(terminal) {
    try {
        // Query DB for UNREAD inbox messages
        const unread = (0, messageRegistry_1.getUnreadMessages)(terminal, 'inbox');
        if (unread.length > 0 && unread[0].model) {
            return unread[0].model;
        }
    }
    catch {
        // Fallback to filesystem if DB not ready
        const inboxPath = terminalsConfig.getInboxPath(terminal);
        try {
            const files = await fs_1.promises.readdir(inboxPath);
            for (const file of files.sort().reverse()) {
                if (!file.endsWith('.md'))
                    continue;
                const content = await fs_1.promises.readFile(path.join(inboxPath, file), 'utf-8');
                if (!content.includes('status: UNREAD'))
                    continue;
                const modelMatch = content.match(/^model:\s*(\w+)/m);
                if (modelMatch)
                    return modelMatch[1];
            }
        }
        catch {
            // Directory doesn't exist
        }
    }
    return 'sonnet'; // Default model
}
// Check if session is running
// Checks both configured socket AND default tmux socket for compatibility
async function isSessionRunning(sessionName) {
    // First try configured socket
    try {
        await execAsync(`tmux -S ${TMUX_SOCKET} has-session -t ${sessionName}`);
        return true;
    }
    catch {
        // Fall through to check default socket
    }
    // Fallback: try default tmux socket
    try {
        await execAsync(`tmux has-session -t ${sessionName}`);
        return true;
    }
    catch {
        return false;
    }
}
/**
 * Capture tmux pane content for state detection
 * Tries both configured and default socket
 */
function capturePane(sessionName) {
    // First try configured socket
    try {
        return (0, child_process_1.execSync)(`tmux -S ${TMUX_SOCKET} capture-pane -t ${sessionName} -p`, {
            timeout: 3000,
            encoding: 'utf-8'
        });
    }
    catch {
        // Fall through to default socket
    }
    // Fallback: try default socket
    try {
        return (0, child_process_1.execSync)(`tmux capture-pane -t ${sessionName} -p`, {
            timeout: 3000,
            encoding: 'utf-8'
        });
    }
    catch {
        return null;
    }
}
/**
 * Detect if pane has a [Pasted text #N] placeholder (Marveen pattern)
 * These placeholders don't auto-submit with Enter
 */
function detectsPastePlaceholder(pane) {
    if (!pane)
        return false;
    // Only check last 8 lines (live input box region)
    const lines = pane.split('\n').slice(-8).join('\n');
    return PENDING_PASTE_RX.test(lines);
}
/**
 * Clear any stuck text in the input buffer (Marveen-style)
 * - Escape: close any modal/menu
 * - Ctrl+U: clear the input line
 * Tries both configured and default socket
 */
function clearInputBuffer(sessionName) {
    try {
        // Send Escape to close any modal (e.g., file picker, menu)
        tmuxSendKeys(sessionName, 'Escape');
        (0, child_process_1.execSync)('sleep 0.1');
        // Send Ctrl+U to clear any parked text in input buffer
        tmuxSendKeys(sessionName, 'C-u');
        (0, child_process_1.execSync)('sleep 0.1');
    }
    catch (error) {
        // Non-fatal - continue with injection anyway
        console.warn(`[SessionStarter] Buffer clear warning for ${sessionName}:`, error);
    }
}
/**
 * Discard [Pasted text #N] placeholder with Ctrl+C (Marveen pattern)
 * Ctrl+U does NOT clear placeholders - only Ctrl+C works
 * Returns true if placeholder was cleared
 * Tries both configured and default socket
 */
function discardPlaceholderBuffer(sessionName) {
    const MAX_ATTEMPTS = 3;
    for (let i = 0; i < MAX_ATTEMPTS; i++) {
        const pane = capturePane(sessionName);
        // Stop if placeholder is gone
        if (!detectsPastePlaceholder(pane))
            return true;
        try {
            tmuxSendKeys(sessionName, 'C-c');
            (0, child_process_1.execSync)('sleep 0.45'); // Settle window
        }
        catch (err) {
            console.warn(`[SessionStarter] discardPlaceholderBuffer: Ctrl+C send failed`, err);
            return false;
        }
    }
    const finalPane = capturePane(sessionName);
    return !detectsPastePlaceholder(finalPane);
}
/**
 * Send chunks to session with proper handling (Marveen pattern)
 * - Handles `-` at chunk start (tmux interprets as flag)
 * - Adds delay between chunks to avoid paste detection
 * - Tries both configured and default socket
 */
function sendChunks(sessionName, text) {
    const oneLine = text.replace(/\r?\n/g, ' ');
    const MAX_SLIDE = 8;
    let i = 0;
    while (i < oneLine.length) {
        let end = Math.min(i + CHUNK_SIZE, oneLine.length);
        // Slide boundary past `-` chars (tmux send-keys treats leading `-` as flag)
        let slide = 0;
        while (end < oneLine.length && oneLine[end] === '-' && slide < MAX_SLIDE) {
            end++;
            slide++;
        }
        let chunk = oneLine.slice(i, end);
        // Prepend space if chunk starts with `-` (would be interpreted as tmux flag)
        if (chunk.startsWith('-'))
            chunk = ' ' + chunk;
        // Send literal chunk using dual-socket helper
        tmuxSendKeys(sessionName, chunk, true);
        i = end;
        // Small delay between chunks to avoid triggering paste detection
        if (i < oneLine.length) {
            (0, child_process_1.execSync)(`sleep ${CHUNK_DELAY_MS}`);
        }
    }
    // Wait before sending Enter to ensure all chunks are processed
    // (Marveen pattern: sleep between paste and Enter to avoid bracketed paste mode swallowing it)
    (0, child_process_1.execSync)('sleep 0.5');
    // Send Enter to submit
    tmuxSendKeys(sessionName, 'Enter');
}
/**
 * Inject a message into a running tmux session (Marveen-style)
 * Full implementation with:
 * - Pre-flight buffer clearing (Escape + Ctrl+U)
 * - Chunked sending with delay
 * - Post-send retry loop for stuck prompts
 * - Placeholder detection and clear-and-resend recovery
 */
function injectMessageToSession(sessionName, message) {
    try {
        // STEP 1: Pre-flight - clear any stuck text in input buffer
        clearInputBuffer(sessionName);
        // STEP 2: Send chunks
        sendChunks(sessionName, message);
        // STEP 3: Post-send retry loop (Marveen pattern)
        // Check if prompt got stuck and needs extra Enter or clear-and-resend
        for (let attempt = 0; attempt < SUBMIT_RETRY_MAX_ATTEMPTS; attempt++) {
            (0, child_process_1.execSync)(`sleep ${SUBMIT_RETRY_POLL_MS}`);
            const pane = capturePane(sessionName);
            // If we can't capture, give up
            if (pane === null) {
                console.warn(`[SessionStarter] Cannot capture pane for ${sessionName}, assuming OK`);
                break;
            }
            // Check for placeholder - needs clear-and-resend, not just Enter
            if (detectsPastePlaceholder(pane)) {
                console.log(`[SessionStarter] Paste placeholder detected for ${sessionName}, clearing and resending (attempt ${attempt + 1})`);
                if (!discardPlaceholderBuffer(sessionName)) {
                    console.warn(`[SessionStarter] Failed to clear placeholder for ${sessionName}`);
                }
                // Resend the message
                sendChunks(sessionName, message);
                continue;
            }
            // Check if prompt is still parked (text visible in input box after idle footer)
            // Simple heuristic: if pane ends with our message text, it's probably stuck
            const lastLines = pane.split('\n').slice(-5).join(' ');
            const msgStart = message.slice(0, Math.min(40, message.length));
            if (lastLines.includes(msgStart) && lastLines.includes('bypass permissions')) {
                // Text is parked with idle footer - send extra Enter
                console.log(`[SessionStarter] Prompt parked for ${sessionName}, sending retry Enter (attempt ${attempt + 1})`);
                tmuxSendKeys(sessionName, 'Enter');
                continue;
            }
            // If we reach here, prompt appears to have been submitted
            break;
        }
        return true;
    }
    catch (error) {
        console.error(`[SessionStarter] Failed to inject message to ${sessionName}:`, error);
        return false;
    }
}
// Start a terminal session
// ADR-046: Optional model parameter for priority sessions (watchPriority)
async function startTerminalSession(terminal, messageId, modelOverride) {
    // SECURITY: Validate terminal name against whitelist
    if (!isValidTerminal(terminal)) {
        console.error(`[SessionStarter] SECURITY: Invalid terminal name rejected: ${terminal}`);
        return {
            success: false,
            message: `Invalid terminal name: ${terminal}`,
        };
    }
    const sessionName = `spaceos-${terminal}`;
    // ─── Smart Task Assignment (2026-06-26 + ADR-049 Phase 3) ───
    // Uses extractInboxContent helper for both domain detection and prompt building
    function buildTaskAssignmentPromptFromContent(term, msgId, inbox) {
        // TELEGRAM: Direct content injection (simple, no MCP fetch needed)
        if (inbox?.isTelegram && inbox.messageContent) {
            return `[TELEGRAM ÜZENET] ${msgId}

📱 Telegram üzenet érkezett:

${inbox.messageContent}

---
Válaszoláshoz használd:
mcp__spaceos-knowledge__telegram_reply
  chat_id: ${inbox.telegramChatId}
  conversation_id: ${inbox.telegramConversationId}
  message: "A válaszod ide..."`;
        }
        // OTHER TASKS: Use MCP fetch (for complex tasks with acceptance criteria, etc.)
        return `[TASK ASSIGNED] Task ID: ${msgId}

Neked egy feladat lett kiosztva. Használd az MCP eszközöket a task kezeléséhez:

1. TASK LEKÉRÉSE:
   mcp__spaceos-knowledge__fetch_task
     terminal: "${term}"
     message_id: "${msgId}"

2. TASK FOGADÁS MEGERŐSÍTÉSE:
   mcp__spaceos-knowledge__ack_task
     terminal: "${term}"
     message_id: "${msgId}"

3. TASK BEFEJEZÉSE (amikor KÉSZ vagy):
   mcp__spaceos-knowledge__complete_task
     terminal: "${term}"
     message_id: "${msgId}"

FONTOS: Csak ezt az egy feladatot dolgozhatod fel. Más feladathoz nincs hozzáférésed.
A token az MCP regisztrációdban van, automatikusan autentikált.`;
    }
    // Priority terminals handling:
    // - If RUNNING and called from InboxWatcher → inject task assignment only
    // - If NOT RUNNING and called from watchPriority → start with cold-start context
    // The distinction is made by checking modelOverride (watchPriority passes it)
    if (isPriorityTerminal(terminal) && await isSessionRunning(sessionName)) {
        // Session running → just inject task assignment
        const inboxContent = await extractInboxContent(terminal, messageId);
        const nudgeMsg = buildTaskAssignmentPromptFromContent(terminal, messageId, inboxContent);
        console.log(`[SessionStarter] ${sessionName} (priority) running, injecting task assignment...`);
        const injected = injectMessageToSession(sessionName, nudgeMsg);
        if (injected) {
            // Mark as INJECTED to prevent re-injection loops (2026-07-01)
            await markAsInjected(terminal, messageId);
            console.log(`[SessionStarter] ✓ Injected task assignment to priority ${sessionName}`);
            return {
                success: true,
                message: `Injected task assignment to priority ${sessionName}`,
            };
        }
        return {
            success: false,
            message: `Failed to inject to running priority ${sessionName}`,
        };
    }
    // Priority terminal NOT running → continue to start it (called from watchPriority)
    // Check if already running → inject task assignment instead of skipping!
    if (await isSessionRunning(sessionName)) {
        // Build task assignment message for running session
        const inboxContent = await extractInboxContent(terminal, messageId);
        const nudgeMsg = buildTaskAssignmentPromptFromContent(terminal, messageId, inboxContent);
        console.log(`[SessionStarter] ${sessionName} already running, injecting task assignment...`);
        const injected = injectMessageToSession(sessionName, nudgeMsg);
        if (injected) {
            // Mark as INJECTED to prevent re-injection loops (2026-07-01)
            await markAsInjected(terminal, messageId);
            console.log(`[SessionStarter] ✓ Injected task assignment to ${sessionName}`);
            return {
                success: true,
                message: `Injected task assignment to running ${sessionName}`,
            };
        }
        else {
            console.error(`[SessionStarter] Failed to inject to ${sessionName}`);
            return {
                success: false,
                message: `Failed to inject to running ${sessionName}`,
            };
        }
    }
    // Get model and workdir from config
    // ADR-046: Use modelOverride if provided (for priority sessions), else detect from inbox
    const model = modelOverride || await getInboxModel(terminal);
    const workdir = getWorkdir(terminal);
    console.log(`[SessionStarter] Starting ${sessionName}: model=${model}, inbox=${messageId}`);
    try {
        // Create tmux session
        await execAsync(`tmux -S ${TMUX_SOCKET} new-session -d -s ${sessionName} -c "${workdir}"`);
        // Wait a bit for session to be ready
        await new Promise(resolve => setTimeout(resolve, 1000));
        // ADR-049 Phase 3: Extract inbox content for domain knowledge detection
        const inboxContent = await extractInboxContent(terminal, messageId);
        const taskContent = inboxContent?.rawContent || '';
        // ADR-046 Track B + ADR-049 Phase 3: Build cold start context with domain knowledge
        const startContext = await (0, sessionHooks_1.buildStartContext)({
            terminal,
            inboxMessageId: messageId,
            taskId: messageId, // Use messageId as taskId for tracking
            taskContent, // Pass task content for domain memory detection
        });
        console.log(`[SessionStarter] Cold start context: ${startContext.memoriesLoaded} memories (~${startContext.contextTokens} tokens)${startContext.domainKnowledge?.domains.length ? `, domains: [${startContext.domainKnowledge.domains.join(', ')}]` : ''}`);
        // Send claude command
        await execAsync(`tmux -S ${TMUX_SOCKET} send-keys -t ${sessionName} "claude --model ${model}" Enter`);
        // Wait for claude to start (10s needed for full initialization on cold start)
        await new Promise(resolve => setTimeout(resolve, 10000));
        // Inject context into session
        if (startContext.memoriesLoaded > 0) {
            const contextInjected = injectMessageToSession(sessionName, startContext.contextMarkdown);
            if (contextInjected) {
                console.log(`[SessionStarter] ✓ Injected ${startContext.memoriesLoaded} memories into ${sessionName}`);
            }
            else {
                console.warn(`[SessionStarter] Failed to inject context into ${sessionName}`);
            }
        }
        // Wait a bit for context to be processed
        await new Promise(resolve => setTimeout(resolve, 1000));
        // ADR-053: Inject Mode #4 Program-Awareness context (especially for Conductor)
        // This tells the terminal what mode it should operate in and provides epic/checkpoint context
        if (terminal === 'conductor') {
            const modeContext = buildModeAwarenessContext();
            const modeInjected = injectMessageToSession(sessionName, modeContext);
            if (modeInjected) {
                console.log(`[SessionStarter] ✓ Injected Mode #4 awareness context to ${sessionName}`);
            }
            else {
                console.warn(`[SessionStarter] Failed to inject mode awareness context to ${sessionName}`);
            }
            // Wait for mode context to be processed
            await new Promise(resolve => setTimeout(resolve, 500));
        }
        // Inject task assignment prompt (Smart 2026-06-26 + ADR-049 - reuses extracted content)
        const taskAssignment = buildTaskAssignmentPromptFromContent(terminal, messageId, inboxContent);
        const taskInjected = injectMessageToSession(sessionName, taskAssignment);
        if (taskInjected) {
            // Mark as INJECTED to prevent re-injection loops (2026-07-01)
            await markAsInjected(terminal, messageId);
            console.log(`[SessionStarter] ✓ Injected task assignment ${messageId} to ${sessionName}`);
        }
        else {
            console.warn(`[SessionStarter] Failed to inject task assignment to ${sessionName}`);
        }
        console.log(`[SessionStarter] ✓ ${sessionName} started`);
        // Send Telegram notification
        const telegramToken = process.env.TELEGRAM_TOKEN;
        const telegramChatId = process.env.TELEGRAM_CHAT_ID;
        if (telegramToken && telegramChatId) {
            const message = `🚀 *${terminal.toUpperCase()} wake-on-inbox*\nModell: \`${model}\`\nInbox: \`${messageId}\``;
            await execAsync(`curl -s -X POST "https://api.telegram.org/bot${telegramToken}/sendMessage" -d chat_id="${telegramChatId}" --data-urlencode "text=${message}" -d parse_mode="Markdown" -o /dev/null`);
        }
        return {
            success: true,
            message: `Started ${sessionName} with model ${model}`,
        };
    }
    catch (error) {
        const msg = error instanceof Error ? error.message : String(error);
        console.error(`[SessionStarter] Failed to start ${sessionName}:`, msg);
        return {
            success: false,
            message: `Failed to start ${sessionName}: ${msg}`,
        };
    }
}
// ─── Work Session Spawning (ADR-049 Phase 2) ────────────────────────────────
/**
 * Start a work session from a chat session
 * This is called by the chat session (Haiku) to hand off complex tasks to Sonnet
 *
 * @param terminal - Terminal name (e.g., 'librarian', 'backend')
 * @param task - Task description to pass to the work session
 * @param model - Model to use (default: sonnet)
 */
async function startWorkSession(terminal, task, model = 'sonnet') {
    // SECURITY: Validate terminal name
    if (!isValidTerminal(terminal)) {
        console.error(`[SessionStarter] SECURITY: Invalid terminal name rejected: ${terminal}`);
        return {
            success: false,
            message: `Invalid terminal name: ${terminal}`,
        };
    }
    const sessionName = `spaceos-${terminal}`;
    const workdir = getWorkdir(terminal);
    // Check if work session is already running
    if (await isSessionRunning(sessionName)) {
        // Inject the task into the running session
        console.log(`[SessionStarter] Work session ${sessionName} already running, injecting task...`);
        const taskPrompt = `[WORK SESSION TASK - delegált a chat session-től]

A chat session átadta neked ezt a feladatot:

${task}

---
Dolgozd fel a feladatot, majd ha kész vagy, jelezd vissza.`;
        const injected = injectMessageToSession(sessionName, taskPrompt);
        if (injected) {
            return {
                success: true,
                message: `Task injected into running work session ${sessionName}`,
                sessionName,
            };
        }
        else {
            return {
                success: false,
                message: `Failed to inject task into ${sessionName}`,
            };
        }
    }
    // Start new work session
    console.log(`[SessionStarter] Starting work session: ${sessionName} (model=${model})`);
    try {
        // Create tmux session
        await execAsync(`tmux -S ${TMUX_SOCKET} new-session -d -s ${sessionName} -c "${workdir}"`);
        // Wait for session to be ready
        await new Promise(resolve => setTimeout(resolve, 1000));
        // Send claude command with specified model
        await execAsync(`tmux -S ${TMUX_SOCKET} send-keys -t ${sessionName} "claude --model ${model}" Enter`);
        // Wait for claude to initialize
        await new Promise(resolve => setTimeout(resolve, 10000));
        // Build and inject task prompt
        const taskPrompt = `[WORK SESSION] Te a ${terminal.toUpperCase()} terminál WORK session-je vagy.

A chat session (spaceos-${terminal}-chat) átadta neked ezt a feladatot:

${task}

---
Olvasd be: CLAUDE.md, MEMORY.md

Ez egy work session (${model}) - teljes fejlesztési képességekkel.
A chat session Haiku, te pedig ${model.toUpperCase()} vagy - összetettebb feladatokhoz.

Dolgozd fel a feladatot. Ha kész vagy, írd: "DONE: [összefoglaló]"`;
        // Inject task
        const taskInjected = injectMessageToSession(sessionName, taskPrompt);
        if (!taskInjected) {
            console.warn(`[SessionStarter] Failed to inject task to work session ${sessionName}`);
        }
        console.log(`[SessionStarter] ✓ Work session ${sessionName} started`);
        // Telegram notification
        const telegramToken = process.env.TELEGRAM_TOKEN;
        const telegramChatId = process.env.TELEGRAM_CHAT_ID;
        if (telegramToken && telegramChatId) {
            const message = `🔧 *${terminal.toUpperCase()} work session started*\nModell: \`${model}\`\nFeladat: \`${task.slice(0, 100)}...\``;
            await execAsync(`curl -s -X POST "https://api.telegram.org/bot${telegramToken}/sendMessage" -d chat_id="${telegramChatId}" --data-urlencode "text=${message}" -d parse_mode="Markdown" -o /dev/null`);
        }
        return {
            success: true,
            message: `Work session ${sessionName} started with model ${model}`,
            sessionName,
        };
    }
    catch (error) {
        const msg = error instanceof Error ? error.message : String(error);
        console.error(`[SessionStarter] Failed to start work session ${sessionName}:`, msg);
        return {
            success: false,
            message: `Failed to start work session: ${msg}`,
        };
    }
}
// ─── ADR-049 Phase 3: Parallel Workers ─────────────────────────────────────
const workerRegistry_1 = require("./pipeline/workerRegistry");
const costLimiter_1 = require("./pipeline/costLimiter");
const workerRegistry_2 = require("./pipeline/workerRegistry");
/**
 * Generate unique worker ID for parallel sessions
 */
function generateWorkerId(terminal) {
    const existing = (0, workerRegistry_1.getActiveWorkerIds)(terminal);
    let num = 1;
    while (existing.includes(`work-${String(num).padStart(3, '0')}`)) {
        num++;
    }
    return `work-${String(num).padStart(3, '0')}`;
}
/**
 * Start parallel work session
 * ADR-049 Phase 3: Support multiple concurrent work sessions per terminal
 */
async function startParallelWorkSession(config) {
    try {
        // Check parallel limit
        const spawnCheck = (0, costLimiter_1.canSpawnWorker)(config.terminal, config.model);
        if (!spawnCheck.allowed) {
            return {
                success: false,
                message: `Cannot spawn worker: ${spawnCheck.reason}`,
            };
        }
        // Check dependencies
        if (config.depends_on?.length) {
            const unfinished = await (0, workerRegistry_2.checkDependencies)(config.depends_on);
            if (unfinished.length > 0) {
                // Queue for later
                const workerId = generateWorkerId(config.terminal);
                (0, workerRegistry_1.queueWorker)(workerId, config);
                return {
                    success: true,
                    workerId,
                    message: `Worker ${workerId} queued, waiting for: ${unfinished.join(', ')}`,
                };
            }
        }
        // Generate worker ID
        const workerId = generateWorkerId(config.terminal);
        const sessionName = `spaceos-${config.terminal}-${workerId}`;
        // Create tmux session
        const workdir = path.join(TERMINALS_DIR, config.terminal);
        (0, child_process_1.execSync)(`tmux -S ${TMUX_SOCKET} new-session -d -s ${sessionName} -c ${workdir}`, { timeout: 5000 });
        // Register worker
        (0, workerRegistry_1.registerWorker)(workerId, config, sessionName);
        console.log(`[SessionStarter] ✓ Parallel worker ${workerId} started (${config.model})`);
        return {
            success: true,
            workerId,
            message: `Parallel worker ${workerId} started`,
        };
    }
    catch (error) {
        const msg = error instanceof Error ? error.message : String(error);
        console.error('[SessionStarter] Failed to start parallel worker:', msg);
        return {
            success: false,
            message: `Failed to start parallel worker: ${msg}`,
        };
    }
}
/**
 * Spawn raw workers for quick prototyping
 * ADR-049 Phase 3: Minimal context workers for N-to-1 selection
 */
async function spawnRawWorkers(config) {
    try {
        const workerIds = [];
        for (let i = 0; i < config.count; i++) {
            const workerId = `raw-${String(i + 1).padStart(3, '0')}`;
            const sessionName = `spaceos-${config.terminal}-${workerId}`;
            const workdir = path.join(TERMINALS_DIR, config.terminal);
            // Create session
            (0, child_process_1.execSync)(`tmux -S ${TMUX_SOCKET} new-session -d -s ${sessionName} -c ${workdir}`, { timeout: 5000 });
            workerIds.push(workerId);
            console.log(`[SessionStarter] ✓ Raw worker ${workerId} spawned (${config.model})`);
        }
        return {
            success: true,
            workerIds,
            message: `Spawned ${workerIds.length} raw workers`,
        };
    }
    catch (error) {
        const msg = error instanceof Error ? error.message : String(error);
        console.error('[SessionStarter] Failed to spawn raw workers:', msg);
        return {
            success: false,
            message: `Failed to spawn raw workers: ${msg}`,
        };
    }
}
/**
 * Collect results from raw workers (MSG-BACKEND-080 - ADR-049 Phase 3)
 * Checks each worker for [RAW-DONE] marker and collects output
 */
async function collectRawResults(terminal, workerIds) {
    const results = [];
    for (const workerId of workerIds) {
        const sessionName = `spaceos-${terminal}-${workerId}`;
        try {
            const output = capturePane(sessionName);
            if (!output) {
                results.push({
                    workerId,
                    output: '',
                    status: 'failed',
                });
                continue;
            }
            if (output.includes('[RAW-DONE]')) {
                results.push({
                    workerId,
                    output: output.replace(/\[RAW-DONE\]/g, '').trim(),
                    status: 'done',
                });
                // Kill completed worker session
                try {
                    (0, child_process_1.execSync)(`tmux -S ${TMUX_SOCKET} kill-session -t ${sessionName}`, {
                        timeout: 3000,
                    });
                    console.log(`[SessionStarter] ✓ Killed completed raw worker ${sessionName}`);
                }
                catch (err) {
                    // Try default socket
                    try {
                        (0, child_process_1.execSync)(`tmux kill-session -t ${sessionName}`, { timeout: 3000 });
                    }
                    catch {
                        console.warn(`[SessionStarter] Failed to kill session ${sessionName}`);
                    }
                }
            }
            else {
                results.push({
                    workerId,
                    output,
                    status: 'running',
                });
            }
        }
        catch (error) {
            const msg = error instanceof Error ? error.message : String(error);
            console.error(`[SessionStarter] Failed to collect result from ${workerId}:`, msg);
            results.push({
                workerId,
                output: '',
                status: 'failed',
            });
        }
    }
    return results;
}
