// Session Starter - Starts tmux Claude sessions for terminals
// Called directly by InboxWatcher when new UNREAD message is detected

import { exec } from 'child_process';
import { promisify } from 'util';
import { promises as fs } from 'fs';
import * as path from 'path';

const execAsync = promisify(exec);

const TMUX_SOCKET = '/tmp/spaceos.tmux';
const SPACEOS_ROOT = process.env.SPACEOS_ROOT || '/opt/spaceos';

// ALLOWED terminals whitelist - prevents path traversal and command injection
const ALLOWED_TERMINALS = [
  'fe', 'kernel', 'identity', 'orchestrator', 'joinery', 'cutting',
  'infra', 'e2e', 'nexus', 'architect', 'librarian', 'conductor', 'root',
  'orch', 'abstractions', 'inventory', 'procurement', 'sales'
] as const;

type TerminalName = typeof ALLOWED_TERMINALS[number];

// Validate terminal name - SECURITY: prevents path traversal and injection
function isValidTerminal(terminal: string): terminal is TerminalName {
  return ALLOWED_TERMINALS.includes(terminal as TerminalName);
}

// Terminal → Working directory mapping
const WORKDIRS: Record<string, string> = {
  fe: `${SPACEOS_ROOT}/spaceos-doorstar-portal`,
  kernel: `${SPACEOS_ROOT}/SpaceOS.Kernel`,
  identity: `${SPACEOS_ROOT}/SpaceOS.Kernel`,
  orchestrator: `${SPACEOS_ROOT}/spaceos-orchestrator`,
  joinery: `${SPACEOS_ROOT}/spaceos-modules-joinery`,
  cutting: `${SPACEOS_ROOT}/spaceos-modules-cutting`,
  infra: `${SPACEOS_ROOT}/infra`,
  e2e: `${SPACEOS_ROOT}/e2e`,
  nexus: `${SPACEOS_ROOT}/spaceos-nexus`,
  architect: SPACEOS_ROOT,
  librarian: SPACEOS_ROOT,
  conductor: SPACEOS_ROOT,
  root: SPACEOS_ROOT,
};

// Priority terminals - don't auto-start these
const PRIORITY_TERMINALS = ['root', 'conductor'];

// Get model from inbox frontmatter
async function getInboxModel(terminal: string): Promise<string> {
  const inboxPath = path.join(SPACEOS_ROOT, 'docs/mailbox', terminal, 'inbox');

  try {
    const files = await fs.readdir(inboxPath);
    for (const file of files.sort().reverse()) {
      if (!file.endsWith('.md')) continue;

      const content = await fs.readFile(path.join(inboxPath, file), 'utf-8');

      // Check if UNREAD
      if (!content.includes('status: UNREAD')) continue;

      // Extract model from frontmatter
      const modelMatch = content.match(/^model:\s*(\w+)/m);
      if (modelMatch) {
        return modelMatch[1];
      }
    }
  } catch {
    // Directory doesn't exist or read error
  }

  return 'sonnet'; // Default model
}

// Check if session is running
async function isSessionRunning(sessionName: string): Promise<boolean> {
  try {
    await execAsync(`tmux -S ${TMUX_SOCKET} has-session -t ${sessionName}`);
    return true;
  } catch {
    return false;
  }
}

// Start a terminal session
export async function startTerminalSession(
  terminal: string,
  messageId: string
): Promise<{ success: boolean; message: string }> {
  // SECURITY: Validate terminal name against whitelist
  if (!isValidTerminal(terminal)) {
    console.error(`[SessionStarter] SECURITY: Invalid terminal name rejected: ${terminal}`);
    return {
      success: false,
      message: `Invalid terminal name: ${terminal}`,
    };
  }

  const sessionName = `spaceos-${terminal}`;

  // Skip priority terminals - they're managed by watch-priority.sh
  if (PRIORITY_TERMINALS.includes(terminal)) {
    return {
      success: false,
      message: `${terminal} is a priority terminal, managed by watch-priority.sh`,
    };
  }

  // Check if already running
  if (await isSessionRunning(sessionName)) {
    console.log(`[SessionStarter] ${sessionName} already running, skip`);
    return {
      success: false,
      message: `${sessionName} is already running`,
    };
  }

  // Get model and workdir
  const model = await getInboxModel(terminal);
  const workdir = WORKDIRS[terminal] || SPACEOS_ROOT;

  console.log(`[SessionStarter] Starting ${sessionName}: model=${model}, inbox=${messageId}`);

  try {
    // Create tmux session
    await execAsync(`tmux -S ${TMUX_SOCKET} new-session -d -s ${sessionName} -c "${workdir}"`);

    // Wait a bit for session to be ready
    await new Promise(resolve => setTimeout(resolve, 1000));

    // Send claude command
    await execAsync(`tmux -S ${TMUX_SOCKET} send-keys -t ${sessionName} "claude --model ${model}" Enter`);

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
  } catch (error) {
    const msg = error instanceof Error ? error.message : String(error);
    console.error(`[SessionStarter] Failed to start ${sessionName}:`, msg);
    return {
      success: false,
      message: `Failed to start ${sessionName}: ${msg}`,
    };
  }
}
