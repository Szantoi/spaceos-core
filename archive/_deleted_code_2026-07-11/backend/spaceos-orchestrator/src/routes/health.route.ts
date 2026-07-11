// src/routes/health.route.ts
import { Router } from 'express';
import axios from 'axios';
import { env } from '../config/env';

export const healthRouter = Router();

async function ping(url: string): Promise<'ok' | 'unreachable'> {
  try {
    await axios.get(url, { timeout: 3_000 });
    return 'ok';
  } catch {
    return 'unreachable';
  }
}

healthRouter.get('/', async (_req, res, next) => {
  try {
    const kernelStatus = await ping(`${env.KERNEL_BASE_URL}/healthz`);

    res.status(kernelStatus === 'ok' ? 200 : 207).json({
      orchestrator:  'ok',
      kernel:        kernelStatus,
      llmProvider:   env.LLM_PROVIDER,
      timestamp:     new Date().toISOString(),
    });
  } catch (err) {
    next(err);
  }
});
