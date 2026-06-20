// Inbox Watcher - Real-time file system monitoring for SpaceOS mailbox
// Watches docs/mailbox/*/inbox/ directories for new UNREAD messages
// and emits SSE events to wake up the appropriate terminal.

import { watch, type FSWatcher } from 'chokidar';
import { promises as fs } from 'fs';
import * as path from 'path';
import { EventEmitter } from 'events';

const SPACEOS_ROOT = process.env.SPACEOS_ROOT || '/opt/spaceos';
const MAILBOX_PATH = path.join(SPACEOS_ROOT, 'docs/mailbox');

// Event emitter for inbox changes
export const inboxEvents = new EventEmitter();
inboxEvents.setMaxListeners(100);

export interface InboxEvent {
  type: 'new_inbox' | 'inbox_updated';
  terminal: string;
  messageId: string;
  filePath: string;
  status: string;
  priority?: string;
  messageType?: string;
  timestamp: string;
}

// Parse YAML frontmatter from markdown file
async function parseFrontmatter(filePath: string): Promise<Record<string, string> | null> {
  try {
    const content = await fs.readFile(filePath, 'utf-8');
    const match = content.match(/^---\n([\s\S]*?)\n---/);
    if (!match) return null;

    const frontmatter: Record<string, string> = {};
    const lines = match[1].split('\n');
    for (const line of lines) {
      const colonIndex = line.indexOf(':');
      if (colonIndex > 0) {
        const key = line.slice(0, colonIndex).trim();
        const value = line.slice(colonIndex + 1).trim();
        frontmatter[key] = value;
      }
    }
    return frontmatter;
  } catch {
    return null;
  }
}

// Extract terminal name from file path
// e.g., /opt/spaceos/docs/mailbox/kernel/inbox/file.md -> kernel
function extractTerminal(filePath: string): string | null {
  const match = filePath.match(/\/mailbox\/([^/]+)\/inbox\//);
  return match ? match[1] : null;
}

// Handle new or modified inbox file
async function handleInboxChange(filePath: string, eventType: 'add' | 'change'): Promise<void> {
  // Only process .md files
  if (!filePath.endsWith('.md')) return;

  const terminal = extractTerminal(filePath);
  if (!terminal) return;

  const frontmatter = await parseFrontmatter(filePath);
  if (!frontmatter) return;

  // Only emit for UNREAD messages
  if (frontmatter.status !== 'UNREAD') return;

  const event: InboxEvent = {
    type: eventType === 'add' ? 'new_inbox' : 'inbox_updated',
    terminal,
    messageId: frontmatter.id || path.basename(filePath, '.md'),
    filePath,
    status: frontmatter.status,
    priority: frontmatter.priority,
    messageType: frontmatter.type,
    timestamp: new Date().toISOString(),
  };

  console.log(`[InboxWatcher] ${event.type}: ${terminal} <- ${event.messageId} (${event.priority || 'normal'})`);
  inboxEvents.emit('inbox_change', event);
}

// Start watching all inbox directories
export function startInboxWatcher(): FSWatcher {
  // Watch the entire mailbox directory recursively, then filter for inbox/*.md
  console.log(`[InboxWatcher] Starting recursive watch on: ${MAILBOX_PATH}`);

  const watcher = watch(MAILBOX_PATH, {
    persistent: true,
    ignoreInitial: true, // Don't emit for existing files on startup
    usePolling: true, // Use polling for better cross-fs compatibility
    interval: 1000, // Poll every 1 second
    depth: 3, // Only need to go 3 levels: mailbox/terminal/inbox/file.md
    awaitWriteFinish: {
      stabilityThreshold: 500, // Wait for file to be fully written
      pollInterval: 100,
    },
    // Filter to only watch inbox directories
    ignored: (pathStr: string) => {
      // Allow the mailbox directory itself
      if (pathStr === MAILBOX_PATH) return false;
      // Allow terminal directories (mailbox/terminal)
      if (pathStr.startsWith(MAILBOX_PATH) && !pathStr.includes('/inbox')) {
        // Check if it's a direct child of mailbox
        const relative = pathStr.slice(MAILBOX_PATH.length + 1);
        if (!relative.includes('/')) return false;
      }
      // Allow inbox directories
      if (pathStr.endsWith('/inbox')) return false;
      // Allow .md files in inbox directories
      if (pathStr.includes('/inbox/') && pathStr.endsWith('.md')) return false;
      // Ignore everything else
      return true;
    },
  });

  watcher.on('add', (filePath: string) => {
    console.log(`[InboxWatcher] File detected: ${filePath}`);
    if (filePath.includes('/inbox/') && filePath.endsWith('.md')) {
      handleInboxChange(filePath, 'add');
    }
  });

  watcher.on('change', (filePath: string) => {
    if (filePath.includes('/inbox/') && filePath.endsWith('.md')) {
      handleInboxChange(filePath, 'change');
    }
  });

  watcher.on('error', (error: unknown) => {
    console.error('[InboxWatcher] Error:', error);
  });

  watcher.on('ready', () => {
    console.log('[InboxWatcher] Ready and watching for inbox changes');
  });

  return watcher;
}

// Scan for existing UNREAD messages (call on startup if needed)
export async function scanExistingUnread(): Promise<InboxEvent[]> {
  const events: InboxEvent[] = [];

  try {
    const terminals = await fs.readdir(MAILBOX_PATH);

    for (const terminal of terminals) {
      const inboxPath = path.join(MAILBOX_PATH, terminal, 'inbox');

      try {
        const files = await fs.readdir(inboxPath);

        for (const file of files) {
          if (!file.endsWith('.md')) continue;

          const filePath = path.join(inboxPath, file);
          const frontmatter = await parseFrontmatter(filePath);

          if (frontmatter?.status === 'UNREAD') {
            events.push({
              type: 'new_inbox',
              terminal,
              messageId: frontmatter.id || path.basename(file, '.md'),
              filePath,
              status: 'UNREAD',
              priority: frontmatter.priority,
              messageType: frontmatter.type,
              timestamp: new Date().toISOString(),
            });
          }
        }
      } catch {
        // Inbox directory doesn't exist for this terminal
      }
    }
  } catch (error) {
    console.error('[InboxWatcher] Error scanning existing:', error);
  }

  return events;
}
