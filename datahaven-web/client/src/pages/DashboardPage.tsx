import { useAuth } from '../hooks/useAuth';
import { useDashboard } from '../hooks/useDashboard';
import { StatsOverview } from '../components/Dashboard/StatsOverview';
import { TerminalGrid } from '../components/Dashboard/TerminalGrid';
import type { Terminal } from '../types/dashboard';

export function DashboardPage() {
  const { authToken } = useAuth();
  const { data, isLoading, error, refresh } = useDashboard(authToken, true);

  const handleTerminalClick = (terminal: Terminal) => {
    console.log('Terminal clicked:', terminal);
    // TODO: Open terminal detail modal or navigate to kanban filtered by terminal
  };

  if (isLoading && !data) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <div className="inline-block w-8 h-8 border-2 border-[var(--accent)] border-t-transparent rounded-full animate-spin mb-4" />
          <div className="text-[var(--text-secondary)]">Loading dashboard...</div>
        </div>
      </div>
    );
  }

  if (error && !data) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <div className="text-[var(--accent-red)] text-xl mb-4">
            Failed to load dashboard
          </div>
          <p className="text-[var(--text-secondary)] mb-4">{error}</p>
          <button
            onClick={refresh}
            className="px-6 py-2 bg-[var(--accent)] text-white rounded-lg hover:bg-[var(--accent-hover)] transition"
          >
            Retry
          </button>
        </div>
      </div>
    );
  }

  if (!data) return null;

  return (
    <div className="space-y-8">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-[var(--text-primary)]">
            SpaceOS Dashboard
          </h1>
          <p className="text-[var(--text-secondary)] text-sm mt-1">
            Last updated: {new Date(data.timestamp).toLocaleTimeString()}
          </p>
        </div>
        <button
          onClick={refresh}
          disabled={isLoading}
          className={`
            px-4 py-2 rounded-lg border border-[var(--border)]
            text-[var(--text-secondary)] hover:text-[var(--text-primary)]
            hover:border-[var(--accent)] transition
            ${isLoading ? 'opacity-50 cursor-not-allowed' : ''}
          `}
        >
          {isLoading ? 'Refreshing...' : 'Refresh'}
        </button>
      </div>

      {/* Stats Overview */}
      <StatsOverview stats={data.stats} health={data.serviceHealth} />

      {/* Terminal Grid */}
      <TerminalGrid
        terminals={data.terminals}
        onTerminalClick={handleTerminalClick}
      />
    </div>
  );
}
