/**
 * SpaceOS Knowledge Service — Express server
 *
 * Endpoints:
 *   GET  /health
 *   GET  /api/knowledge/search?q=...&topK=5
 *   POST /api/knowledge/search   { q: string, topK?: number }
 *   POST /api/knowledge/index    (re-index docs/knowledge/)
 *   GET  /api/mailbox/:terminal/inbox?status=UNREAD|READ|all
 *   POST /api/mailbox/:terminal/inbox (send_message)
 *   POST /api/mailbox/:terminal/outbox (submit_done)
 *   GET  /api/tasks/status?task_id=...
 *   GET  /api/mailbox/:terminal/subscribe (SSE live notifications)
 */

import 'dotenv/config';
import express, { Request, Response, NextFunction } from 'express';
import { EventEmitter } from 'events';
import path from 'path';

// ─── Constants ────────────────────────────────────────────────────────────────

const projectRoot = '/opt/spaceos';

// ─── Rate Limiting (P1 Security) ─────────────────────────────────────────────

interface RateLimitEntry {
  count: number;
  resetAt: number;
}

const rateLimitStore = new Map<string, RateLimitEntry>();
const RATE_LIMIT_WINDOW = 60000; // 1 minute
const RATE_LIMIT_MAX = 500; // 500 requests per minute per IP (increased for dashboard)

function rateLimit(req: Request, res: Response, next: NextFunction): void {
  // Skip rate limiting for health checks and static assets
  if (
    req.path === '/health' ||
    req.path === '/ready' ||
    req.path.startsWith('/assets/') ||
    req.path === '/favicon.svg' ||
    req.path === '/icons.svg'
  ) {
    next();
    return;
  }

  // Get real IP from proxy headers (nginx sets X-Real-IP)
  const ip = (req.headers['x-real-ip'] as string) ||
              (req.headers['x-forwarded-for'] as string)?.split(',')[0]?.trim() ||
              req.ip ||
              req.socket.remoteAddress ||
              'unknown';

  const now = Date.now();
  const entry = rateLimitStore.get(ip);

  if (!entry || now > entry.resetAt) {
    rateLimitStore.set(ip, { count: 1, resetAt: now + RATE_LIMIT_WINDOW });
    next();
    return;
  }

  if (entry.count >= RATE_LIMIT_MAX) {
    res.status(429).json({
      error: 'Too many requests',
      retryAfter: Math.ceil((entry.resetAt - now) / 1000),
    });
    return;
  }

  entry.count++;
  next();
}

// Cleanup old entries periodically
setInterval(() => {
  const now = Date.now();
  for (const [ip, entry] of rateLimitStore) {
    if (now > entry.resetAt) {
      rateLimitStore.delete(ip);
    }
  }
}, 60000);
import {
  initVectorStore,
  searchKnowledge,
  getDocumentCount,
  usingChroma,
} from './vectorStore';
import { embeddingBackend } from './embeddings';
import { buildIndex } from './indexer';
import {
  listInbox,
  sendMessage,
  submitDone,
  getTaskStatus,
} from './mailbox';
import mcpRouter from './mcp';
import { startInboxWatcher, inboxEvents, InboxEvent, scanExistingUnread } from './inboxWatcher';
import {
  registerWorking,
  registerIdle,
  heartbeat,
  shouldWakeUp,
  getAllStatus,
  getStatus,
} from './terminalStatus';
import { startTerminalSession } from './sessionStarter';
import {
  validate,
  SearchBodySchema,
  SearchQuerySchema,
  TerminalParamSchema,
  InboxQuerySchema,
  TerminalSchema,
} from './validation';
import { startNightwatchScheduler, stopNightwatchScheduler } from './pipeline';

const app = express();
const PORT = parseInt(process.env.PORT || '3456', 10);

// ─── Event Emitter for SSE ───────────────────────────────────────────────────

export const mailboxEvents = new EventEmitter();
mailboxEvents.setMaxListeners(100); // Support many subscribers

interface MailboxEventData {
  terminal: string;
  type: 'new_message' | 'message_sent' | 'done_submitted';
  messageId: string;
  timestamp: string;
  details?: Record<string, unknown>;
}

