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
// Focus Area Panel (Top section - dynamic load)
// =============================================================================

async function loadFocusPanel() {
  const domainSelect = document.getElementById('domain-select');
  const criteriaDisplay = document.getElementById('criteria-display');

  try {
    const data = await api('/planning/focus');

    // Update domain dropdown
    if (domainSelect) {
      domainSelect.value = data.currentDomain;
    }

    // Update criteria display
    if (criteriaDisplay) {
      criteriaDisplay.innerHTML = `
        <ul>
          ${data.criteria.map(criterion => `
            <li><strong>${escapeHtml(criterion.name)}:</strong> ${escapeHtml(criterion.description)}</li>
          `).join('')}
        </ul>
      `;
    }
  } catch (err) {
    console.error('Failed to load focus panel:', err);
    if (criteriaDisplay) {
      criteriaDisplay.innerHTML = `<div class="empty">Error loading focus data: ${escapeHtml(err.message)}</div>`;
    }
  }
}

function setupFocusPanelHandlers() {
  const domainSelect = document.getElementById('domain-select');
  const syncBtn = document.querySelector('.btn-sync');

  // Sync button handler
  if (syncBtn) {
    syncBtn.addEventListener('click', (e) => {
      e.preventDefault();
      loadFocusPanel();
    });
  }

  // Domain select change handler
  if (domainSelect) {
    domainSelect.addEventListener('change', async (e) => {
      try {
        const response = await fetch('/api/planning/focus', {
          method: 'PUT',
          headers: {
            'Authorization': state.token ? `Bearer ${state.token}` : '',
            'Content-Type': 'application/json'
          },
          body: JSON.stringify({ domain: e.target.value })
        });

        if (!response.ok) throw new Error('Failed to save domain');

        // Reload to reflect changes
        await loadFocusPanel();
      } catch (err) {
        console.error('Failed to save focus domain:', err);
        alert('Failed to save domain: ' + err.message);
      }
    });
  }
}

// =============================================================================
// Epic Flow Panel (Mermaid Graph)
// =============================================================================

let currentEpicGraph = null;

async function loadEpicFlow() {
  const statsEl = document.getElementById('flow-stats');
  const diagramEl = document.getElementById('mermaid-diagram');
  const detailsEl = document.getElementById('epic-details');

  diagramEl.innerHTML = '<div class="loading">Loading epic flow...</div>';
  statsEl.innerHTML = '';
  detailsEl.innerHTML = '';

  try {
    // Fetch epic graph and mermaid diagram
    const [graphData, mermaidData] = await Promise.all([
      fetch('http://localhost:3456/api/graph/epics').then(r => r.json()),
      fetch('http://localhost:3456/api/graph/mermaid/epic/EPICS').then(r => r.json())
    ]);

    currentEpicGraph = graphData.graph || graphData;

    // Render stats
    const nodes = currentEpicGraph.nodes || [];
    const stats = calculateEpicStats(nodes);
    statsEl.innerHTML = `
      <div class="flow-stat-item">
        <strong>${stats.total}</strong> epics
      </div>
      <div class="flow-stat-item">
        <strong>${stats.done}</strong> done
      </div>
      <div class="flow-stat-item">
        <strong>${stats.active}</strong> active
      </div>
      <div class="flow-stat-item">
        <strong>${stats.pending}</strong> pending
      </div>
      <div class="flow-stat-critical">
        Critical Path: ${stats.criticalPath.join(' → ')}
      </div>
    `;

    // Render Mermaid diagram
    const mermaidCode = mermaidData.mermaid || mermaidData.diagram;
    const uniqueId = 'mermaid-' + Date.now();
    diagramEl.innerHTML = `<div class="mermaid" id="${uniqueId}">${escapeHtml(mermaidCode)}</div>`;

    // Render with Mermaid
    await mermaid.run({ nodes: [document.getElementById(uniqueId)] });

    // Render epic details list
    detailsEl.innerHTML = `
      <div class="epic-list">
        <h3>Epic Details</h3>
        ${nodes.map(epic => `
          <div class="epic-item" data-epic="${escapeHtml(epic.id)}">
            <div class="epic-header">
              <span class="epic-id">${escapeHtml(epic.id)}</span>
              <span class="epic-status status-${epic.status}">${escapeHtml(epic.status)}</span>
            </div>
            <div class="epic-meta">
              ${epic.depends_on?.length ? `<div>Depends on: ${epic.depends_on.join(', ')}</div>` : ''}
              ${epic.parallel_with?.length ? `<div>Parallel: ${epic.parallel_with.join(', ')}</div>` : ''}
            </div>
          </div>
        `).join('')}
      </div>
    `;

  } catch (err) {
    diagramEl.innerHTML = `<div class="empty">Error: ${escapeHtml(err.message)}</div>`;
    console.error('Failed to load epic flow:', err);
  }
}

