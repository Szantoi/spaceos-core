/**
 * Planning Pipeline Frontend
 */

// =============================================================================
// State & Config
// =============================================================================

function getToken() {
  const urlParams = new URLSearchParams(window.location.search);
  const urlToken = urlParams.get('token');
  if (urlToken) {
    localStorage.setItem('datahaven_token', urlToken);
    window.history.replaceState({}, document.title, window.location.pathname);
    return urlToken;
  }
  return localStorage.getItem('datahaven_token') || '';
}

const state = {
  token: getToken(),
  currentStage: 'ideas',
  pipeline: null
};

// =============================================================================
// API Client
// =============================================================================

async function api(endpoint) {
  const headers = {};
  if (state.token) {
    headers['Authorization'] = `Bearer ${state.token}`;
  }

  const response = await fetch(`/api${endpoint}`, { headers });

  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: 'Request failed' }));
    throw new Error(error.error || 'Request failed');
  }

  return response.json();
}

// =============================================================================
// Pipeline Overview
// =============================================================================

async function loadPipeline() {
  try {
    const pipeline = await api('/planning/pipeline');
    state.pipeline = pipeline;

    // Update counts
    document.getElementById('count-ideas').textContent = pipeline.totals.ideas;
    document.getElementById('count-selected').textContent = pipeline.totals.selected;
    document.getElementById('count-debate').textContent = pipeline.totals.debate;
    document.getElementById('count-queue').textContent = pipeline.totals.queue;

    // Load metrics for health indicator
    const metrics = await api('/planning/metrics');
    updateHealthIndicator(metrics);

  } catch (err) {
    console.error('Failed to load pipeline:', err);
  }
}

function updateHealthIndicator(metrics) {
  const el = document.getElementById('pipeline-health');
  const health = metrics.pipeline_health || 'unknown';

  el.className = 'pipeline-health ' + health;
  el.querySelector('.health-text').textContent =
    health === 'healthy' ? 'Pipeline Healthy' :
    health === 'degraded' ? 'Pipeline Degraded' : 'Unknown';
}

// =============================================================================
// Workflow Panel (Plan → Project/Epic tracking)
// =============================================================================

async function loadWorkflow() {
  const tbody = document.getElementById('workflow-body');
  const statsEl = document.getElementById('workflow-stats');

  tbody.innerHTML = '<tr><td colspan="6" class="loading">Loading workflow...</td></tr>';

  try {
    const data = await api('/planning/workflow');

    // Render stats
    const stats = data.stats;
    statsEl.innerHTML = `
      <div class="workflow-stat draft">
        <span class="stat-count">${stats.draft}</span>
        <span class="stat-label">Draft</span>
      </div>
      <div class="workflow-stat selected">
        <span class="stat-count">${stats.selected}</span>
        <span class="stat-label">Selected</span>
      </div>
      <div class="workflow-stat in_debate">
        <span class="stat-count">${stats.in_debate}</span>
        <span class="stat-label">In Debate</span>
      </div>
      <div class="workflow-stat approved">
        <span class="stat-count">${stats.approved}</span>
        <span class="stat-label">Approved</span>
      </div>
      <div class="workflow-stat implemented">
        <span class="stat-count">${stats.implemented}</span>
        <span class="stat-label">Implemented</span>
      </div>
    `;

    // Render table rows
    if (data.items.length === 0) {
      tbody.innerHTML = '<tr><td colspan="6" class="empty">No workflow items</td></tr>';
      return;
    }

    tbody.innerHTML = data.items.map(item => `
      <tr class="workflow-row status-${item.workflowStatus}" onclick="showItem('${escapeHtml(item.path)}', '${item.workflowStage}')">
        <td class="workflow-title">
          <span class="item-title">${escapeHtml(item.title)}</span>
          <span class="item-filename">${escapeHtml(item.filename)}</span>
        </td>
        <td>
          <span class="workflow-status-badge ${item.workflowStatus}">${getStatusLabel(item.workflowStatus)}</span>
        </td>
        <td>
          ${item.priority ? `<span class="badge badge-priority-${item.priority}">${escapeHtml(item.priority)}</span>` : '-'}
        </td>
        <td>
          ${item.domain ? `<span class="badge badge-domain">${escapeHtml(item.domain)}</span>` : '-'}
        </td>
        <td class="workflow-outcome">
          ${renderOutcome(item)}
        </td>
        <td class="workflow-date">${formatDate(item.updated)}</td>
      </tr>
    `).join('');

  } catch (err) {
    tbody.innerHTML = `<tr><td colspan="6" class="empty">Error: ${escapeHtml(err.message)}</td></tr>`;
  }
}

