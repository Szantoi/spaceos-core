/**
 * Routes Layer - Message Routes
 *
 * API endpoints for message operations
 */

import { Router } from 'express';
import * as messageService from '../services/messageService.js';

const router = Router();

/**
 * GET /api/messages
 * List messages with filtering
 * Query params: status, daemon, type, limit, offset
 */
router.get('/', (req, res) => {
  try {
    const { status, daemon, type, limit, offset } = req.query;
    const result = messageService.listMessages({ status, daemon, type, limit, offset });
    res.json(result);
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

/**
 * GET /api/messages/pending
 * Get pending messages grouped by daemon
 */
router.get('/pending', (req, res) => {
  try {
    const overview = messageService.getPendingOverview();
    res.json(overview);
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

/**
 * GET /api/messages/:id
 * Get single message details
 */
router.get('/:id', (req, res) => {
  try {
    const message = messageService.getMessage(req.params.id);
    if (!message) {
      return res.status(404).json({ error: 'Message not found' });
    }
    res.json(message);
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

/**
 * GET /api/messages/inbox/:daemon
 * Get pending messages for a daemon
 */
router.get('/inbox/:daemon', (req, res) => {
  try {
    const inbox = messageService.getInbox(req.params.daemon);
    res.json(inbox);
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

export default router;
