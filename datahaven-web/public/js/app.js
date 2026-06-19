/**
 * Datahaven Dashboard Frontend
 */

// =============================================================================
// State & Config
// =============================================================================

const state = {
  token: localStorage.getItem('datahaven_token') || '',
  authRequired: false,
  connected: false
};

// =============================================================================
// API Client
// =============================================================================

async function api(endpoint, options = {}) {
  const headers = { ...options.headers };

  if (state.token) {
    headers['Authorization'] = `Bearer ${state.token}`;
  }

  const response = await fetch(`/api${endpoint}`, { ...options, headers });

  if (response.status === 401 || response.status === 403) {
    state.authRequired = true;
    showAuthOverlay();
    throw new Error('Authentication required');
  }

  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: 'Request failed' }));
    throw new Error(error.error || 'Request failed');
  }

  return response.json();
}

// =============================================================================
// Auth
// =============================================================================

function showAuthOverlay() {
  document.getElementById('auth-overlay').classList.remove('hidden');
}

function hideAuthOverlay() {
  document.getElementById('auth-overlay').classList.add('hidden');
}

document.getElementById('auth-submit')?.addEventListener('click', async () => {
  const token = document.getElementById('auth-token').value;
  const errorEl = document.getElementById('auth-error');

  if (!token) {
    errorEl.textContent = 'Please enter a token';
    errorEl.classList.remove('hidden');
    return;
  }

  state.token = token;
  localStorage.setItem('datahaven_token', token);

  try {
    await api('/stats');
    hideAuthOverlay();
    state.authRequired = false;
    init();
  } catch (err) {
    errorEl.textContent = 'Invalid token';
    errorEl.classList.remove('hidden');
    state.token = '';
    localStorage.removeItem('datahaven_token');
  }
});

document.getElementById('auth-token')?.addEventListener('keypress', (e) => {
  if (e.key === 'Enter') {
    document.getElementById('auth-submit').click();
  }
});

// =============================================================================
// SSE Connection
// =============================================================================

let eventSource = null;

function connectSSE() {
  if (eventSource) {
    eventSource.close();
  }

  const url = state.token ? `/api/events?token=${state.token}` : '/api/events';
  eventSource = new EventSource(url);

  eventSource.onopen = () => {
    state.connected = true;
    updateConnectionStatus(true);
  };

  eventSource.onmessage = (event) => {
    try {
      const data = JSON.parse(event.data);
      handleSSEMessage(data);
    } catch (err) {
      console.error('SSE parse error:', err);
    }
  };

  eventSource.onerror = () => {
    state.connected = false;
    updateConnectionStatus(false);

    // Reconnect after 5 seconds
    setTimeout(connectSSE, 5000);
  };
}

function handleSSEMessage(data) {
  if (data.type === 'stats' || data.type === 'update') {
    loadStats();
  }
}

function updateConnectionStatus(online) {
  const el = document.getElementById('connection-status');
  if (online) {
    el.classList.remove('offline');
    el.classList.add('online');
    el.querySelector('.status-text').textContent = 'Connected';
  } else {
    el.classList.remove('online');
    el.classList.add('offline');
    el.querySelector('.status-text').textContent = 'Disconnected';
  }
}

// =============================================================================
// Stats
// =============================================================================

async function loadStats() {
  try {
    const stats = await api('/stats');

    document.getElementById('stat-total').textContent = stats.total;
    document.getElementById('stat-pending').textContent = stats.pending;
    document.getElementById('stat-acked').textContent = stats.acked;
    document.getElementById('stat-daemons').textContent = stats.daemons;
  } catch (err) {
    console.error('Failed to load stats:', err);
  }
}

// =============================================================================
// Daemons
// =============================================================================

async function loadDaemons() {
  const container = document.getElementById('daemons-list');

  try {
    const daemons = await api('/daemons');

    if (daemons.length === 0) {
      container.innerHTML = '<div class="empty">No daemons registered</div>';
      return;
    }

    container.innerHTML = daemons.map(d => `
      <div class="daemon-item">
        <div class="daemon-info">
          <div class="daemon-status ${d.online ? 'online' : ''}"></div>
          <div>
            <div class="daemon-name">${escapeHtml(d.id)}</div>
            <div class="daemon-description">${escapeHtml(d.description || 'No description')}</div>
          </div>
        </div>
        ${d.pending_count > 0 ? `<div class="daemon-badge">${d.pending_count}</div>` : ''}
      </div>
    `).join('');
  } catch (err) {
    container.innerHTML = `<div class="error">Failed to load daemons: ${escapeHtml(err.message)}</div>`;
  }
}

// =============================================================================
// Messages
// =============================================================================

async function loadMessages() {
  const container = document.getElementById('messages-list');
  const status = document.getElementById('filter-status').value;

  try {
    const params = new URLSearchParams();
    if (status) params.append('status', status);
    params.append('limit', '20');

    const result = await api(`/messages?${params}`);
    const messages = result.messages || result;

    if (messages.length === 0) {
      container.innerHTML = '<div class="empty">No messages found</div>';
      return;
    }

    container.innerHTML = messages.map(m => `
      <div class="message-item" onclick="showMessage(${m.id})">
        <div class="message-header">
          <span class="message-subject">${escapeHtml(m.subject || 'No subject')}</span>
          <span class="message-priority priority-${m.priority || 'medium'}">${m.priority || 'medium'}</span>
        </div>
        <div class="message-meta">
          <span>${escapeHtml(m.from_daemon)} → ${escapeHtml(m.to_daemon)}</span>
          <span class="message-status status-${m.status}">${m.status}</span>
          <span>${formatDate(m.created_at)}</span>
        </div>
      </div>
    `).join('');
  } catch (err) {
    container.innerHTML = `<div class="error">Failed to load messages: ${escapeHtml(err.message)}</div>`;
  }
}