function getStatusLabel(status) {
  const labels = {
    'draft': 'Draft',
    'selected': 'Selected',
    'in_debate': 'In Debate',
    'approved': 'Approved',
    'implemented': 'Implemented',
    'archived': 'Archived'
  };
  return labels[status] || status;
}

function renderOutcome(item) {
  if (!item.outcome && !item.outcomePath) {
    if (item.workflowStatus === 'implemented') {
      return '<span class="outcome-pending">Link pending</span>';
    }
    return '-';
  }

  const type = item.outcomeType || 'project';
  const icon = type === 'epic' ? '📦' : type === 'project' ? '📁' : '✅';
  const path = item.outcomePath || '';

  return `
    <a href="/projects.html#${escapeHtml(path)}" class="outcome-link" onclick="event.stopPropagation()">
      ${icon} ${escapeHtml(item.outcome || path)}
    </a>
  `;
}

// =============================================================================
// Stage Panels
// =============================================================================

async function loadIdeas() {
  const container = document.getElementById('ideas-list');
  container.innerHTML = '<div class="loading">Loading ideas...</div>';

  try {
    const data = await api('/planning/ideas');
    renderItems(container, data.items, 'ideas');
  } catch (err) {
    container.innerHTML = `<div class="empty">Error: ${escapeHtml(err.message)}</div>`;
  }
}

async function loadSelected() {
  const container = document.getElementById('selected-list');
  container.innerHTML = '<div class="loading">Loading...</div>';

  try {
    const data = await api('/planning/selected');
    if (data.items.length === 0) {
      container.innerHTML = '<div class="empty">No current selection (pending.md)</div>';
    } else {
      renderItems(container, data.items, 'selected');
    }
  } catch (err) {
    container.innerHTML = `<div class="empty">Error: ${escapeHtml(err.message)}</div>`;
  }
}

async function loadDebate() {
  const container = document.getElementById('debate-list');
  container.innerHTML = '<div class="loading">Loading...</div>';

  try {
    const data = await api('/planning/debate');
    if (data.items.length === 0) {
      container.innerHTML = '<div class="empty">No active debates in consensus/</div>';
    } else {
      renderItems(container, data.items, 'debate');
    }
  } catch (err) {
    container.innerHTML = `<div class="empty">Error: ${escapeHtml(err.message)}</div>`;
  }
}

async function loadQueue() {
  const container = document.getElementById('queue-list');
  container.innerHTML = '<div class="loading">Loading...</div>';

  try {
    const data = await api('/planning/queue');
    if (data.items.length === 0) {
      container.innerHTML = '<div class="empty">Queue is empty - ready for new items</div>';
    } else {
      renderItems(container, data.items, 'queue');
    }
  } catch (err) {
    container.innerHTML = `<div class="empty">Error: ${escapeHtml(err.message)}</div>`;
  }
}

async function loadLogs() {
  const container = document.getElementById('logs-list');
  container.innerHTML = '<div class="loading">Loading logs...</div>';

  try {
    const data = await api('/planning/logs?lines=100');

    if (data.lines.length === 0) {
      container.innerHTML = '<div class="empty">No log entries</div>';
      return;
    }

    container.innerHTML = data.lines.map(line => `
      <div class="log-line ${line.level}">
        <span class="log-timestamp">${line.timestamp || ''}</span>
        <span class="log-message">${escapeHtml(line.message)}</span>
      </div>
    `).join('');

    // Scroll to bottom
    container.scrollTop = container.scrollHeight;

  } catch (err) {
    container.innerHTML = `<div class="empty">Error: ${escapeHtml(err.message)}</div>`;
  }
}

// =============================================================================
// Render Items
// =============================================================================

function renderItems(container, items, stage) {
  if (items.length === 0) {
    container.innerHTML = '<div class="empty">No items</div>';
    return;
  }

  container.innerHTML = items.map(item => `
    <div class="planning-item" onclick="showItem('${escapeHtml(item.path)}', '${stage}')">
      <div class="item-header">
        <div class="item-title">${escapeHtml(item.title)}</div>
        <div class="item-badges">
          ${item.domain ? `<span class="badge badge-domain">${escapeHtml(item.domain)}</span>` : ''}
          ${item.segment ? `<span class="badge badge-segment">${escapeHtml(item.segment)}</span>` : ''}
          ${item.priority ? `<span class="badge badge-priority-${item.priority}">${escapeHtml(item.priority)}</span>` : ''}
        </div>
      </div>
      <div class="item-preview">${escapeHtml(item.preview || '')}</div>
      <div class="item-meta">
        <span>${escapeHtml(item.filename)}</span>
        <span>${formatDate(item.updated)}</span>
      </div>
    </div>
  `).join('');
}

