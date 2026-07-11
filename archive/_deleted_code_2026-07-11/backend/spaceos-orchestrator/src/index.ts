// src/index.ts
import 'dotenv/config';
import express from 'express';
import helmet from 'helmet';
import cors from 'cors';
import morgan from 'morgan';
import rateLimit from 'express-rate-limit';

import { env } from './config/env';
import { chatRouter } from './routes/chat.route';
import { healthRouter } from './routes/health.route';
import { testRouter } from './routes/test.route';
import { proxyRouter, identityProxyRouter } from './routes/proxy.route';
import { knowledgeRouter } from './routes/knowledge.route';
import './middleware/testGuard'; // SEC-TS-02: startup WARNING log if test endpoints enabled
import { errorHandler } from './middleware/error.middleware';
import { requireAuth } from './middleware/auth.middleware';

const app = express();

// --- Security ----------------------------------------------------------------
app.use(helmet());
app.use(cors({
  origin: env.CORS_ORIGINS.split(',').map(s => s.trim()),
  credentials: true,
}));

// --- Logging -----------------------------------------------------------------
app.use(morgan(env.NODE_ENV === 'production' ? 'combined' : 'dev'));

// --- Rate limiting -----------------------------------------------------------

const isProd = env.NODE_ENV === 'production';

// Chat endpoint: tighter limit (LLM calls are expensive)
const chatLimiter = rateLimit({
  windowMs: 60 * 1000,
  max: isProd ? 20 : 200,
  standardHeaders: true,
  legacyHeaders: false,
  message: { error: 'Too many requests. Please wait before sending another message.' },
});

// --- Body parsing ------------------------------------------------------------
app.use(express.json({ limit: '512kb' }));

// --- Routes ------------------------------------------------------------------

// Health — no auth, no rate limit
app.use('/bff/health', healthRouter);

// Chat — auth first, then rate limit (auth must fire before chatLimiter to ensure
// unauthenticated requests get 401, not 429)
app.use('/bff/chat', requireAuth, chatLimiter, chatRouter);

// Test endpoints (BE-TEST-01) — guarded by testGuard (3-layer sec)
app.use('/bff/test', testRouter);

// Proxy routes for Joinery and Cutting APIs (no auth required for now)
app.use('/api', proxyRouter);

// Identity proxy routes (separate mount point)
app.use('/identity', identityProxyRouter);

// Knowledge base search (RAG) — PostgreSQL FTS
app.use('/knowledge', knowledgeRouter);

// --- Error handler (must be last) --------------------------------------------
app.use(errorHandler);

// --- Start -------------------------------------------------------------------
const PORT = parseInt(env.PORT, 10);

app.listen(PORT, '127.0.0.1', () => {
  console.log(`SpaceOS Orchestrator running on http://127.0.0.1:${PORT}`);
  console.log(`    LLM provider : ${env.LLM_PROVIDER}`);
  console.log(`    Kernel URL   : ${env.KERNEL_BASE_URL}`);
  console.log(`    Environment  : ${env.NODE_ENV}`);
});

export default app;
