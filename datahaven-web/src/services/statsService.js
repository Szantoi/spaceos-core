/**
 * Service Layer - Stats Service
 *
 * Business logic for dashboard statistics
 */

import * as messageRepo from '../data/messageRepository.js';
import * as daemonRepo from '../data/daemonRepository.js';

/**
 * Get complete dashboard statistics
 * @returns {Object} Combined stats for dashboard
 */
export function getDashboardStats() {
  const messageStats = messageRepo.getStats();
  const daemonCount = daemonRepo.getDaemonCount();

  return {
    ...messageStats,
    daemons: daemonCount
  };
}

/**
 * Get stats for a specific daemon
 * @param {string} daemonId - Daemon identifier
 * @returns {Object} Daemon-specific stats
 */
export function getDaemonStats(daemonId) {
  const daemon = daemonRepo.getDaemonById(daemonId);
  if (!daemon) {
    return null;
  }

  const inbox = messageRepo.getInbox(daemonId);

  return {
    daemon,
    pendingMessages: inbox.length,
    criticalCount: inbox.filter(m => m.priority === 'critical').length,
    highCount: inbox.filter(m => m.priority === 'high').length
  };
}

export default {
  getDashboardStats,
  getDaemonStats
};
