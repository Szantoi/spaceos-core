import type { DashboardStats, ServiceHealth } from '../../types/dashboard';

interface StatsOverviewProps {
  stats: DashboardStats;
  health: ServiceHealth;
}

interface StatCardProps {
  label: string;
  value: number | string;
  color?: 'default' | 'green' | 'yellow' | 'red' | 'purple';
}

function StatCard({ label, value, color = 'default' }: StatCardProps) {
  const colorClasses = {
    default: 'text-[var(--text-primary)]',
    green: 'text-emerald-400',
    yellow: 'text-amber-400',
    red: 'text-red-400',
    purple: 'text-purple-400',
  };

  return (
    <div className="bg-[var(--bg-secondary)] rounded-lg p-4 border border-[var(--border)]">
      <div className={`text-3xl font-bold ${colorClasses[color]}`}>
        {value}
      </div>
      <div className="text-sm text-[var(--text-secondary)] mt-1">
        {label}
      </div>
    </div>
  );
}

export function StatsOverview({ stats, health }: StatsOverviewProps) {
  return (
    <div className="space-y-6">
      {/* Terminal Stats */}
      <div>
        <h2 className="text-lg font-semibold text-[var(--text-primary)] mb-4">
          Terminal Overview
        </h2>
        <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-4">
          <StatCard label="Total Terminals" value={stats.totalTerminals} />
          <StatCard label="Working" value={stats.activeTerminals} color="green" />
          <StatCard label="Idle" value={stats.idleTerminals} color="yellow" />
          <StatCard label="Offline" value={stats.offlineTerminals} color="red" />
          <StatCard label="Unread Messages" value={stats.totalUnread} color="purple" />
          <StatCard label="Documents" value={stats.documentsIndexed.toLocaleString()} />
        </div>
      </div>

      {/* Service Health */}
      <div className="bg-[var(--bg-secondary)] rounded-lg p-4 border border-[var(--border)]">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className={`w-3 h-3 rounded-full ${
              health.status === 'ok' ? 'bg-emerald-500' :
              health.status === 'degraded' ? 'bg-amber-500' : 'bg-red-500'
            }`} />
            <span className="font-medium text-[var(--text-primary)]">
              Knowledge Service
            </span>
            <span className={`text-sm ${
              health.status === 'ok' ? 'text-emerald-400' :
              health.status === 'degraded' ? 'text-amber-400' : 'text-red-400'
            }`}>
              {health.status.toUpperCase()}
            </span>
          </div>
          <div className="flex items-center gap-6 text-sm text-[var(--text-secondary)]">
            <span>Vector: {health.vectorBackend}</span>
            <span>Embedding: {health.embeddingBackend}</span>
            <span>Port: {health.port}</span>
          </div>
        </div>
      </div>
    </div>
  );
}
