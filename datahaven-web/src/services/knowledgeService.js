/**
 * Service Layer - Knowledge Service
 *
 * Business logic for knowledge service proxy
 */

/**
 * Search knowledge base
 * @param {string} query - Search query
 * @param {number} limit - Max results
 * @param {string} knowledgeUrl - Knowledge service URL
 * @returns {Promise<Object>} Search results
 */
export async function search(query, limit = 5, knowledgeUrl) {
  const response = await fetch(`${knowledgeUrl}/api/knowledge/search`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ q: query, limit: parseInt(limit) })
  });

  if (!response.ok) {
    throw new Error(`Knowledge service returned ${response.status}`);
  }

  return response.json();
}

/**
 * Check knowledge service health
 * @param {string} knowledgeUrl - Knowledge service URL
 * @returns {Promise<Object>} Health status
 */
export async function checkHealth(knowledgeUrl) {
  try {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 5000);

    const response = await fetch(`${knowledgeUrl}/health`, {
      signal: controller.signal
    });

    clearTimeout(timeoutId);

    if (response.ok) {
      const data = await response.json();
      return { status: 'ok', ...data };
    } else {
      return { status: 'error', code: response.status };
    }
  } catch (err) {
    if (err.name === 'AbortError') {
      return { status: 'offline', error: 'Timeout' };
    }
    return { status: 'offline', error: err.message };
  }
}

export default {
  search,
  checkHealth
};