// SSE clients per terminal
const sseClients: Map<string, Set<Response>> = new Map();

// CORS for browser clients
app.use((req, res, next) => {
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, PUT, DELETE, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type, Authorization');

  if (req.method === 'OPTIONS') {
    res.sendStatus(204);
    return;
  }
  next();
});

app.use(express.json());
app.use(rateLimit);

// ─── MCP Protocol ─────────────────────────────────────────────────────────────

app.use('/mcp', mcpRouter);

// ─── Health ──────────────────────────────────────────────────────────────────

// Service readiness state
let isReady = false;
let isShuttingDown = false;

app.get('/health', async (_req: Request, res: Response) => {
  const count = await getDocumentCount();
  res.json({
    status: isShuttingDown ? 'shutting_down' : 'ok',
    vectorBackend: usingChroma() ? 'chromadb' : 'memory',
    embeddingBackend: embeddingBackend(),
    documents: count,
    knowledgePath: process.env.KNOWLEDGE_BASE_PATH || '(default)',
    port: PORT,
  });
});

// Kubernetes readiness probe
app.get('/ready', async (_req: Request, res: Response) => {
  if (isShuttingDown) {
    res.status(503).json({ ready: false, reason: 'shutting_down' });
    return;
  }

  if (!isReady) {
    res.status(503).json({ ready: false, reason: 'initializing' });
    return;
  }

  try {
    const count = await getDocumentCount();
    if (count === 0) {
      res.status(503).json({ ready: false, reason: 'no_documents' });
      return;
    }
    res.json({ ready: true, documents: count });
  } catch (err) {
    res.status(503).json({ ready: false, reason: 'vector_store_error' });
  }
});

// ─── Search (GET) ─────────────────────────────────────────────────────────────

app.get('/api/knowledge/search', async (req: Request, res: Response) => {
  const q = typeof req.query.q === 'string' ? req.query.q : '';
  const topK = parseInt(String(req.query.topK || '5'), 10);

  if (!q) {
    res.status(400).json({ error: 'q query param required' });
    return;
  }

  try {
    const results = await searchKnowledge(q, topK);
    res.json({ query: q, topK, count: results.length, results });
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : String(err);
    res.status(500).json({ error: msg });
  }
});

// ─── Search (POST) ────────────────────────────────────────────────────────────

app.post('/api/knowledge/search', validate(SearchBodySchema), async (req: Request, res: Response) => {
  const { q, topK } = req.body as { q: string; topK: number };

  try {
    const results = await searchKnowledge(q, topK);
    res.json({ query: q, topK, count: results.length, results });
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : String(err);
    res.status(500).json({ error: msg });
  }
});

// ─── Re-index ─────────────────────────────────────────────────────────────────

app.post('/api/knowledge/index', async (_req: Request, res: Response) => {
  try {
    const result = await buildIndex();
    const count = await getDocumentCount();
    res.json({ success: true, totalInStore: count, ...result });
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : String(err);
    res.status(500).json({ error: msg });
  }
});

// ─── Mailbox: List Inbox ──────────────────────────────────────────────────────

app.get('/api/mailbox/:terminal/inbox', validate(TerminalParamSchema, 'params'), async (req: Request, res: Response) => {
  const terminal = String(req.params.terminal);
  const statusParam = req.query.status as string | undefined;
  const status = statusParam && ['UNREAD', 'READ', 'all'].includes(statusParam)
    ? statusParam as 'UNREAD' | 'READ' | 'all'
    : undefined;

  try {
    const messages = await listInbox(terminal, status);
    res.json({ terminal, status: status || 'all', count: messages.length, messages });
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : String(err);
    res.status(500).json({ error: msg });
  }
});

// ─── Mailbox: Send Message ────────────────────────────────────────────────────

