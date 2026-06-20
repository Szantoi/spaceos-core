import type { KanbanMetrics } from '../../types/kanban';

interface MetricsBarProps {
  discoveryWip: number;
  deliveryWip: number;
  activeSessions: number;
  metrics: KanbanMetrics | null;
}

export function MetricsBar({ discoveryWip, deliveryWip, activeSessions, metrics }: MetricsBarProps) {
  const throughput = metrics?.delivery?.throughput?.items_per_day || '0.00';

  return (
    <div className="bg-[var(--bg-secondary)] border border-[var(--border)] rounded-lg p-4 mb-6">
      <div className="grid grid-cols-4 gap-6">
        <div>
          <div className="text-2xl font-semibold text-[var(--accent)]">{discoveryWip}</div>
          <div className="text-sm text-[var(--text-secondary)]">Discovery WIP</div>
        </div>
        <div>
          <div className="text-2xl font-semibold text-[var(--accent)]">{deliveryWip}</div>
          <div className="text-sm text-[var(--text-secondary)]">Delivery WIP</div>
        </div>
        <div>
          <div className="text-2xl font-semibold text-[var(--accent-green)]">{throughput}</div>
          <div className="text-sm text-[var(--text-secondary)]">Items/Day</div>
        </div>
        <div>
          <div className="text-2xl font-semibold text-[var(--accent-green)]">{activeSessions}</div>
          <div className="text-sm text-[var(--text-secondary)]">Active Sessions</div>
        </div>
      </div>
    </div>
  );
}
