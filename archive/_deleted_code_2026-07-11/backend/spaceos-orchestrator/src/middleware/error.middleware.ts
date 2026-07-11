// src/middleware/error.middleware.ts
import type { Request, Response, NextFunction } from 'express';
import { env } from '../config/env';

export function errorHandler(
  err: Error,
  _req: Request,
  res: Response,
  _next: NextFunction,
): void {
  console.error('[ErrorHandler]', err.message, err.stack);
  const detail = env.NODE_ENV === 'production' ? undefined : err.message;
  res.status(500).json({
    error: 'Internal orchestrator error.',
    ...(detail ? { message: detail } : {}),
  });
}
