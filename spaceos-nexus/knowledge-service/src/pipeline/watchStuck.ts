// Watch Stuck - TypeScript equivalent of watch-stuck.sh
// Detects stuck sessions and sends nudge

import { promises as fs } from 'fs';
import * as path from 'path';
import {
  SPACEOS_ROOT,
  SESSIONS,
  listSessions,
  hasSession,
  capturePane,
  sendKeys,
  sendEnter,
  getState,
  setState,
  getInboxModel,
  telegram,
  log,
} from './common';

const STUCK_COOLDOWN = 300; // 5 minutes between nudges

interface StuckResult {
  session: string;
  reason: string;
}

async function getUnreadInboxFile(terminal: string): Promise<string | null> {
  const inboxPath = path.join(SPACEOS_ROOT, 'docs/mailbox', terminal, 'inbox');

  try {
    const files = await fs.readdir(inboxPath);
    for (const file of files.sort()) {
      if (!file.endsWith('.md')) continue;
      const content = await fs.readFile(path.join(inboxPath, file), 'utf-8');
      if (content.includes('status: UNREAD')) {
        return path.join(inboxPath, file);
      }
    }
  } catch {
    // Directory doesn't exist
  }

  return null;
}

async function getFilesFromInbox(inboxFile: string): Promise<string> {
  try {
    const content = await fs.readFile(inboxFile, 'utf-8');
    const lines = content.split('\n');
    let inFrontmatter = false;
    const files: string[] = [];

    for (const line of lines) {
      if (line === '---') {
        inFrontmatter = !inFrontmatter;
        continue;
      }
      if (inFrontmatter && line.match(/^\s*-\s+/)) {
        files.push(line.replace(/^\s*-\s+/, '').trim());
      }
    }

    return files.slice(0, 5).join(' ');
  } catch {
    return '';
  }
}

export async function watchStuck(): Promise<{ processed: number; nudged: StuckResult[] }> {
  const now = Math.floor(Date.now() / 1000);
  let processed = 0;
  const nudged: StuckResult[] = [];

  const sessions = await listSessions();

  for (const sessionName of sessions) {
    const terminal = SESSIONS[sessionName];
    if (!terminal) continue;

    // Skip root - doesn't get automatic triggers
    if (sessionName === 'spaceos-root') continue;

    if (!(await hasSession(sessionName))) continue;

    processed++;

    const paneOutput = await capturePane(sessionName, 10);

    // Model selector handling
    const modelSelectorPattern = /claude opus|claude sonnet|claude haiku|Choose a model|Select model|which model|Default model/i;
    if (modelSelectorPattern.test(paneOutput)) {
      const wantedModel = await getInboxModel(terminal);
      await sendKeys(sessionName, wantedModel);
      await sendEnter(sessionName);
      await log(`Model választó kezelve: ${sessionName} → ${wantedModel}`);
      await telegram(`🤖 *${terminal.toUpperCase()} model választó* — \`${wantedModel}\` kiválasztva`);
      nudged.push({ session: sessionName, reason: 'model-selector' });
      continue;
    }

    // Stuck state detection
    let stuck = false;
    let stuckReason = '';

    if (paneOutput.includes('Press up to edit queued messages')) {
      stuck = true;
      stuckReason = 'queued-messages prompt';
    } else if (/Choose a model|Select model|which model/i.test(paneOutput)) {
      stuck = true;
      stuckReason = 'model-selector';
    }
    // NOTE: "shift+tab to cycle" and empty prompt are NOT stuck states!
    // That's normal input waiting. watch-inbox.sh handles nudging if there's UNREAD.

    if (!stuck) continue;

    // Check cooldown
    const stuckKey = `${sessionName}_stuck_sent`;
    const lastSent = await getState(stuckKey);
    const elapsed = lastSent ? now - parseInt(lastSent, 10) : STUCK_COOLDOWN + 1;

    if (elapsed <= STUCK_COOLDOWN) continue;

    // Get inbox info
    const unreadInbox = await getUnreadInboxFile(terminal);
    const filesList = unreadInbox ? await getFilesFromInbox(unreadInbox) : '';

    // Build and send stuck message
    let stuckMsg = `Folytasd a munkát.`;
    if (unreadInbox) {
      stuckMsg += ` Inbox: ${path.basename(unreadInbox)}`;
    }
    if (filesList) {
      stuckMsg += ` Fájlok: ${filesList}`;
    }

    await sendKeys(sessionName, stuckMsg);
    await new Promise(r => setTimeout(r, 500));
    await sendEnter(sessionName);
    await new Promise(r => setTimeout(r, 1000));
    await sendEnter(sessionName);

    await setState(stuckKey, String(now));
    await log(`Escape+Enter küldve: ${sessionName} (${stuckReason})`);
    await telegram(`🔧 *${terminal.toUpperCase()} beakadt* (${stuckReason}) — Escape+Enter elküldve`);

    nudged.push({ session: sessionName, reason: stuckReason });
  }

  return { processed, nudged };
}

// Run standalone
if (require.main === module) {
  watchStuck().then(result => {
    console.log(`[watchStuck] Processed: ${result.processed}, Nudged: ${result.nudged.length}`);
    result.nudged.forEach(n => console.log(`  - ${n.session}: ${n.reason}`));
  });
}
