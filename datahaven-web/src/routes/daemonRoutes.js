/**
 * Routes Layer - Daemon Routes
 *
 * API endpoints for daemon management
 */

import { Router } from 'express';
import * as daemonService from '../services/daemonService.js';

const router = Router();

/**
 * GET /api/daemons
 * List all registered daemons with status
 */
router.get('/', (req, res) => {
  try {
    const daemons = daemonService.listDaemons();
    res.json(daemons);
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

/**
 * GET /api/daemons/summary
 * Get summary with online/offline counts
 */
router.get('/summary', (req, res) => {
  try {
    const summary = daemonService.getDaemonsSummary();
    res.json(summary);
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

/**
 * GET /api/daemons/:id
 * Get single daemon details
 */
router.get('/:id', (req, res) => {
  try {
    const daemon = daemonService.getDaemonDetails(req.params.id);
    if (!daemon) {
      return res.status(404).json({ error: 'Daemon not found' });
    }
    res.json(daemon);
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

export default router;