app.post('/api/mailbox/:terminal/inbox', validate(TerminalParamSchema, 'params'), async (req: Request, res: Response) => {
  const terminal = String(req.params.terminal);
  const { type, content, priority, ref, model } = req.body;

  if (!type || !content || !priority) {
    res.status(400).json({ error: 'type, content, and priority are required' });
    return;
  }

  try {
    const result = await sendMessage({
      to: terminal,
      type,
      content,
      priority,
      ref,
      model,
    });

    // Emit SSE event to subscribers
    const eventData: MailboxEventData = {
      terminal,
      type: 'new_message',
      messageId: result.id,
      timestamp: new Date().toISOString(),
      details: { type, priority, ref },
    };
    broadcastToTerminal(terminal, 'new_message', eventData);
    mailboxEvents.emit('new_message', eventData);

    res.json({ success: true, ...result });
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : String(err);
    res.status(500).json({ error: msg });
  }
});

// ─── Mailbox: Submit DONE ─────────────────────────────────────────────────────

app.post('/api/mailbox/:terminal/outbox', validate(TerminalParamSchema, 'params'), async (req: Request, res: Response) => {
  const terminal = String(req.params.terminal);
  const { task_id, summary, files_changed } = req.body;

  if (!task_id || !summary || !files_changed) {
    res.status(400).json({ error: 'task_id, summary, and files_changed are required' });
    return;
  }

  try {
    const result = await submitDone({
      from: terminal,
      task_id,
      summary,
      files_changed,
    });

    // Emit SSE event to 'root' (DONE messages go to root for review)
    const eventData: MailboxEventData = {
      terminal: 'root',
      type: 'done_submitted',
      messageId: result.id,
      timestamp: new Date().toISOString(),
      details: { from: terminal, task_id, summary },
    };
    broadcastToTerminal('root', 'done_submitted', eventData);
    mailboxEvents.emit('done_submitted', eventData);

    res.json({ success: true, ...result });
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : String(err);
    res.status(500).json({ error: msg });
  }
});

// ─── Tasks: Get Status ────────────────────────────────────────────────────────

app.get('/api/tasks/status', async (req: Request, res: Response) => {
  const task_id = req.query.task_id as string | undefined;

  try {
    const tasks = await getTaskStatus(task_id);
    res.json({ count: tasks.length, tasks });
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : String(err);
    res.status(500).json({ error: msg });
  }
});

// ─── SSE: Subscribe to Inbox ─────────────────────────────────────────────────

app.get('/api/mailbox/:terminal/subscribe', (req: Request, res: Response) => {
  const terminal = String(req.params.terminal);

  // Validate terminal
  const parsed = TerminalSchema.safeParse(terminal);
  if (!parsed.success) {
    res.status(400).json({ error: 'Invalid terminal name' });
    return;
  }

  // Set SSE headers
  res.setHeader('Content-Type', 'text/event-stream');
  res.setHeader('Cache-Control', 'no-cache');
  res.setHeader('Connection', 'keep-alive');
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.flushHeaders();

  // Send initial connection event
  res.write(`event: connected\ndata: ${JSON.stringify({ terminal, timestamp: new Date().toISOString() })}\n\n`);

  // Add client to subscribers
  if (!sseClients.has(terminal)) {
    sseClients.set(terminal, new Set());
  }
  sseClients.get(terminal)!.add(res);

  // Also subscribe to 'all' for broadcast messages
  if (!sseClients.has('all')) {
    sseClients.set('all', new Set());
  }
  sseClients.get('all')!.add(res);

  console.log(`[SSE] Client subscribed to terminal: ${terminal} (total: ${sseClients.get(terminal)?.size || 0})`);

  // Keep connection alive with heartbeat every 30s
  const heartbeat = setInterval(() => {
    res.write(`:heartbeat\n\n`);
  }, 30000);

  // Clean up on disconnect
  req.on('close', () => {
    clearInterval(heartbeat);
    sseClients.get(terminal)?.delete(res);
    sseClients.get('all')?.delete(res);
    console.log(`[SSE] Client disconnected from terminal: ${terminal}`);
  });
});

// ─── Broadcast helper ────────────────────────────────────────────────────────

function broadcastToTerminal(terminal: string, event: string, data: MailboxEventData): void {
  const clients = sseClients.get(terminal);
  if (clients) {
    const message = `event: ${event}\ndata: ${JSON.stringify(data)}\n\n`;
    clients.forEach(client => {
      try {
        client.write(message);
      } catch {
        // Client disconnected, will be cleaned up
      }
    });
  }
}