function calculateEpicStats(nodes) {
  const stats = {
    total: nodes.length,
    done: nodes.filter(n => n.status === 'done').length,
    active: nodes.filter(n => n.status === 'active').length,
    pending: nodes.filter(n => n.status === 'pending').length,
    criticalPath: ['KERNEL', 'JOINERY', 'ORCH', 'PORTAL'] // Simplified
  };
  return stats;
}

async function exportMermaid() {
  try {
    const data = await fetch('http://localhost:3456/api/graph/mermaid/epic/EPICS').then(r => r.json());
    await navigator.clipboard.writeText(data.mermaid || data.diagram);
    alert('Mermaid diagram copied to clipboard!');
  } catch (err) {
    alert('Failed to export: ' + err.message);
  }
}

// =============================================================================
// Focus Panel
// =============================================================================

let currentFocus = null;

async function loadFocus() {
  const domainSelect = document.getElementById('focus-domain');
  const domainsList = document.getElementById('focus-domains-list');
  const criteriaList = document.getElementById('focus-criteria');

  domainSelect.innerHTML = '<option value="">Loading...</option>';

  try {
    const data = await api('/planning/focus');
    currentFocus = data;

    // Render domain select
    domainSelect.innerHTML = data.domains.map(d => `
      <option value="${d.value}" ${d.value === data.currentDomain ? 'selected' : ''}>
        ${d.label}
      </option>
    `).join('');

    // Render domains list
    domainsList.innerHTML = data.domains.map(d => `
      <div class="domain-item ${d.value === data.currentDomain ? 'active' : ''}">
        <strong>${d.value}</strong> — ${d.description}
      </div>
    `).join('');

    // Render criteria
    criteriaList.innerHTML = data.criteria.map(c => `
      <div class="criteria-item">
        <strong>${c.name}:</strong> ${c.description}
      </div>
    `).join('');

  } catch (err) {
    domainSelect.innerHTML = '<option value="">Error loading</option>';
    console.error('Failed to load focus:', err);
  }
}

async function saveFocus() {
  const domainSelect = document.getElementById('focus-domain');
  const newDomain = domainSelect.value;

  if (!newDomain) {
    alert('Please select a domain');
    return;
  }

  try {
    const headers = {};
    if (state.token) {
      headers['Authorization'] = `Bearer ${state.token}`;
    }
    headers['Content-Type'] = 'application/json';

    const response = await fetch('/api/planning/focus', {
      method: 'PUT',
      headers,
      body: JSON.stringify({ domain: newDomain })
    });

    if (!response.ok) {
      throw new Error('Failed to save focus');
    }

    alert('Domain focus updated successfully!');
    loadFocus(); // Reload to reflect changes

  } catch (err) {
    alert('Failed to save: ' + err.message);
    console.error('Failed to save focus:', err);
  }
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
    case 'workflow': loadEpicFlow(); break;
    case 'focus': loadFocus(); break;
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
  setupFocusPanelHandlers();
  await loadPipeline();
  await loadFocusPanel();
  loadIdeas();

  // Refresh periodically
  setInterval(loadPipeline, 30000);
}

document.addEventListener('DOMContentLoaded', init);
