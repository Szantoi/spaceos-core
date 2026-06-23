/**
 * Datahaven Web Dashboard
 *
 * Express server with 3-layer architecture:
 * - Routes: HTTP endpoint handlers
 * - Services: Business logic
 * - Data: Database access
 *
 * Features:
 * - REST API for messaging and stats
 * - Static file serving for dashboard UI
 * - Real-time updates via SSE
 * - Token-based authentication
 */

import express from 'express';
import cors from 'cors';
import { fileURLToPath } from 'url';
import { dirname, join } from 'path';
import dotenv from 'dotenv';

// Load environment
dotenv.config();

// Data layer
import { initDatabase } from './data/database.js';

// Middleware
import { createAuthMiddleware, createRateLimiter } from './middleware/auth.js';

// Routes
import statsRoutes from './routes/statsRoutes.js';
import daemonRoutes from './routes/daemonRoutes.js';
import messageRoutes from './routes/messageRoutes.js';
import kanbanRoutes from './routes/kanbanRoutes.js';
import { createKnowledgeRoutes } from './routes/knowledgeRoutes.js';
import { createDashboardRoutes } from './routes/dashboardRoutes.js';
import { createAutonomousRoutes } from './routes/autonomousRoutes.js';
import sseRoutes, { startPolling } from './routes/sseRoutes.js';

// =============================================================================
// Configuration
// =============================================================================

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const config = {
  port: process.env.PORT || 3457,
  host: process.env.HOST || '0.0.0.0',
  dbPath: process.env.MESSAGES_DB || '/opt/spaceos/datahaven/messages.db',
  knowledgeUrl: process.env.KNOWLEDGE_URL || 'http://localhost:3456',
  auth: {
    enabled: process.env.AUTH_ENABLED === 'true',
    token: process.env.AUTH_TOKEN || ''
  },
  rateLimit: {
    windowMs: parseInt(process.env.RATE_LIMIT_WINDOW_MS) || 60000,
    maxRequests: parseInt(process.env.RATE_LIMIT_MAX) || 100
  }
};

// =============================================================================
// Initialize
// =============================================================================

const app = express();

// Initialize database
initDatabase(config.dbPath);

// =============================================================================
// Global Middleware
// =============================================================================

app.use(cors());
app.use(express.json());

// Static files (no auth required for dashboard)
// Serve React SPA from client/dist (prioritize over legacy public folder)
app.use(express.static(join(__dirname, '../client/dist')));
app.use(express.static(join(__dirname, '../public')));

// Rate limiting for API
app.use('/api', createRateLimiter(config.rateLimit));

// Auth middleware for API (except SSE which handles its own auth)
const authMiddleware = createAuthMiddleware(config.auth);

// =============================================================================
// API Routes
// =============================================================================

// Health check (no auth)
app.get('/health', (req, res) => {
  res.json({ status: 'ok', timestamp: new Date().toISOString() });
});

// SSE events (no auth - relies on dashboard session)
app.use('/api/events', sseRoutes);

// Protected API routes
app.use('/api/stats', authMiddleware, statsRoutes);
app.use('/api/daemons', authMiddleware, daemonRoutes);
app.use('/api/messages', authMiddleware, messageRoutes);
app.use('/api/kanban', authMiddleware, kanbanRoutes);
app.use('/api/knowledge', authMiddleware, createKnowledgeRoutes({ knowledgeUrl: config.knowledgeUrl }));
app.use('/api/dashboard', authMiddleware, createDashboardRoutes({ knowledgeUrl: config.knowledgeUrl }));
console.log('[SERVER] Registering /api/autonomous route...');
const autonomousRouter = createAutonomousRoutes({ knowledgeUrl: config.knowledgeUrl });
console.log('[SERVER] Autonomous router:', autonomousRouter);
app.use('/api/autonomous', authMiddleware, autonomousRouter);

// Legacy endpoint compatibility
app.get('/api/inbox/:daemon', authMiddleware, (req, res, next) => {
  req.url = `/inbox/${req.params.daemon}`;
  messageRoutes(req, res, next);
});

app.get('/api/pending-by-daemon', authMiddleware, (req, res, next) => {
  req.url = '/pending';
  messageRoutes(req, res, next);
});

// =============================================================================
// SPA Catch-all
// =============================================================================

app.get('*', (req, res) => {
  // Serve React SPA index.html for all non-API routes
  res.sendFile(join(__dirname, '../client/dist/index.html'));
});

// =============================================================================
// Error Handler
// =============================================================================

app.use((err, req, res, next) => {
  console.error('[ERROR]', err.stack);
  res.status(500).json({ error: 'Internal server error' });
});

// =============================================================================
// Start Server
// =============================================================================

app.listen(config.port, config.host, () => {
  console.log('═══════════════════════════════════════════════════════════════');
  console.log('  Datahaven Web Dashboard');
  console.log('═══════════════════════════════════════════════════════════════');
  console.log(`  URL:        http://${config.host}:${config.port}`);
  console.log(`  Database:   ${config.dbPath}`);
  console.log(`  Knowledge:  ${config.knowledgeUrl}`);
  console.log(`  Auth:       ${config.auth.enabled ? 'ENABLED' : 'disabled'}`);
  console.log('═══════════════════════════════════════════════════════════════');
});

// Start SSE polling
startPolling(5000);
