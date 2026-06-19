/**
 * Data Access Layer - Daemon Repository
 *
 * Database operations for daemons table
 */

import { getDb } from './database.js';

/**
 * Get daemon count
 * @returns {number} Number of registered daemons
 */
export function getDaemonCount() {
  const db = getDb();
  if (!db) {
    return 0;
  }

  const result = db.prepare('SELECT COUNT(*) as count FROM daemons').get();
  return result.count || 0;
}

/**
 * Get all daemons with their pending message counts
 * @returns {Array} Array of daemon objects with online status
 */
export function getAllDaemons() {
  const db = getDb();
  if (!db) {
    return [];
  }

  const daemons = db.prepare(`
    SELECT
      d.id,
      d.description,
      d.last_heartbeat,
      (SELECT COUNT(*) FROM messages WHERE to_daemon = d.id AND status = 'pending') as pending_count
    FROM daemons d
    ORDER BY d.last_heartbeat DESC
  `).all();

  // Calculate online status (within last 5 minutes)
  const now = Date.now();
  const onlineThreshold = 5 * 60 * 1000;

  return daemons.map(d => ({
    ...d,
    online: d.last_heartbeat ? (now - new Date(d.last_heartbeat).getTime()) < onlineThreshold : false
  }));
}

/**
 * Get single daemon by ID
 * @param {string} id - Daemon identifier
 * @returns {Object|null} Daemon object or null
 */
export function getDaemonById(id) {
  const db = getDb();
  if (!db) {
    return null;
  }

  const daemon = db.prepare(`
    SELECT
      d.id,
      d.description,
      d.last_heartbeat,
      (SELECT COUNT(*) FROM messages WHERE to_daemon = d.id AND status = 'pending') as pending_count
    FROM daemons d
    WHERE d.id = ?
  `).get(id);

  if (!daemon) return null;

  const now = Date.now();
  const onlineThreshold = 5 * 60 * 1000;

  return {
    ...daemon,
    online: daemon.last_heartbeat ? (now - new Date(daemon.last_heartbeat).getTime()) < onlineThreshold : false
  };
}

export default {
  getDaemonCount,
  getAllDaemons,
  getDaemonById
};
