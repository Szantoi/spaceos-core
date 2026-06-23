import { useState, useEffect, useCallback } from 'react';
import { useAuth } from '../hooks/useAuth';

interface PlanningItem {
  id: string;
  title: string;
  status: 'idea' | 'selected' | 'debate' | 'consensus' | 'queue';
  priority: 'critical' | 'high' | 'medium' | 'low';
  segment?: string;
  createdAt: string;
  confidence?: number;
}

interface PlanningMetrics {
  ideas: number;
  selected: number;
  inDebate: number;
  consensus: number;
  queued: number;
  lastScan?: string;
}

export function PlanningPage() {
  const { authToken } = useAuth();
  const [items, setItems] = useState<PlanningItem[]>([]);
  const [metrics, setMetrics] = useState<PlanningMetrics>({
    ideas: 0,
    selected: 0,
    inDebate: 0,
    consensus: 0,
    queued: 0,
  });
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filterStatus, setFilterStatus] = useState<string>('all');
  const [filterPriority, setFilterPriority] = useState<string>('all');

  const loadPlanningData = useCallback(async () => {
    if (!authToken) return;

    try {
      const headers = {
        'Authorization': `Bearer ${authToken}`,
        'Content-Type': 'application/json',
      };

      // Load planning items
      const itemsRes = await fetch('/api/planning/items', { headers });
      if (itemsRes.ok) {
        const data = await itemsRes.json();
        setItems(data.items || []);
        setMetrics(data.metrics || metrics);
      }

      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load planning data');
    } finally {
      setIsLoading(false);
    }
  }, [authToken, metrics]);

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    loadPlanningData();
    // Poll every 60 seconds
    const interval = setInterval(loadPlanningData, 60000);
    return () => clearInterval(interval);
  }, [loadPlanningData]);

  const filteredItems = items.filter(item => {
    if (filterStatus !== 'all' && item.status !== filterStatus) return false;
    if (filterPriority !== 'all' && item.priority !== filterPriority) return false;
    return true;
  });

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'idea': return 'bg-blue-500/10 text-blue-400 border-blue-500/30';
      case 'selected': return 'bg-purple-500/10 text-purple-400 border-purple-500/30';
      case 'debate': return 'bg-yellow-500/10 text-yellow-400 border-yellow-500/30';
      case 'consensus': return 'bg-green-500/10 text-green-400 border-green-500/30';
      case 'queue': return 'bg-cyan-500/10 text-cyan-400 border-cyan-500/30';
      default: return 'bg-gray-500/10 text-gray-400 border-gray-500/30';
    }
  };

  const getPriorityColor = (priority: string) => {
    switch (priority) {
      case 'critical': return 'text-red-400';
      case 'high': return 'text-orange-400';
      case 'medium': return 'text-yellow-400';
      case 'low': return 'text-blue-400';
      default: return 'text-gray-400';
    }
  };

  if (isLoading && items.length === 0) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <div className="inline-block w-8 h-8 border-2 border-[var(--accent)] border-t-transparent rounded-full animate-spin mb-4" />
          <div className="text-[var(--text-secondary)]">Loading planning pipeline...</div>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-[var(--text-primary)]">Planning Pipeline</h1>
          <p className="text-[var(--text-secondary)] text-sm mt-1">
            Idea → Selected → Debate → Consensus → Queue
          </p>
        </div>
        <button
          onClick={loadPlanningData}
          className="px-4 py-2 bg-[var(--accent)] text-white rounded-lg hover:bg-[var(--accent-hover)] transition"
          disabled={isLoading}
        >
          {isLoading ? 'Loading...' : 'Refresh'}
        </button>
      </div>

      {error && (
        <div className="bg-red-500/10 border border-red-500/30 rounded-lg p-4">
          <div className="text-red-400">{error}</div>
        </div>
      )}

      {/* Metrics Overview */}
      <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
        <div className="bg-[var(--bg-secondary)] border border-blue-500/30 rounded-lg p-4">
          <div className="text-blue-400 text-sm font-medium mb-1">Ideas</div>
          <div className="text-2xl font-bold text-[var(--text-primary)]">{metrics.ideas}</div>
        </div>
        <div className="bg-[var(--bg-secondary)] border border-purple-500/30 rounded-lg p-4">
          <div className="text-purple-400 text-sm font-medium mb-1">Selected</div>
          <div className="text-2xl font-bold text-[var(--text-primary)]">{metrics.selected}</div>
        </div>
        <div className="bg-[var(--bg-secondary)] border border-yellow-500/30 rounded-lg p-4">
          <div className="text-yellow-400 text-sm font-medium mb-1">In Debate</div>
          <div className="text-2xl font-bold text-[var(--text-primary)]">{metrics.inDebate}</div>
        </div>
        <div className="bg-[var(--bg-secondary)] border border-green-500/30 rounded-lg p-4">
          <div className="text-green-400 text-sm font-medium mb-1">Consensus</div>
          <div className="text-2xl font-bold text-[var(--text-primary)]">{metrics.consensus}</div>
        </div>
        <div className="bg-[var(--bg-secondary)] border border-cyan-500/30 rounded-lg p-4">
          <div className="text-cyan-400 text-sm font-medium mb-1">Queued</div>
          <div className="text-2xl font-bold text-[var(--text-primary)]">{metrics.queued}</div>
        </div>
      </div>

      {/* Filters */}
      <div className="flex gap-4">
        <select
          value={filterStatus}
          onChange={(e) => setFilterStatus(e.target.value)}
          className="px-4 py-2 bg-[var(--bg-secondary)] border border-[var(--border)] rounded-lg text-[var(--text-primary)] focus:outline-none focus:border-[var(--accent)]"
        >
          <option value="all">All Stages</option>
          <option value="idea">Ideas</option>
          <option value="selected">Selected</option>
          <option value="debate">In Debate</option>
          <option value="consensus">Consensus</option>
          <option value="queue">Queued</option>
        </select>
        <select
          value={filterPriority}
          onChange={(e) => setFilterPriority(e.target.value)}
          className="px-4 py-2 bg-[var(--bg-secondary)] border border-[var(--border)] rounded-lg text-[var(--text-primary)] focus:outline-none focus:border-[var(--accent)]"
        >
          <option value="all">All Priorities</option>
          <option value="critical">Critical</option>
          <option value="high">High</option>
          <option value="medium">Medium</option>
          <option value="low">Low</option>
        </select>
      </div>

      {/* Planning Items List */}
      <div className="space-y-3">
        {filteredItems.length === 0 ? (
          <div className="bg-[var(--bg-secondary)] border border-[var(--border)] rounded-lg p-8 text-center">
            <div className="text-[var(--text-secondary)]">
              {filterStatus === 'all' && filterPriority === 'all'
                ? 'No planning items found. The planning pipeline will generate ideas every 30 minutes.'
                : 'No items match the current filters.'}
            </div>
          </div>
        ) : (
          filteredItems.map((item) => (
            <div
              key={item.id}
              className="bg-[var(--bg-secondary)] border border-[var(--border)] rounded-lg p-4 hover:border-[var(--accent)] transition cursor-pointer"
            >
              <div className="flex items-start justify-between mb-2">
                <div className="flex-1">
                  <div className="flex items-center gap-2 mb-1">
                    <span className={`px-2 py-1 rounded text-xs font-medium border ${getStatusColor(item.status)}`}>
                      {item.status}
                    </span>
                    <span className={`text-xs font-medium ${getPriorityColor(item.priority)}`}>
                      {item.priority.toUpperCase()}
                    </span>
                    {item.segment && (
                      <span className="text-xs text-[var(--text-secondary)]">
                        {item.segment}
                      </span>
                    )}
                  </div>
                  <h3 className="text-[var(--text-primary)] font-medium">{item.title}</h3>
                </div>
                {item.confidence !== undefined && (
                  <div className="ml-4">
                    <div className="text-xs text-[var(--text-secondary)] mb-1">Confidence</div>
                    <div className="text-lg font-bold text-[var(--accent)]">
                      {Math.round(item.confidence * 100)}%
                    </div>
                  </div>
                )}
              </div>
              <div className="flex items-center gap-4 text-xs text-[var(--text-secondary)]">
                <span>Created: {new Date(item.createdAt).toLocaleDateString()}</span>
              </div>
            </div>
          ))
        )}
      </div>

      {/* Last Scan Info */}
      {metrics.lastScan && (
        <div className="text-center text-sm text-[var(--text-secondary)]">
          Last scan: {new Date(metrics.lastScan).toLocaleString()}
        </div>
      )}
    </div>
  );
}
