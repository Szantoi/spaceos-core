// src/routes/knowledge.route.ts
import express, { Request, Response } from 'express';
import { z } from 'zod';
import { searchDocuments } from '../services/knowledgeService';

export const knowledgeRouter = express.Router();

/**
 * Request schema for POST /knowledge/search
 */
const searchRequestSchema = z.object({
  query: z.string().min(1, 'Query must not be empty'),
  limit: z.number().int().min(1).max(20).optional().default(5),
  metadata_filter: z.record(z.any()).optional(),
});

/**
 * POST /knowledge/search
 *
 * Search knowledge base using PostgreSQL full-text search
 *
 * Request body:
 *   {
 *     query: string,
 *     limit?: number (1-20, default 5),
 *     metadata_filter?: { [key: string]: any }
 *   }
 *
 * Response:
 *   {
 *     results: KnowledgeDocument[],
 *     total: number,
 *     query: string
 *   }
 */
knowledgeRouter.post('/search', async (req: Request, res: Response) => {
  try {
    // Validate request body
    const parsed = searchRequestSchema.safeParse(req.body);

    if (!parsed.success) {
      res.status(400).json({
        error: 'Invalid request',
        details: parsed.error.flatten().fieldErrors,
      });
      return;
    }

    const { query, limit, metadata_filter } = parsed.data;

    // Execute search
    const results = await searchDocuments(query, limit, metadata_filter);

    res.json({
      results,
      total: results.length,
      query,
    });
  } catch (err: any) {
    console.error('[knowledge.route] POST /search error:', err);

    // Database connection errors
    if (err.message?.includes('DATABASE_URL not configured')) {
      res.status(500).json({
        error: 'Knowledge base not configured',
        details: 'DATABASE_URL environment variable is missing',
      });
      return;
    }

    // PostgreSQL errors
    if (err.code) {
      res.status(500).json({
        error: 'Database error',
        details: err.message,
      });
      return;
    }

    // Generic server error
    res.status(500).json({
      error: 'Internal server error',
    });
  }
});

export default knowledgeRouter;
