"use strict";
// watchInbox.ts - TypeScript equivalent of watch-inbox.sh
// Handles inbox nudge for running sessions and auto-starts sessions with UNREAD inbox
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
exports.runWatchInbox = runWatchInbox;
const fs_1 = require("fs");
const path = __importStar(require("path"));
const common_1 = require("./common");
// ── Find oldest UNREAD inbox ────────────────────────────────────────────────
async function findOldestUnread(terminal) {
    const inboxPath = (0, common_1.getInboxPath)(terminal);
    try {
        const files = await fs_1.promises.readdir(inboxPath);
        let oldestUnread = null;
        for (const file of files.sort()) {
            if (!file.endsWith('.md'))
                continue;
            const filePath = path.join(inboxPath, file);
            const content = await fs_1.promises.readFile(filePath, 'utf-8');
            if (!content.includes('status: UNREAD'))
                continue;
            const stat = await fs_1.promises.stat(filePath);
            // Extract files: from frontmatter
            const filesMatch = content.match(/files:\s*\n((?:\s*-\s*.+\n?)+)/);
            const filesList = [];
            if (filesMatch) {
                const lines = filesMatch[1].split('\n');
                for (const line of lines) {
                    const match = line.match(/^\s*-\s*(.+)/);
                    if (match)
                        filesList.push(match[1].trim());
                }
            }
            if (!oldestUnread || stat.mtime < oldestUnread.mtime) {
                oldestUnread = { path: filePath, mtime: stat.mtime, files: filesList };
            }
        }
        if (!oldestUnread)
            return null;
        const now = Date.now();
        const age = Math.floor((now - oldestUnread.mtime.getTime()) / 1000);
        return {
            path: oldestUnread.path,
            age,
            files: oldestUnread.files
        };
    }
    catch {
        return null;
    }
}
// ── Nudge running session ───────────────────────────────────────────────────
async function nudgeSession(sessionName, terminal, unread) {
    const nudgeKey = `${sessionName}_inbox_nudge`;
    const lastNudge = parseInt(await (0, common_1.getState)(nudgeKey) || '0', 10);
    const now = Math.floor(Date.now() / 1000);
    // Only nudge every 5 minutes
    if (now - lastNudge < 300)
        return false;
    // Build nudge message
    let nudgeMsg = `Te a ${terminal.toUpperCase()} terminál vagy. Olvasd be: MEMORY.md — Inbox: ${path.basename(unread.path)}`;
    if (unread.files.length > 0) {
        nudgeMsg += ` Fájlok: ${unread.files.join(' ').substring(0, 500)}`;
    }
    await (0, common_1.sendKeys)(sessionName, nudgeMsg);
    await new Promise(r => setTimeout(r, 500));
    await (0, common_1.sendEnter)(sessionName);
    await new Promise(r => setTimeout(r, 1000));
    await (0, common_1.sendEnter)(sessionName);
    await (0, common_1.setState)(nudgeKey, String(now));
    await (0, common_1.log)(`[WatchInbox] Nudge: ${sessionName} → ${path.basename(unread.path)}`);
    return true;
}
// ── Auto-start session ──────────────────────────────────────────────────────
async function autoStartSession(sessionName, terminal, unread) {
    const startKey = `${sessionName}_autostart`;
    const lastStart = parseInt(await (0, common_1.getState)(startKey) || '0', 10);
    const now = Math.floor(Date.now() / 1000);
    // Only attempt every 30 minutes
    if (now - lastStart < 1800)
        return false;
    const wantedModel = await (0, common_1.getInboxModel)(terminal);
    const workdir = common_1.SESSION_WORKDIR[sessionName] || common_1.SPACEOS_ROOT;
    // Create session
    await (0, common_1.newSession)(sessionName, workdir);
    await new Promise(r => setTimeout(r, 1000));
    // Start claude with correct model
    await (0, common_1.sendKeys)(sessionName, `claude --model ${wantedModel}`);
    await new Promise(r => setTimeout(r, 500));
    await (0, common_1.sendEnter)(sessionName);
    await (0, common_1.setState)(startKey, String(now));
    await (0, common_1.log)(`[WatchInbox] Auto-started: ${sessionName} (model: ${wantedModel}, inbox: ${path.basename(unread.path)})`);
    await (0, common_1.telegram)(`🚀 *${terminal.toUpperCase()} auto-indítva*\nModell: \`${wantedModel}\`\nInbox: \`${path.basename(unread.path)}\``);
    return true;
}
// ── Main function ───────────────────────────────────────────────────────────
async function runWatchInbox() {
    const result = {
        nudged: [],
        autoStarted: [],
        skipped: []
    };
    for (const [sessionName, terminal] of Object.entries(common_1.SESSIONS)) {
        const unread = await findOldestUnread(terminal);
        if (!unread) {
            result.skipped.push(sessionName);
            continue;
        }
        const sessionRunning = await (0, common_1.hasSession)(sessionName);
        const isPriority = (0, common_1.isPrioritySession)(sessionName);
        if (sessionRunning) {
            // A) Session running, inbox 3+ minutes old → nudge (both priority and non-priority)
            if (unread.age > 180) {
                const nudged = await nudgeSession(sessionName, terminal, unread);
                if (nudged) {
                    result.nudged.push(sessionName);
                }
            }
        }
        else {
            // B) Session not running
            if (isPriority) {
                // Priority sessions are handled by watchPriority - skip auto-start
                // but still log that we detected UNREAD inbox
                await (0, common_1.log)(`[WatchInbox] Priority session ${sessionName} has UNREAD inbox but not running (watchPriority will handle)`);
                result.skipped.push(sessionName);
            }
            else {
                // Non-priority: inbox 2+ minutes old → auto-start
                if (unread.age >= 120) {
                    const started = await autoStartSession(sessionName, terminal, unread);
                    if (started) {
                        result.autoStarted.push(sessionName);
                    }
                }
            }
        }
    }
    return result;
}
// ── Standalone execution ────────────────────────────────────────────────────
if (require.main === module) {
    runWatchInbox().then(result => {
        console.log('WatchInbox result:', JSON.stringify(result, null, 2));
    });
}