// ─── Broadcast endpoint ──────────────────────────────────────────────────────

app.post('/api/mailbox/broadcast', (req: Request, res: Response) => {
  const { message, priority = 'info' } = req.body;

  if (!message) {
    res.status(400).json({ error: 'message is required' });
    return;
  }

  const eventData: MailboxEventData = {
    terminal: 'all',
    type: 'new_message',
    messageId: `BROADCAST-${Date.now()}`,
    timestamp: new Date().toISOString(),
    details: { message, priority },
  };

  broadcastToTerminal('all', 'broadcast', eventData);

  res.json({ success: true, sentTo: sseClients.get('all')?.size || 0 });
});

// ─── Error handler ────────────────────────────────────────────────────────────

// eslint-disable-next-line @typescript-eslint/no-unused-vars
app.use((err: Error, _req: Request, res: Response, _next: NextFunction) => {
  console.error('[ERROR]', err.message);
  res.status(500).json({ error: err.message });
});

// ─── Terminal Status API ─────────────────────────────────────────────────────

app.post('/api/terminal/:terminal/status', (req: Request, res: Response) => {
  const terminal = String(req.params.terminal);
  const { state, taskId } = req.body as { state?: 'working' | 'idle'; taskId?: string };

  if (state === 'working') {
    registerWorking(terminal, taskId);
    res.json({ success: true, terminal, state: 'working', taskId });
  } else if (state === 'idle') {
    registerIdle(terminal);
    res.json({ success: true, terminal, state: 'idle' });
  } else {
    // Heartbeat if no state specified
    heartbeat(terminal);
    res.json({ success: true, terminal, action: 'heartbeat' });
  }
});

app.get('/api/terminal/:terminal/status', (req: Request, res: Response) => {
  const terminal = String(req.params.terminal);
  const status = getStatus(terminal);
  res.json({ terminal, status: status || { state: 'idle', lastActivity: null } });
});

app.get('/api/terminals/status', (_req: Request, res: Response) => {
  res.json({ terminals: getAllStatus() });
});

// ─── Auth API (for React Dashboard) ──────────────────────────────────────────

// Simple auth token (no database, just static token from env)
const DASHBOARD_TOKEN = process.env.DASHBOARD_AUTH_TOKEN || 'dev-token-spaceos-dashboard-2026';

// Support both GET and POST for auth verification
const verifyAuthToken = (req: Request, res: Response) => {
  const authHeader = req.headers.authorization;
  const token = authHeader?.replace('Bearer ', '');

  if (token === DASHBOARD_TOKEN) {
    res.json({ valid: true, message: 'Token is valid' });
  } else {
    res.status(401).json({ valid: false, message: 'Invalid token' });
  }
};

app.get('/api/auth/verify', verifyAuthToken);
app.post('/api/auth/verify', verifyAuthToken);

// ─── Terminal Status API ─────────────────────────────────────────────────────

app.post('/api/terminal/status', express.json(), async (req: Request, res: Response) => {
  try {
    const { terminal, status, currentTask } = req.body;

    if (!terminal || typeof terminal !== 'string') {
      return res.status(400).json({ error: 'Missing or invalid "terminal" field' });
    }

    if (!status || (status !== 'working' && status !== 'idle')) {
      return res.status(400).json({ error: 'Invalid "status" field - must be "working" or "idle"' });
    }

    if (status === 'working') {
      registerWorking(terminal, currentTask || 'Working');
      res.json({
        success: true,
        message: `Terminal "${terminal}" registered as WORKING`,
        currentTask: currentTask || 'Working'
      });
    } else {
      registerIdle(terminal);
      res.json({
        success: true,
        message: `Terminal "${terminal}" registered as IDLE`
      });
    }
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : String(err);
    res.status(500).json({ error: msg });
  }
});

// ─── Dashboard API ───────────────────────────────────────────────────────────

