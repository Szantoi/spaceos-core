/**
 * Service Layer - Message Service
 *
 * Business logic for message operations
 */

import * as messageRepo from '../data/messageRepository.js';

/**
 * List messages with filtering
 * @param {Object} filters - Filter options
 * @returns {Object} Paginated result with messages and metadata
 */
export function listMessages(filters = {}) {
  const messages = messageRepo.getMessages(filters);
  const { limit = 50, offset = 0 } = filters;

  return {
    messages,
    pagination: {
      limit: parseInt(limit),
      offset: parseInt(offset),
      count: messages.length
    }
  };
}

/**
 * Get single message by ID
 * @param {number|string} id - Message ID
 * @returns {Object|null} Message or null
 */
export function getMessage(id) {
  return messageRepo.getMessageById(id);
}

/**
 * Get inbox for a daemon
 * @param {string} daemonId - Daemon identifier
 * @returns {Object} Inbox with messages grouped by priority
 */
export function getInbox(daemonId) {
  const messages = messageRepo.getInbox(daemonId);

  // Group by priority for dashboard display
  const grouped = {
    critical: messages.filter(m => m.priority === 'critical'),
    high: messages.filter(m => m.priority === 'high'),
    medium: messages.filter(m => m.priority === 'medium'),
    low: messages.filter(m => m.priority === 'low')
  };

  return {
    messages,
    total: messages.length,
    byPriority: grouped
  };
}

/**
 * Get pending messages grouped by daemon
 * @returns {Object} Map of daemon -> count
 */
export function getPendingOverview() {
  return messageRepo.getPendingByDaemon();
}

export default {
  listMessages,
  getMessage,
  getInbox,
  getPendingOverview
};
