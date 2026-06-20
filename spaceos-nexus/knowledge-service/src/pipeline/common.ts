// Pipeline Common Utilities - TypeScript equivalent of common.sh
import { exec } from 'child_process';
import { promisify } from 'util';
import { promises as fs } from 'fs';
import * as path from 'path';

const execAsync = promisify(exec);

// Environment
export const SPACEOS_ROOT = process.env.SPACEOS_ROOT || '/opt/spaceos';
export const TMUX_SOCKET = '/tmp/spaceos.tmux';
export const LOG_DIR = path.join(SPACEOS_ROOT, 'logs/dispatcher');
export const STATE_FILE = path.join(SPACEOS_ROOT, 'scripts/.nightwatch-state');

// Session → Terminal mappings
export const SESSIONS: Record<string, string> = {
  'spaceos-fe': 'fe',
  'spaceos-fe-b': 'fe2',
  'spaceos-architect': 'architect',
  'spaceos-root': 'root',
  'spaceos-conductor': 'conductor',
  'spaceos-kernel': 'kernel',
  'spaceos-identity': 'identity',
  'spaceos-orchestrator': 'orchestrator',
  'spaceos-joinery': 'joinery',
  'spaceos-cutting': 'cutting',
  'spaceos-infra': 'infra',
  'spaceos-e2e': 'e2e',
  'spaceos-librarian': 'librarian',
  'spaceos-nexus': 'nexus',
};

// Priority sessions - these always run
export const PRIORITY_SESSIONS = ['spaceos-conductor'];

// Session → Working directory
export const SESSION_WORKDIR: Record<string, string> = {
  'spaceos-fe': `${SPACEOS_ROOT}/spaceos-doorstar-portal`,
  'spaceos-fe-b': `${SPACEOS_ROOT}/spaceos-doorstar-portal`,
  'spaceos-architect': SPACEOS_ROOT,
  'spaceos-root': SPACEOS_ROOT,
  'spaceos-conductor': SPACEOS_ROOT,
  'spaceos-kernel': `${SPACEOS_ROOT}/SpaceOS.Kernel`,
  'spaceos-identity': `${SPACEOS_ROOT}/SpaceOS.Kernel`,
  'spaceos-orchestrator': `${SPACEOS_ROOT}/spaceos-orchestrator`,
  'spaceos-joinery': `${SPACEOS_ROOT}/spaceos-modules-joinery`,
  'spaceos-cutting': `${SPACEOS_ROOT}/spaceos-modules-cutting`,
  'spaceos-infra': `${SPACEOS_ROOT}/infra`,
  'spaceos-e2e': `${SPACEOS_ROOT}/e2e`,
  'spaceos-librarian': SPACEOS_ROOT,
  'spaceos-nexus': `${SPACEOS_ROOT}/spaceos-nexus`,
};

// Task-only terminals (not priority)
export const TASK_ONLY_TERMINALS = [
  'fe', 'fe2', 'kernel', 'identity', 'joinery', 'cutting',
  'infra', 'e2e', 'nexus', 'architect', 'librarian', 'orchestrator'
];

// Tmux wrapper - always uses spaceos socket
export async function tmux(...args: string[]): Promise<string> {
  try {
    const { stdout } = await execAsync(`tmux -S ${TMUX_SOCKET} ${args.join(' ')}`);
    return stdout.trim();
  } catch (error) {
    return '';
  }
}

// Check if session exists
export async function hasSession(sessionName: string): Promise<boolean> {
  try {
    await execAsync(`tmux -S ${TMUX_SOCKET} has-session -t ${sessionName}`);
    return true;
  } catch {
    return false;
  }
}

// List all running sessions
export async function listSessions(): Promise<string[]> {
  try {
    const { stdout } = await execAsync(`tmux -S ${TMUX_SOCKET} list-sessions -F '#{session_name}'`);
    return stdout.trim().split('\n').filter(s => s);
  } catch {
    return [];
  }
}

// Get session activity timestamp
export async function getSessionActivity(sessionName: string): Promise<number> {
  try {
    const { stdout } = await execAsync(
      `tmux -S ${TMUX_SOCKET} display-message -p -t ${sessionName} '#{session_activity}'`
    );
    return parseInt(stdout.trim(), 10) || 0;
  } catch {
    return 0;
  }
}

// Capture pane output
export async function capturePane(sessionName: string, lines = 10): Promise<string> {
  try {
    const { stdout } = await execAsync(
      `tmux -S ${TMUX_SOCKET} capture-pane -t ${sessionName} -p | tail -${lines}`
    );
    return stdout;
  } catch {
    return '';
  }
}