app.get('/api/dashboard', async (_req: Request, res: Response) => {
  try {
    const fs = await import('fs/promises');
    const path = await import('path');

    const projectRoot = '/opt/spaceos';
    const mailboxRoot = path.join(projectRoot, 'docs/mailbox');

    // Get all terminals status
    const terminals: any[] = [];
    const terminalNames = [
      'kernel', 'orch', 'fe', 'joinery', 'abstractions', 'cutting',
      'inventory', 'procurement', 'sales', 'identity', 'infra', 'e2e',
      'architect', 'librarian', 'nexus', 'root', 'conductor'
    ];

    for (const terminal of terminalNames) {
      const inboxDir = path.join(mailboxRoot, terminal, 'inbox');
      const outboxDir = path.join(mailboxRoot, terminal, 'outbox');

      let inboxCount = 0;
      let outboxCount = 0;
      let unreadInbox = 0;
      let unreadOutbox = 0;

      try {
        const inboxFiles = await fs.readdir(inboxDir);
        inboxCount = inboxFiles.filter(f => f.endsWith('.md')).length;
        for (const file of inboxFiles.filter(f => f.endsWith('.md'))) {
          const content = await fs.readFile(path.join(inboxDir, file), 'utf-8');
          if (content.includes('status: UNREAD')) unreadInbox++;
        }
      } catch (err) { /* dir may not exist */ }

      try {
        const outboxFiles = await fs.readdir(outboxDir);
        outboxCount = outboxFiles.filter(f => f.endsWith('.md')).length;
        for (const file of outboxFiles.filter(f => f.endsWith('.md'))) {
          const content = await fs.readFile(path.join(outboxDir, file), 'utf-8');
          if (content.includes('status: UNREAD')) unreadOutbox++;
        }
      } catch (err) { /* dir may not exist */ }

      const status = getStatus(terminal);

      terminals.push({
        name: terminal,
        inbox: inboxCount,
        outbox: outboxCount,
        unreadInbox,
        unreadOutbox,
        status: status?.state || 'idle',
        lastActivity: status?.lastActivity || null,
      });
    }

    // Global metrics
    const totalInbox = terminals.reduce((sum, t) => sum + t.inbox, 0);
    const totalOutbox = terminals.reduce((sum, t) => sum + t.outbox, 0);
    const totalUnread = terminals.reduce((sum, t) => sum + t.unreadInbox + t.unreadOutbox, 0);
    const activeSessions = terminals.filter(t => t.status === 'working').length;

    res.json({
      timestamp: new Date().toISOString(),
      metrics: {
        totalInbox,
        totalOutbox,
        totalUnread,
        activeSessions,
        terminals: terminals.length,
      },
      terminals,
    });
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : String(err);
    res.status(500).json({ error: msg });
  }
});

// ─── Kanban API ──────────────────────────────────────────────────────────────

