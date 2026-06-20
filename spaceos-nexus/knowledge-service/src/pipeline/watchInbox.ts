// watchInbox.ts - TypeScript equivalent of watch-inbox.sh
// Handles inbox nudge for running sessions and auto-starts sessions with UNREAD inbox

import { promises as fs } from 'fs';
import * as path from 'path';
import {
  SPACEOS_ROOT,
  SESSIONS,
  SESSION_WORKDIR,
  hasSession,
  sendKeys,
  sendEnter,
  newSession,
  getState,
  setState,
  getInboxModel,
  log,
  telegram,
  isPrioritySession
} from './common';

// ── Types ───────────────────────────────────────────────────────────────────

interface WatchInboxResult {
  nudged: string[];
  autoStarted: string[];
  skipped: string[];
}

interface UnreadInfo {
  path: string;
  age: number;
  files: string[];
}

// ── Find oldest UNREAD inbox ────────────────────────────────────────────────

async function findOldestUnread(terminal: string): Promise<UnreadInfo | null> {
  const inboxPath = path.join(SPACEOS_ROOT, 'docs/mailbox', terminal, 'inbox');

  try {
    const files = await fs.readdir(inboxPath);
    let oldestUnread: { path: string; mtime: Date; files: string[] } | null = null;

    for (const file of files.sort()) {
      if (!file.endsWith('.md')) continue;

      const filePath = path.join(inboxPath, file);
      const content = await fs.readFile(filePath, 'utf-8');

      if (!content.includes('status: UNREAD')) continue;

      const stat = await fs.stat(filePath);

      // Extract files: from frontmatter
      const filesMatch = content.match(/files:\s*\n((?:\s*-\s*.+\n?)+)/);
      const filesList: string[] = [];
      if (filesMatch) {
        const lines = filesMatch[1].split('\n');
        for (const line of lines) {
          const match = line.match(/^\s*-\s*(.+)/);
          if (match) filesList.push(match[1].trim());
        }
      }

      if (!oldestUnread || stat.mtime < oldestUnread.mtime) {
        oldestUnread = { path: filePath, mtime: stat.mtime, files: filesList };
      }
    }

    if (!oldestUnread) return null;

    const now = Date.now();
    const age = Math.floor((now - oldestUnread.mtime.getTime()) / 1000);

    return {
      path: oldestUnread.path,
      age,
      files: oldestUnread.files
    };
  } catch {
    return null;
  }
}

// ── Nudge running session ───────────────────────────────────────────────────

async function nudgeSession(
  sessionName: string,
  terminal: string,
  unread: UnreadInfo
): Promise<boolean> {
  const nudgeKey = `${sessionName}_inbox_nudge`;
  const lastNudge = parseInt(await getState(nudgeKey) || '0', 10);
  const now = Math.floor(Date.now() / 1000);

  // Only nudge every 5 minutes
  if (now - lastNudge < 300) return false;

  // Build nudge message
  let nudgeMsg = `Te a ${terminal.toUpperCase()} terminál vagy. Olvasd be: MEMORY.md — Inbox: ${path.basename(unread.path)}`;
  if (unread.files.length > 0) {
    nudgeMsg += ` Fájlok: ${unread.files.join(' ').substring(0, 500)}`;
  }

  await sendKeys(sessionName, nudgeMsg);
  await new Promise(r => setTimeout(r, 500));
  await sendEnter(sessionName);
  await new Promise(r => setTimeout(r, 1000));
  await sendEnter(sessionName);

  await setState(nudgeKey, String(now));
  await log(`[WatchInbox] Nudge: ${sessionName} → ${path.basename(unread.path)}`);

  return true;
}

// ── Auto-start session ──────────────────────────────────────────────────────

async function autoStartSession(
  sessionName: string,
  terminal: string,
  unread: UnreadInfo
): Promise<boolean> {
  const startKey = `${sessionName}_autostart`;
  const lastStart = parseInt(await getState(startKey) || '0', 10);
  const now = Math.floor(Date.now() / 1000);

  // Only attempt every 30 minutes
  if (now - lastStart < 1800) return false;

  const wantedModel = await getInboxModel(terminal);
  const workdir = SESSION_WORKDIR[sessionName] || SPACEOS_ROOT;

  // Create session
  await newSession(sessionName, workdir);
  await new Promise(r => setTimeout(r, 1000));

  // Start claude with correct model
  await sendKeys(sessionName, `claude --model ${wantedModel}`);
  await new Promise(r => setTimeout(r, 500));
  await sendEnter(sessionName);

  await setState(startKey, String(now));
  await log(`[WatchInbox] Auto-started: ${sessionName} (model: ${wantedModel}, inbox: ${path.basename(unread.path)})`);

  await telegram(`🚀 *${terminal.toUpperCase()} auto-indítva*\nModell: \`${wantedModel}\`\nInbox: \`${path.basename(unread.path)}\``);

  return true;
}

// ── Main function ───────────────────────────────────────────────────────────

export async function runWatchInbox(): Promise<WatchInboxResult> {
  const result: WatchInboxResult = {
    nudged: [],
    autoStarted: [],
    skipped: []
  };

  for (const [sessionName, terminal] of Object.entries(SESSIONS)) {
    // Skip priority sessions - handled by watchPriority
    if (isPrioritySession(sessionName)) {
      result.skipped.push(sessionName);
      continue;
    }

    const unread = await findOldestUnread(terminal);
    if (!unread) {
      result.skipped.push(sessionName);
      continue;
    }

    const sessionRunning = await hasSession(sessionName);

    if (sessionRunning) {
      // A) Session running, inbox 3+ minutes old → nudge
      if (unread.age > 180) {
        const nudged = await nudgeSession(sessionName, terminal, unread);
        if (nudged) {
          result.nudged.push(sessionName);
        }
      }
    } else {
      // B) Session not running, inbox 2+ minutes old → auto-start
      if (unread.age >= 120) {
        const started = await autoStartSession(sessionName, terminal, unread);
        if (started) {
          result.autoStarted.push(sessionName);
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
