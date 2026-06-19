/**
 * Data Access Layer - Message Repository
 *
 * Database operations for messages table
 */

import { getDb } from './database.js';

/**
 * Get overall queue statistics
 * @returns {Object} Stats object with total, pending, acked counts
 */
export function getStats() {
  const db = getDb();
  if (!db) {
    return { total: 0, pending: 0, acked: 0 };
  }

  const stats = db.prepare(`
    SELECT
      COUNT(*) as total,
      SUM(CASE WHEN status = 'pending' THEN 1 ELSE 0 END) as pending,
      SUM(CASE WHEN status = 'acked' THEN 1 ELSE 0 END) as acked
    FROM messages
  `).get();

  return {
    total: stats.total || 0,
    pending: stats.pending || 0,
    acked: stats.acked || 0
  };
}

/**
 * Get messages with filtering
 * @param {Object} filters - Filter options
 * @param {string} filters.status - Filter by status
 * @param {string} filters.daemon - Filter by daemon (from or to)
 * @param {string} filters.type - Filter by message type
 * @param {number} filters.limit - Max results (default 50)
 * @param {number} filters.offset - Offset for pagination (default 0)
 * @returns {Array} Array of message objects
 */
export function getMessages({ status, daemon, type, limit = 50, offset = 0 } = {}) {
  const db = getDb();
  if (!db) {
    return [];
  }

  let sql = 'SELECT * FROM messages WHERE 1=1';
  const params = [];

  if (status) {
    sql += ' AND status = ?';
    params.push(status);
  }
  if (daemon) {
    sql += ' AND (from_daemon = ? OR to_daemon = ?)';
    params.push(daemon, daemon);
  }
  if (type) {
    sql += ' AND msg_type = ?';
    params.push(type);
  }

  sql += ' ORDER BY created_at DESC LIMIT ? OFFSET ?';
  params.push(parseInt(limit), parseInt(offset));

  return db.prepare(sql).all(...params);
}

/**
 * Get single message by ID
 * @param {number|string} id - Message ID
 * @returns {Object|null} Message object or null
 */
export function getMessageById(id) {
  const db = getDb();
  if (!db) {
    return null;
  }

  return db.prepare('SELECT * FROM messages WHERE id = ?').get(id);
}

/**
 * Get pending messages for a specific daemon (inbox)
 * @param {string} daemonId - Daemon identifier
 * @returns {Array} Array of pending messages sorted by priority
 */
export function getInbox(daemonId) {
  const db = getDb();
  if (!db) {
    return [];
  }

  return db.prepare(`
    SELECT * FROM messages
    WHERE to_daemon = ? AND status = 'pending'
    ORDER BY
      CASE priority
        WHEN 'critical' THEN 1
        WHEN 'high' THEN 2
        WHEN 'medium' THEN 3
        ELSE 4
      END,
      created_at ASC
  `).all(daemonId);
}

/**
 * Get pending message counts grouped by daemon
 * @returns {Object} Map of daemon_id -> pending count
 */
export function getPendingByDaemon() {
  const db = getDb();
  if (!db) {
    return {};
  }

  const rows = db.prepare(`
    SELECT to_daemon, COUNT(*) as count
    FROM messages
    WHERE status = 'pending'
    GROUP BY to_daemon
  `).all();

  const result = {};
  rows.forEach(r => { result[r.to_daemon] = r.count; });
  return result;
}

export default {
  getStats,
  getMessages,
  getMessageById,
  getInbox,
  getPendingByDaemon
};
