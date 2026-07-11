// src/routes/chat.route.ts
import { Router } from 'express';
import { z } from 'zod';
import rateLimit from 'express-rate-limit';
import { requireAuth, type AuthenticatedRequest } from '../middleware/auth.middleware';
import { interpret, streamChat } from '../interpreter/interpreter.service';
import { SseSerializer } from '../interpreter/sse-serializer';
import { env } from '../config/env';
import type { ChatRequest } from '../types/llm.types';

export const chatRouter = Router();

const isProd = env.NODE_ENV === 'production';

const chatRequestSchema = z.object({
  // Discriminated union: user messages must have non-empty content (security),
  // assistant messages may be empty — the portal sometimes sends '' on first response
  // because useStreamingChat reads the JSON /bff/chat response as SSE (finds no data: lines)
  // and the assistant turn content stays ''. Fix: filter empty assistant turns in interpret().
  messages: z.array(
    z.discriminatedUnion('role', [
      z.object({ role: z.literal('user'),      content: z.string().min(1) }),
      z.object({ role: z.literal('assistant'), content: z.string() }),
    ]),
  ).min(1),
  context: z
    .object({
      tenantId:   z.string().uuid().optional(),
      facilityId: z.string().uuid().optional(),
    })
    .optional(),
});

// SSE streaming rate limit: tighter than the global chatLimiter (10/min prod)
const sseChatRateLimit = rateLimit({
  windowMs: 60 * 1000,
  max: isProd ? 10 : 100,
  standardHeaders: true,
  legacyHeaders: false,
  message: { error: 'Too many streaming requests. Please wait before starting a new stream.' },
});

/**
 * POST /bff/chat
 *
 * Body: ChatRequest
 * Returns: ChatResponse (JSON)
 *
 * Auth: Bearer JWT issued by Keycloak (OIDC PKCE flow)
 */
chatRouter.post('/', requireAuth, async (req: AuthenticatedRequest, res, next) => {
  try {
    const parsed = chatRequestSchema.safeParse(req.body);
    if (!parsed.success) {
      res.status(422).json({ error: 'Validation failed', details: parsed.error.flatten() });
      return;
    }

    const chatRequest: ChatRequest = parsed.data;
    const brand = req.headers['x-spaceos-brand'] as string | undefined;
    const result = await interpret(chatRequest, req.jwtToken!, brand);

    res.json(result);
  } catch (err) {
    next(err);
  }
});

/**
 * POST /bff/chat/stream
 *
 * Body: ChatRequest
 * Returns: Server-Sent Events stream
 *   - data: { type: 'text', text: '...' }
 *   - data: { type: 'done', toolsUsed: [...], iterations: N }
 *   - data: [DONE]  ← SSE sentinel (always last)
 *
 * Auth: Bearer JWT
 * Rate limit: 10 req/min prod (tighter than JSON endpoint — LLM streaming is expensive)
 */
chatRouter.post('/stream', requireAuth, sseChatRateLimit, async (req: AuthenticatedRequest, res) => {
  const parsed = chatRequestSchema.safeParse(req.body);
  if (!parsed.success) {
    res.status(422).json({ error: 'Validation failed', details: parsed.error.flatten() });
    return;
  }

  // SSE headers — must be sent before any data
  res.setHeader('Content-Type', 'text/event-stream');
  res.setHeader('Cache-Control', 'no-cache');
  res.setHeader('Connection', 'keep-alive');
  res.flushHeaders();

  // BE-P2-05: AbortController — client disconnect cleanly stops the generator
  const abortController = new AbortController();
  req.on('close', () => {
    abortController.abort();
  });

  try {
    const chatRequest: ChatRequest = parsed.data;
    const brand = req.headers['x-spaceos-brand'] as string | undefined;

    for await (const chunk of streamChat(chatRequest, req.jwtToken!, abortController.signal, brand)) {
      if (abortController.signal.aborted) break;
      SseSerializer.write(res, chunk);
    }
  } catch (err) {
    if ((err as Error).name !== 'AbortError') {
      // Unexpected error — send an error chunk before closing
      SseSerializer.write(res, { type: 'error', error: 'stream_error' });
    }
    // AbortError: client disconnected — silent, expected
  } finally {
    res.write('data: [DONE]\n\n');
    res.end();
  }
});
