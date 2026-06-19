/**
 * Routes Layer - SSE Routes
 *
 * Server-Sent Events for real-time updates
 */

import { Router } from 'express';
import * as statsService from '../services/statsService.js';
import { getDb } from '../data/database.js';

const router = Router();
const clients = new Set();

/**
 * GET /api/events
 * SSE endpoint for real-time updates
 */
router.get('/', (req, res) => {
  res.setHeader('Content-Type', 'text/event-stream');
  res.setHeader('Cache-Control', 'no-cache');
  res.setHeader('Connection', 'keep-alive');

  clients.add(res);
  console.log(`[SSE] Client connected (total: ${clients.size})`);

  // Send initial stats
  try {
    const stats = statsService.getDashboardStats();
    res.write(`data: ${JSON.stringify({ type: 'stats', ...stats })}\n\n`);
  } catch (err) {
    // Ignore initial stats error
  }

  req.on('close', () => {
    clients.delete(res);
    console.log(`[SSE] Client disconnected (total: ${clients.size})`);
  });
});

/**
 * Broadcast message to all SSE clients
 * @param {Object} data - Data to broadcast
 */
export function broadcast(data) {
  const message = `data: ${JSON.stringify(data)}\n\n`;
  clients.forEach(client => {
    try {
      client.write(message);
    } catch (err) {
      // Remove dead client
      clients.delete(client);
    }
  });
}

/**
 * Get number of connected SSE clients
 * @returns {number}
 */
export function getClientCount() {
  return clients.size;
}

/**
 * Start polling for changes
 * @param {number} intervalMs - Polling interval in milliseconds
 */
export function startPolling(intervalMs = 5000) {
  let lastTotal = 0;

  setInterval(() => {
    const db = getDb();
    if (!db || clients.size === 0) return;

    try {
      const stats = db.prepare('SELECT COUNT(*) as total FROM messages').get();
      if (stats.total !== lastTotal) {
        lastTotal = stats.total;
        broadcast({ type: 'update', total: stats.total });
      }
    } catch (err) {
      // Ignore polling errors
    }
  }, intervalMs);
}

export default router;