// =============================================================================
// Item Detail Modal
// =============================================================================

function showItem(path, stage) {
  const modal = document.getElementById('item-modal');
  const title = document.getElementById('modal-title');
  const body = document.getElementById('modal-body');

  // Find item from state
  const items = state.pipeline?.stages?.[stage]?.items || [];
  const item = items.find(i => i.path === path);

  if (!item) {
    body.innerHTML = '<div class="empty">Item not found</div>';
    modal.classList.remove('hidden');
    return;
  }

  title.textContent = item.title;
  body.innerHTML = `
    <div class="detail-row">
      <div class="detail-label">File</div>
      <div class="detail-value">${escapeHtml(item.filename)}</div>
    </div>
    <div class="detail-row">
      <div class="detail-label">Path</div>
      <div class="detail-value">${escapeHtml(item.path)}</div>
    </div>
    ${item.domain ? `
    <div class="detail-row">
      <div class="detail-label">Domain</div>
      <div class="detail-value"><span class="badge badge-domain">${escapeHtml(item.domain)}</span></div>
    </div>
    ` : ''}
    ${item.segment ? `
    <div class="detail-row">
      <div class="detail-label">Segment</div>
      <div class="detail-value"><span class="badge badge-segment">${escapeHtml(item.segment)}</span></div>
    </div>
    ` : ''}
    ${item.type ? `
    <div class="detail-row">
      <div class="detail-label">Type</div>
      <div class="detail-value">${escapeHtml(item.type)}</div>
    </div>
    ` : ''}
    <div class="detail-row">
      <div class="detail-label">Priority</div>
      <div class="detail-value"><span class="badge badge-priority-${item.priority}">${escapeHtml(item.priority)}</span></div>
    </div>
    <div class="detail-row">
      <div class="detail-label">Created</div>
      <div class="detail-value">${escapeHtml(item.created)}</div>
    </div>
    <div class="detail-row">
      <div class="detail-label">Updated</div>
      <div class="detail-value">${formatDate(item.updated)}</div>
    </div>
    ${item.preview ? `
    <div class="detail-row">
      <div class="detail-label">Preview</div>
      <div class="detail-value">
        <div class="detail-preview">${escapeHtml(item.preview)}</div>
      </div>
    </div>
    ` : ''}
  `;

  modal.classList.remove('hidden');
}

function closeModal() {
  document.getElementById('item-modal').classList.add('hidden');
}

// =============================================================================
// Tab Switching
// =============================================================================

function switchStage(stage) {
  state.currentStage = stage;

  // Update tab active state
  document.querySelectorAll('.stage-tab').forEach(tab => {
    tab.classList.toggle('active', tab.dataset.stage === stage);
  });

  // Update pipeline diagram active state
  document.querySelectorAll('.pipeline-stage').forEach(el => {
    el.classList.toggle('active', el.dataset.stage === stage);
  });

  // Update panel visibility
  document.querySelectorAll('.stage-panel').forEach(panel => {
    panel.classList.toggle('active', panel.id === `panel-${stage}`);
  });

  // Load data for the stage
  switch (stage) {
    case 'workflow': loadWorkflow(); break;
    case 'ideas': loadIdeas(); break;
    case 'selected': loadSelected(); break;
    case 'debate': loadDebate(); break;
    case 'queue': loadQueue(); break;
    case 'logs': loadLogs(); break;
  }
}

// =============================================================================
// Event Listeners
// =============================================================================

// Stage tabs
document.querySelectorAll('.stage-tab').forEach(tab => {
  tab.addEventListener('click', () => switchStage(tab.dataset.stage));
});

// Pipeline diagram stages
document.querySelectorAll('.pipeline-stage').forEach(el => {
  el.addEventListener('click', () => switchStage(el.dataset.stage));
});

// Close modal on escape
document.addEventListener('keydown', (e) => {
  if (e.key === 'Escape') closeModal();
});

// Close modal on backdrop click
document.getElementById('item-modal')?.addEventListener('click', (e) => {
  if (e.target.classList.contains('modal')) closeModal();
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

// =============================================================================
// Initialize
// =============================================================================

async function init() {
  await loadPipeline();
  loadIdeas();

  // Refresh periodically
  setInterval(loadPipeline, 30000);
}

document.addEventListener('DOMContentLoaded', init);