app.get('/api/kanban/snapshot', async (_req: Request, res: Response) => {
  try {
    const fs = await import('fs/promises');
    const path = await import('path');

    const projectRoot = '/opt/spaceos';
    const planningRoot = path.join(projectRoot, 'docs/planning');
    const mailboxRoot = path.join(projectRoot, 'docs/mailbox');

    // Discovery Track
    const discovery = {
      ideas: [],
      selected: [],
      debate: [],
      consensus: [],
      queue: [],
      totals: { ideas: 0, selected: 0, debate: 0, consensus: 0, queue: 0 },
    };

    try {
      const ideaFiles = await fs.readdir(path.join(planningRoot, 'ideas'));
      discovery.totals.ideas = ideaFiles.filter(f => f.endsWith('.md')).length;
    } catch (err) { /* dir may not exist */ }

    try {
      const queueFiles = await fs.readdir(path.join(planningRoot, 'queue'));
      discovery.totals.queue = queueFiles.filter(f => f.endsWith('.md')).length;
    } catch (err) { /* dir may not exist */ }

    // Delivery Track
    const terminals = [
      'kernel', 'orch', 'fe', 'joinery', 'cutting', 'identity',
      'infra', 'e2e', 'architect', 'librarian', 'nexus', 'conductor'
    ];

    const swimlanes: any[] = [];

    for (const terminal of terminals) {
      const inboxDir = path.join(mailboxRoot, terminal, 'inbox');
      const outboxDir = path.join(mailboxRoot, terminal, 'outbox');

      let inbox = 0;
      let working = 0;
      let review = 0;
      let done = 0;

      try {
        const inboxFiles = await fs.readdir(inboxDir);
        inbox = inboxFiles.filter(f => f.endsWith('.md')).length;
      } catch (err) { /* dir may not exist */ }

      try {
        const outboxFiles = await fs.readdir(outboxDir);
        done = outboxFiles.filter(f => f.endsWith('.md')).length;
      } catch (err) { /* dir may not exist */ }

      const status = getStatus(terminal);

      swimlanes.push({
        terminal,
        sessionActive: status?.state === 'working',
        totals: { inbox, working, review, done },
        messages: { inbox: [], working: [], review: [], done: [] },
      });
    }

    const delivery = {
      swimlanes,
      activeSessions: swimlanes.filter(s => s.sessionActive).map(s => s.terminal),
      totals: {
        inbox: swimlanes.reduce((sum, s) => sum + s.totals.inbox, 0),
        working: swimlanes.reduce((sum, s) => sum + s.totals.working, 0),
        review: swimlanes.reduce((sum, s) => sum + s.totals.review, 0),
        done: swimlanes.reduce((sum, s) => sum + s.totals.done, 0),
      },
    };

    res.json({ discovery, delivery });
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : String(err);
    res.status(500).json({ error: msg });
  }
});

app.get('/api/kanban/metrics', async (_req: Request, res: Response) => {
  try {
    const fs = await import('fs/promises');
    const path = await import('path');

    const projectRoot = '/opt/spaceos';
    const planningRoot = path.join(projectRoot, 'docs/planning');

    let discoveryWip = 0;
    let deliveryWip = 0;

    try {
      const ideaFiles = await fs.readdir(path.join(planningRoot, 'ideas'));
      discoveryWip += ideaFiles.filter(f => f.endsWith('.md')).length;
    } catch (err) { /* ignore */ }

    try {
      const queueFiles = await fs.readdir(path.join(planningRoot, 'queue'));
      discoveryWip += queueFiles.filter(f => f.endsWith('.md')).length;
    } catch (err) { /* ignore */ }

    const allStatus = getAllStatus();
    const activeSessions = Object.values(allStatus).filter((s: any) => s.state === 'working').length;

    res.json({
      discoveryWip,
      deliveryWip,
      activeSessions,
      throughput: 0,
      cycleTime: 0,
    });
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : String(err);
    res.status(500).json({ error: msg });
  }
});

// ─── Planning Pipeline API ───────────────────────────────────────────────────

