/**
 * Routes Layer - Knowledge Routes
 *
 * API endpoints for knowledge service proxy
 */

import { Router } from 'express';
import * as knowledgeService from '../services/knowledgeService.js';

/**
 * Create knowledge routes with config
 * @param {Object} config - Configuration
 * @param {string} config.knowledgeUrl - Knowledge service URL
 * @returns {Router} Express router
 */
export function createKnowledgeRoutes(config) {
  const router = Router();
  const { knowledgeUrl } = config;

  /**
   * GET /api/knowledge/search
   * Search knowledge base
   * Query params: q (required), limit
   */
  router.get('/search', async (req, res) => {
    const { q, limit = 5 } = req.query;

    if (!q) {
      return res.status(400).json({ error: 'Query parameter "q" required' });
    }

    try {
      const results = await knowledgeService.search(q, limit, knowledgeUrl);
      res.json(results);
    } catch (err) {
      res.status(503).json({ error: `Knowledge service unavailable: ${err.message}` });
    }
  });

  /**
   * GET /api/knowledge/health
   * Check knowledge service health
   */
  router.get('/health', async (req, res) => {
    const health = await knowledgeService.checkHealth(knowledgeUrl);
    res.json(health);
  });

  return router;
}

export default { createKnowledgeRoutes };
