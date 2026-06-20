/**
 * SpaceOS Mailbox Service — MCP Phase 2 Tools
 *
 * Tools:
 *   - list_inbox: List inbox messages for a terminal
 *   - send_message: Create new inbox message
 *   - submit_done: Create DONE outbox message
 *   - get_task_status: Query task status from docs/tasks/
 */

import * as fs from 'fs/promises';
import * as path from 'path';
import matter from 'gray-matter';

// ─── Types ────────────────────────────────────────────────────────────────

interface MessageFrontmatter {
  id: string;
  from: string;
  to: string;
  type: 'task' | 'question' | 'done' | 'blocked';
  priority: 'critical' | 'high' | 'medium' | 'low';
  status: 'UNREAD' | 'READ';
  model?: 'haiku' | 'sonnet' | 'opus';
  ref?: string;
  created: string;
  completed?: string;
}

interface InboxMessage {
  frontmatter: MessageFrontmatter;
  content: string;
  filePath: string;
}

interface SendMessageParams {
  to: string;
  type: 'task' | 'question' | 'done' | 'blocked';
  content: string;
  priority: 'critical' | 'high' | 'medium' | 'low';
  ref?: string;
  model?: 'haiku' | 'sonnet' | 'opus';
}

interface SubmitDoneParams {
  from: string;
  task_id: string;
  summary: string;
  files_changed: string[];
}

interface TaskStatus {
  id: string;
  title: string;
  status: 'new' | 'active' | 'archive';
  priority: 'high' | 'medium' | 'low';
  assignee?: string;
  epic?: string;
  blocked_by?: string;
  created: string;
  updated: string;
  filePath: string;
}

// ─── Constants ────────────────────────────────────────────────────────────

const REPO_ROOT = path.resolve(__dirname, '../../..');
const MAILBOX_ROOT = path.join(REPO_ROOT, 'docs/mailbox');
const TASKS_ROOT = path.join(REPO_ROOT, 'docs/tasks');

// ─── Helpers ──────────────────────────────────────────────────────────────

/**
 * Get the next message sequence number for a terminal's inbox/outbox
 */
async function getNextMessageNumber(
  terminal: string,
  box: 'inbox' | 'outbox'
): Promise<number> {
  const boxPath = path.join(MAILBOX_ROOT, terminal, box);

  try {
    const files = await fs.readdir(boxPath);
    const numbers = files
      .filter(f => f.match(/^\d{4}-\d{2}-\d{2}_(\d{3})_/))
      .map(f => {
        const match = f.match(/_(\d{3})_/);
        return match ? parseInt(match[1], 10) : 0;
      });

    return numbers.length > 0 ? Math.max(...numbers) + 1 : 1;
  } catch (err) {
    // Directory doesn't exist or is empty
    return 1;
  }
}

/**
 * Generate slug from content (first heading or title)
 */
function generateSlug(content: string): string {
  // Try to extract first heading
  const headingMatch = content.match(/^#\s+(.+)$/m);
  if (headingMatch) {
    return headingMatch[1]
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-|-$/g, '')
      .substring(0, 50);
  }

  // Fallback: use first few words
  const words = content
    .split(/\s+/)
    .slice(0, 5)
    .join('-')
    .toLowerCase()
    .replace(/[^a-z0-9-]/g, '');

  return words || 'message';
}

/**
 * Format date as YYYY-MM-DD
 */
function formatDate(date: Date = new Date()): string {
  return date.toISOString().split('T')[0];
}

/**
 * Parse message file with frontmatter
 */
async function parseMessageFile(filePath: string): Promise<InboxMessage> {
  const raw = await fs.readFile(filePath, 'utf-8');
  const { data, content } = matter(raw);

  return {
    frontmatter: data as MessageFrontmatter,
    content: content.trim(),
    filePath,
  };
}

// ─── API Functions ────────────────────────────────────────────────────────

/**
 * List inbox messages for a terminal
 */
export async function listInbox(
  terminal: string,
  status?: 'UNREAD' | 'READ' | 'all'
): Promise<InboxMessage[]> {
  const inboxPath = path.join(MAILBOX_ROOT, terminal, 'inbox');

  try {
    const files = await fs.readdir(inboxPath);
    const mdFiles = files.filter(f => f.endsWith('.md'));

    const messages = await Promise.all(
      mdFiles.map(async file => {
        const filePath = path.join(inboxPath, file);
        return parseMessageFile(filePath);
      })
    );

    // Filter by status if specified
    if (status && status !== 'all') {
      return messages.filter(m => m.frontmatter.status === status);
    }

    return messages;
  } catch (err) {
    if ((err as NodeJS.ErrnoException).code === 'ENOENT') {
      return []; // Inbox doesn't exist
    }
    throw err;
  }
}

