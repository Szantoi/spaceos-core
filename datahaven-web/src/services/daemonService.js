/**
 * Service Layer - Daemon Service
 *
 * Business logic for daemon operations
 */

import * as daemonRepo from '../data/daemonRepository.js';
import * as messageRepo from '../data/messageRepository.js';

/**
 * List all daemons with status
 * @returns {Array} Array of daemon objects
 */
export function listDaemons() {
  return daemonRepo.getAllDaemons();
}

/**
 * Get daemon details with stats
 * @param {string} daemonId - Daemon identifier
 * @returns {Object|null} Daemon details or null
 */
export function getDaemonDetails(daemonId) {
  const daemon = daemonRepo.getDaemonById(daemonId);
  if (!daemon) {
    return null;
  }

  const inbox = messageRepo.getInbox(daemonId);

  return {
    ...daemon,
    inbox: {
      total: inbox.length,
      critical: inbox.filter(m => m.priority === 'critical').length,
      high: inbox.filter(m => m.priority === 'high').length
    }
  };
}

/**
 * Get dashboard summary for all daemons
 * @returns {Object} Summary with online/offline counts
 */
export function getDaemonsSummary() {
  const daemons = daemonRepo.getAllDaemons();

  return {
    total: daemons.length,
    online: daemons.filter(d => d.online).length,
    offline: daemons.filter(d => !d.online).length,
    daemons
  };
}

export default {
  listDaemons,
  getDaemonDetails,
  getDaemonsSummary
};