app.get('/api/planning/items', async (_req: Request, res: Response) => {
  try {
    const fs = await import('fs/promises');
    const path = await import('path');

    const projectRoot = '/opt/spaceos';
    const ideasDir = path.join(projectRoot, 'docs/planning/ideas');
    const selectedDir = path.join(projectRoot, 'docs/planning/selected');
    const queueDir = path.join(projectRoot, 'docs/planning/queue');

    // Read all planning items
    const items: any[] = [];

    // Ideas
    try {
      const ideaFiles = await fs.readdir(ideasDir);
      for (const file of ideaFiles.filter(f => f.endsWith('.md'))) {
        const content = await fs.readFile(path.join(ideasDir, file), 'utf-8');
        const match = content.match(/^#\s+(.+)$/m);
        items.push({
          id: file,
          title: match ? match[1] : file,
          status: 'idea',
          priority: 'medium',
          createdAt: (await fs.stat(path.join(ideasDir, file))).mtime.toISOString(),
        });
      }
    } catch (err) { /* dir may not exist */ }

    // Queue
    try {
      const queueFiles = await fs.readdir(queueDir);
      for (const file of queueFiles.filter(f => f.endsWith('.md'))) {
        const content = await fs.readFile(path.join(queueDir, file), 'utf-8');
        const match = content.match(/^#\s+(.+)$/m);
        items.push({
          id: file,
          title: match ? match[1] : file,
          status: 'queue',
          priority: 'high',
          createdAt: (await fs.stat(path.join(queueDir, file))).mtime.toISOString(),
        });
      }
    } catch (err) { /* dir may not exist */ }

    // Metrics
    const metrics = {
      ideas: items.filter(i => i.status === 'idea').length,
      selected: 0,
      inDebate: 0,
      consensus: 0,
      queued: items.filter(i => i.status === 'queue').length,
      lastScan: new Date().toISOString(),
    };

    res.json({ items, metrics });
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : String(err);
    res.status(500).json({ error: msg });
  }
});

// ─── Projects API ────────────────────────────────────────────────────────────

app.get('/api/projects', async (_req: Request, res: Response) => {
  try {
    const fs = await import('fs/promises');
    const path = await import('path');

    const projectRoot = '/opt/spaceos';
    const tasksDir = path.join(projectRoot, 'docs/tasks');

    // Read active tasks
    const projects: any[] = [];
    const milestones: any[] = [];

    try {
      const activeFiles = await fs.readdir(path.join(tasksDir, 'active'));
      for (const file of activeFiles.filter(f => f.endsWith('.md'))) {
        const content = await fs.readFile(path.join(tasksDir, 'active', file), 'utf-8');
        const match = content.match(/^#\s+(.+)$/m);
        const title = match ? match[1] : file;

        projects.push({
          id: file,
          name: title,
          status: 'active',
          priority: 'high',
          startDate: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString(),
          endDate: new Date(Date.now() + 14 * 24 * 60 * 60 * 1000).toISOString(),
          progress: Math.floor(Math.random() * 80),
          terminal: 'nexus',
          epic: 'NEXUS-001',
          tasks: 10,
          completedTasks: Math.floor(Math.random() * 8),
        });
      }
    } catch (err) { /* dir may not exist */ }

    // Add a demo milestone
    milestones.push({
      id: 'milestone-1',
      name: 'Nexus Phase 6 Complete',
      date: new Date(Date.now() + 21 * 24 * 60 * 60 * 1000).toISOString(),
      status: 'upcoming',
    });

    res.json({ projects, milestones });
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : String(err);
    res.status(500).json({ error: msg });
  }
});

// ─── Inbox Watcher SSE Bridge ────────────────────────────────────────────────

function setupInboxWatcherBridge(): void {
  inboxEvents.on('inbox_change', async (event: InboxEvent) => {
    // Check if terminal should be woken up (not already working)
    if (!shouldWakeUp(event.terminal)) {
      console.log(`[InboxWatcher] ${event.terminal} is WORKING — not sending wake-up for ${event.messageId}`);
      return;
    }

    // Broadcast to the specific terminal
    const eventData: MailboxEventData = {
      terminal: event.terminal,
      type: 'new_message',
      messageId: event.messageId,
      timestamp: event.timestamp,
      details: {
        priority: event.priority,
        messageType: event.messageType,
        filePath: event.filePath,
        source: 'file_watcher',
      },
    };

    broadcastToTerminal(event.terminal, 'wake_up', eventData);
    mailboxEvents.emit('new_message', eventData);

    console.log(`[SSE] Wake-up sent to ${event.terminal} for ${event.messageId}`);

    // Start the terminal session directly (no external daemon needed)
    try {
      const result = await startTerminalSession(event.terminal, event.messageId);
      if (result.success) {
        console.log(`[SessionStarter] ${result.message}`);
      } else {
        console.log(`[SessionStarter] Skip: ${result.message}`);
      }
    } catch (err) {
      console.error(`[SessionStarter] Error starting ${event.terminal}:`, err);
    }
  });
}

// ─── React Frontend Serving ──────────────────────────────────────────────────

// Serve React build (Datahaven Dashboard)
const reactBuildPath = path.join(projectRoot, 'datahaven-web/client/dist');
app.use(express.static(reactBuildPath));

// SPA fallback - serve index.html for all non-API routes
app.use((req: Request, res: Response) => {
  // Skip if already responded (static files)
  if (res.headersSent) {
    return;
  }

  // 404 for API routes
  if (req.path.startsWith('/api') || req.path.startsWith('/mcp') || req.path === '/health' || req.path === '/ready') {
    return res.status(404).json({ error: 'Not found' });
  }

  // Serve React app for all other routes
  res.sendFile(path.join(reactBuildPath, 'index.html'));
});

// ─── Startup ──────────────────────────────────────────────────────────────────

async function main() {
  await initVectorStore();

  const count = await getDocumentCount();
  if (count === 0) {
    console.log('📚 Store empty — running initial knowledge base indexing...');
    await buildIndex();
  } else {
    console.log(
      `📚 Store has ${count} documents. POST /api/knowledge/index to re-index.`
    );
  }

  // Start inbox file watcher
  startInboxWatcher();
  setupInboxWatcherBridge();

  // Log existing UNREAD messages on startup
  const existingUnread = await scanExistingUnread();
  if (existingUnread.length > 0) {
    console.log(`\n📬 Found ${existingUnread.length} existing UNREAD messages:`);
    for (const msg of existingUnread) {
      console.log(`   - ${msg.terminal}: ${msg.messageId} (${msg.priority || 'normal'})`);
    }
  }

  const server = app.listen(PORT, () => {
    isReady = true;
    console.log(`\n🚀 SpaceOS Knowledge Service on port ${PORT}`);
    console.log(`   GET  /health`);
    console.log(`   GET  /ready`);
    console.log(`\n   Knowledge Service:`);
    console.log(`   GET  /api/knowledge/search?q=...&topK=5`);
    console.log(`   POST /api/knowledge/search   { q, topK? }`);
    console.log(`   POST /api/knowledge/index    (re-index)`);
    console.log(`\n   Mailbox Tools:`);
    console.log(`   GET  /api/mailbox/:terminal/inbox?status=UNREAD|READ|all`);
    console.log(`   POST /api/mailbox/:terminal/inbox   (send_message)`);
    console.log(`   POST /api/mailbox/:terminal/outbox  (submit_done)`);
    console.log(`\n   Tasks:`);
    console.log(`   GET  /api/tasks/status?task_id=...`);
    console.log(`\n   Live Notifications:`);
    console.log(`   GET  /api/mailbox/:terminal/subscribe  (SSE wake-on-inbox)`);
    console.log(`\n   MCP Protocol (Claude Code):`);
    console.log(`   GET  /mcp              (server info)`);
    console.log(`   POST /mcp              (JSON-RPC: initialize, tools/list, tools/call)\n`);

    // Start Nightwatch scheduler if enabled (replaces bash cron)
    if (process.env.ENABLE_NIGHTWATCH === 'true') {
      const intervalMs = parseInt(process.env.NIGHTWATCH_INTERVAL || '120000', 10);
      startNightwatchScheduler(intervalMs);
      console.log(`   ⏰ Nightwatch Scheduler: ENABLED (every ${intervalMs / 1000}s)\n`);
    } else {
      console.log(`   ⏰ Nightwatch Scheduler: DISABLED (set ENABLE_NIGHTWATCH=true to enable)\n`);
    }
  });

  // Graceful shutdown (P2)
  const gracefulShutdown = (signal: string) => {
    console.log(`\n⏳ ${signal} received, shutting down gracefully...`);
    isShuttingDown = true;
    isReady = false;

    // Stop Nightwatch scheduler if running
    stopNightwatchScheduler();

    // Stop accepting new connections
    server.close(() => {
      console.log('✅ HTTP server closed');

      // Close all SSE connections
      for (const [terminal, clients] of sseClients) {
        for (const client of clients) {
          client.end();
        }
        clients.clear();
      }
      console.log('✅ SSE connections closed');

      console.log('👋 Goodbye!');
      process.exit(0);
    });

    // Force exit after 10 seconds
    setTimeout(() => {
      console.error('⚠️ Forced exit after timeout');
      process.exit(1);
    }, 10000);
  };

  process.on('SIGTERM', () => gracefulShutdown('SIGTERM'));
  process.on('SIGINT', () => gracefulShutdown('SIGINT'));
}

main().catch(err => {
  console.error('Fatal startup error:', err);
  process.exit(1);
});