// Send keys to session
export async function sendKeys(sessionName: string, keys: string): Promise<void> {
  try {
    await execAsync(`tmux -S ${TMUX_SOCKET} send-keys -t ${sessionName} "${keys.replace(/"/g, '\\"')}"`);
  } catch {
    // Ignore errors
  }
}

// Send Enter key
export async function sendEnter(sessionName: string): Promise<void> {
  try {
    await execAsync(`tmux -S ${TMUX_SOCKET} send-keys -t ${sessionName} Enter`);
  } catch {
    // Ignore errors
  }
}

// Kill session
export async function killSession(sessionName: string): Promise<void> {
  try {
    await execAsync(`tmux -S ${TMUX_SOCKET} kill-session -t ${sessionName}`);
  } catch {
    // Ignore errors
  }
}

// Create new session
export async function newSession(sessionName: string, workdir: string): Promise<void> {
  try {
    await execAsync(`tmux -S ${TMUX_SOCKET} new-session -d -s ${sessionName} -c "${workdir}"`);
  } catch {
    // Ignore errors
  }
}

// Telegram notification
export async function telegram(message: string): Promise<void> {
  const token = process.env.TELEGRAM_TOKEN;
  const chatId = process.env.TELEGRAM_CHAT_ID;

  if (!token || !chatId) return;

  try {
    await execAsync(
      `curl -s -X POST "https://api.telegram.org/bot${token}/sendMessage" ` +
      `-d chat_id="${chatId}" --data-urlencode "text=${message}" ` +
      `-d parse_mode="Markdown" -o /dev/null`
    );
  } catch {
    // Ignore errors
  }
}

// Get inbox model from frontmatter
export async function getInboxModel(terminal: string): Promise<string> {
  const inboxPath = path.join(SPACEOS_ROOT, 'docs/mailbox', terminal, 'inbox');

  try {
    const files = await fs.readdir(inboxPath);
    for (const file of files.sort().reverse()) {
      if (!file.endsWith('.md')) continue;

      const content = await fs.readFile(path.join(inboxPath, file), 'utf-8');
      if (!content.includes('status: UNREAD')) continue;

      const modelMatch = content.match(/^model:\s*(\w+)/m);
      if (modelMatch) return modelMatch[1];
    }
  } catch {
    // Directory doesn't exist
  }

  return 'sonnet';
}

// Check if terminal has UNREAD inbox
export async function hasUnreadInbox(terminal: string): Promise<boolean> {
  const inboxPath = path.join(SPACEOS_ROOT, 'docs/mailbox', terminal, 'inbox');

  try {
    const files = await fs.readdir(inboxPath);
    for (const file of files) {
      if (!file.endsWith('.md')) continue;
      const content = await fs.readFile(path.join(inboxPath, file), 'utf-8');
      if (content.includes('status: UNREAD')) return true;
    }
  } catch {
    // Directory doesn't exist
  }

  return false;
}

// State file operations
export async function getState(key: string): Promise<string | null> {
  try {
    const content = await fs.readFile(STATE_FILE, 'utf-8');
    const match = content.match(new RegExp(`^${key}=(.*)$`, 'm'));
    return match ? match[1] : null;
  } catch {
    return null;
  }
}

export async function setState(key: string, value: string): Promise<void> {
  try {
    let content = '';
    try {
      content = await fs.readFile(STATE_FILE, 'utf-8');
    } catch {
      // File doesn't exist
    }

    const regex = new RegExp(`^${key}=.*$`, 'm');
    if (regex.test(content)) {
      content = content.replace(regex, `${key}=${value}`);
    } else {
      content += `${key}=${value}\n`;
    }

    await fs.writeFile(STATE_FILE, content);
  } catch {
    // Ignore errors
  }
}

export async function deleteState(key: string): Promise<void> {
  try {
    const content = await fs.readFile(STATE_FILE, 'utf-8');
    const newContent = content.split('\n').filter(line => !line.startsWith(`${key}=`)).join('\n');
    await fs.writeFile(STATE_FILE, newContent);
  } catch {
    // Ignore errors
  }
}

// Logging
export async function log(message: string): Promise<void> {
  const timestamp = new Date().toISOString().replace('T', ' ').slice(0, 19);
  const logFile = path.join(LOG_DIR, 'nightwatch.log');

  try {
    await fs.mkdir(LOG_DIR, { recursive: true });
    await fs.appendFile(logFile, `${timestamp} ${message}\n`);
  } catch {
    console.log(`[Pipeline] ${message}`);
  }
}

// Is priority session?
export function isPrioritySession(sessionName: string): boolean {
  return PRIORITY_SESSIONS.includes(sessionName) || sessionName === 'spaceos-root';
}