async function showMessage(id) {
  const modal = document.getElementById('message-modal');
  const body = document.getElementById('message-detail');

  try {
    const msg = await api(`/messages/${id}`);

    body.innerHTML = `
      <div class="detail-row">
        <div class="detail-label">ID</div>
        <div class="detail-value">${msg.id}</div>
      </div>
      <div class="detail-row">
        <div class="detail-label">From</div>
        <div class="detail-value">${escapeHtml(msg.from_daemon)}</div>
      </div>
      <div class="detail-row">
        <div class="detail-label">To</div>
        <div class="detail-value">${escapeHtml(msg.to_daemon)}</div>
      </div>
      <div class="detail-row">
        <div class="detail-label">Type</div>
        <div class="detail-value">${escapeHtml(msg.msg_type || '-')}</div>
      </div>
      <div class="detail-row">
        <div class="detail-label">Subject</div>
        <div class="detail-value">${escapeHtml(msg.subject || '-')}</div>
      </div>
      <div class="detail-row">
        <div class="detail-label">Priority</div>
        <div class="detail-value">
          <span class="message-priority priority-${msg.priority || 'medium'}">${msg.priority || 'medium'}</span>
        </div>
      </div>
      <div class="detail-row">
        <div class="detail-label">Status</div>
        <div class="detail-value">
          <span class="message-status status-${msg.status}">${msg.status}</span>
        </div>
      </div>
      <div class="detail-row">
        <div class="detail-label">Created</div>
        <div class="detail-value">${formatDate(msg.created_at)}</div>
      </div>
      ${msg.payload ? `
        <div class="detail-row">
          <div class="detail-label">Payload</div>
          <div class="detail-value">
            <pre class="detail-payload">${escapeHtml(JSON.stringify(JSON.parse(msg.payload), null, 2))}</pre>
          </div>
        </div>
      ` : ''}
    `;

    modal.classList.remove('hidden');
  } catch (err) {
    body.innerHTML = `<div class="error">Failed to load message: ${escapeHtml(err.message)}</div>`;
    modal.classList.remove('hidden');
  }
}

function closeModal() {
  document.getElementById('message-modal').classList.add('hidden');
}

// Close modal on escape key
document.addEventListener('keydown', (e) => {
  if (e.key === 'Escape') {
    closeModal();
  }
});

// Close modal on backdrop click
document.getElementById('message-modal')?.addEventListener('click', (e) => {
  if (e.target.classList.contains('modal')) {
    closeModal();
  }
});

// =============================================================================
// Knowledge Search
// =============================================================================

async function checkKnowledgeHealth() {
  const el = document.getElementById('knowledge-status');

  try {
    const health = await api('/knowledge/health');

    if (health.status === 'ok') {
      el.classList.remove('offline');
      el.classList.add('online');
      el.querySelector('.status-text').textContent = `Online (${health.doc_count || 0} docs)`;
    } else {
      el.classList.remove('online');
      el.classList.add('offline');
      el.querySelector('.status-text').textContent = 'Offline';
    }
  } catch (err) {
    el.classList.remove('online');
    el.classList.add('offline');
    el.querySelector('.status-text').textContent = 'Error';
  }
}

async function searchKnowledge() {
  const query = document.getElementById('knowledge-query').value.trim();
  const container = document.getElementById('knowledge-results');

  if (!query) {
    container.innerHTML = '<p class="hint">Enter a query to search the knowledge base.</p>';
    return;
  }

  container.innerHTML = '<div class="loading">Searching...</div>';

  try {
    const result = await api(`/knowledge/search?q=${encodeURIComponent(query)}&limit=5`);
    const results = result.results || [];

    if (results.length === 0) {
      container.innerHTML = '<div class="empty">No results found</div>';
      return;
    }

    container.innerHTML = results.map(r => `
      <div class="knowledge-result">
        <div class="knowledge-source">${escapeHtml(r.source || 'Unknown source')}</div>
        <div class="knowledge-content">${escapeHtml(truncate(r.content, 300))}</div>
        <div class="knowledge-score">Score: ${(r.score * 100).toFixed(1)}%</div>
      </div>
    `).join('');
  } catch (err) {
    container.innerHTML = `<div class="error">Search failed: ${escapeHtml(err.message)}</div>`;
  }
}

// Search on Enter key
document.getElementById('knowledge-query')?.addEventListener('keypress', (e) => {
  if (e.key === 'Enter') {
    searchKnowledge();
  }
});

// =============================================================================
// Utilities
// =============================================================================

function escapeHtml(str) {
  if (!str) return '';
  const div = document.createElement('div');
  div.textContent = str;
  return div.innerHTML;
}

function formatDate(dateStr) {
  if (!dateStr) return '-';
  const date = new Date(dateStr);
  return date.toLocaleString('hu-HU', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  });
}

function truncate(str, maxLength) {
  if (!str) return '';
  if (str.length <= maxLength) return str;
  return str.substring(0, maxLength) + '...';
}

// =============================================================================
// Initialize
// =============================================================================

async function init() {
  try {
    // Test API access
    await api('/stats');

    // Load initial data
    await Promise.all([
      loadStats(),
      loadDaemons(),
      loadMessages(),
      checkKnowledgeHealth()
    ]);

    // Connect SSE
    connectSSE();

    // Refresh periodically
    setInterval(loadStats, 30000);
    setInterval(loadDaemons, 60000);
    setInterval(checkKnowledgeHealth, 60000);

  } catch (err) {
    if (!state.authRequired) {
      console.error('Init failed:', err);
    }
  }
}

// Start
document.addEventListener('DOMContentLoaded', init);