/**
 * Send a message to a terminal's inbox
 */
export async function sendMessage(
  params: SendMessageParams
): Promise<{ id: string; path: string }> {
  const { to, type, content, priority, ref, model } = params;

  // Ensure inbox directory exists
  const inboxPath = path.join(MAILBOX_ROOT, to, 'inbox');
  await fs.mkdir(inboxPath, { recursive: true });

  // Get next message number
  const num = await getNextMessageNumber(to, 'inbox');
  const msgId = `MSG-${to.toUpperCase()}-${num.toString().padStart(3, '0')}`;

  // Generate slug
  const slug = generateSlug(content);

  // Create filename
  const date = formatDate();
  const filename = `${date}_${num.toString().padStart(3, '0')}_${slug}.md`;
  const filePath = path.join(inboxPath, filename);

  // Build frontmatter
  const frontmatter: Partial<MessageFrontmatter> = {
    id: msgId,
    from: 'mcp-server', // or get from context
    to,
    type,
    priority,
    status: 'UNREAD',
    created: date,
  };

  if (ref) frontmatter.ref = ref;
  if (model) frontmatter.model = model;

  // Write file
  const yaml = Object.entries(frontmatter)
    .map(([k, v]) => `${k}: ${v}`)
    .join('\n');

  const fileContent = `---\n${yaml}\n---\n\n${content}\n`;

  await fs.writeFile(filePath, fileContent, 'utf-8');

  return { id: msgId, path: filePath };
}

/**
 * Submit a DONE outbox message
 */
export async function submitDone(
  params: SubmitDoneParams
): Promise<{ id: string; path: string }> {
  const { from, task_id, summary, files_changed } = params;

  // Ensure outbox directory exists
  const outboxPath = path.join(MAILBOX_ROOT, from, 'outbox');
  await fs.mkdir(outboxPath, { recursive: true });

  // Get next message number
  const num = await getNextMessageNumber(from, 'outbox');
  const msgId = `MSG-${from.toUpperCase()}-${num.toString().padStart(3, '0')}-DONE`;

  // Generate slug
  const slug = generateSlug(summary);

  // Create filename
  const date = formatDate();
  const filename = `${date}_${num.toString().padStart(3, '0')}_${slug}.md`;
  const filePath = path.join(outboxPath, filename);

  // Build frontmatter
  const frontmatter = {
    id: msgId,
    from,
    to: 'root',
    type: 'done',
    priority: 'high',
    status: 'UNREAD',
    ref: task_id,
    created: date,
    completed: date,
  };

  // Build content
  const filesSection = files_changed
    .map(f => `- ${f}`)
    .join('\n');

  const content = `# ${task_id} — DONE

## Summary

${summary}

## Files Changed

${filesSection}

---

**Timestamp:** ${new Date().toISOString()}
`;

  // Write file
  const yaml = Object.entries(frontmatter)
    .map(([k, v]) => `${k}: ${v}`)
    .join('\n');

  const fileContent = `---\n${yaml}\n---\n\n${content}`;

  await fs.writeFile(filePath, fileContent, 'utf-8');

  return { id: msgId, path: filePath };
}

/**
 * Get task status from docs/tasks/
 */
export async function getTaskStatus(
  task_id?: string
): Promise<TaskStatus[]> {
  const statuses: ('new' | 'active' | 'archive')[] = ['new', 'active', 'archive'];
  const tasks: TaskStatus[] = [];

  for (const status of statuses) {
    const tasksPath = path.join(TASKS_ROOT, status);

    try {
      const files = await fs.readdir(tasksPath, { recursive: true });
      const mdFiles = files.filter(f => typeof f === 'string' && f.endsWith('.md'));

      for (const file of mdFiles) {
        const filePath = path.join(tasksPath, file as string);
        const raw = await fs.readFile(filePath, 'utf-8');
        const { data } = matter(raw);

        const task: TaskStatus = {
          id: data.id || 'unknown',
          title: data.title || 'Untitled',
          status,
          priority: data.priority || 'medium',
          assignee: data.assignee,
          epic: data.epic,
          blocked_by: data.blocked_by,
          created: data.created || 'unknown',
          updated: data.updated || data.created || 'unknown',
          filePath,
        };

        // Filter by task_id if specified
        if (!task_id || task.id === task_id) {
          tasks.push(task);
        }
      }
    } catch (err) {
      if ((err as NodeJS.ErrnoException).code !== 'ENOENT') {
        throw err;
      }
      // Directory doesn't exist, skip
    }
  }

  return tasks;
}
