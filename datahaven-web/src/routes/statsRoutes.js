/**
 * Routes Layer - Stats Routes
 *
 * API endpoints for dashboard statistics
 */

import { Router } from 'express';
import * as statsService from '../services/statsService.js';

const router = Router();

/**
 * GET /api/stats
 * Overall queue statistics
 */
router.get('/', (req, res) => {
  try {
    const stats = statsService.getDashboardStats();
    res.json(stats);
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

/**
 * GET /api/stats/daemon/:id
 * Stats for specific daemon
 */
router.get('/daemon/:id', (req, res) => {
  try {
    const stats = statsService.getDaemonStats(req.params.id);
    if (!stats) {
      return res.status(404).json({ error: 'Daemon not found' });
    }
    res.json(stats);
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

export default router;
