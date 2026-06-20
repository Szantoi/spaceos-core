import type { DiscoveryItem, DeliveryMessage } from '../../types/kanban';

interface KanbanCardProps {
  item: DiscoveryItem | DeliveryMessage;
  onClick: () => void;
  compact?: boolean;
}

export function KanbanCard({ item, onClick, compact = false }: KanbanCardProps) {
  const isDelivery = 'from' in item;

  const priorityColors = {
    critical: 'bg-[var(--accent-red)] text-white',
    high: 'bg-[var(--accent-yellow)] text-black',
    medium: 'bg-[var(--accent)] text-white',
    low: 'bg-[var(--text-muted)] text-white',
  };

  const priorityClass = item.priority ? priorityColors[item.priority] : 'bg-[var(--text-muted)] text-white';

  const title = item.title || item.id;
  const displayTitle = compact && title.length > 50 ? title.substring(0, 50) + '...' : title;

  return (
    <div
      onClick={onClick}
      className={`bg-[var(--bg-tertiary)] border border-[var(--border)] rounded p-3 cursor-pointer hover:border-[var(--accent)] transition ${
        compact ? 'text-sm' : ''
      }`}
    >
      <div className="text-[var(--text-primary)] font-medium mb-2">
        {displayTitle}
      </div>
      <div className="flex flex-wrap gap-1">
        {item.priority && (
          <span className={`px-2 py-0.5 text-xs rounded ${priorityClass}`}>
            {item.priority}
          </span>
        )}
        {!isDelivery && 'complexity' in item && item.complexity && (
          <span className="px-2 py-0.5 text-xs rounded bg-[var(--bg-secondary)] text-[var(--text-secondary)] border border-[var(--border)]">
            {item.complexity}
          </span>
        )}
        {!isDelivery && 'assignee' in item && item.assignee && (
          <span className="px-2 py-0.5 text-xs rounded bg-[var(--accent-bg)] text-[var(--accent)] border border-[var(--accent-border)]">
            {item.assignee}
          </span>
        )}
        {isDelivery && item.type && (
          <span className="px-2 py-0.5 text-xs rounded bg-[var(--bg-secondary)] text-[var(--text-secondary)] border border-[var(--border)]">
            {item.type}
          </span>
        )}
        {isDelivery && item.from && (
          <span className="px-2 py-0.5 text-xs rounded bg-[var(--bg-secondary)] text-[var(--text-secondary)] border border-[var(--border)]">
            from: {item.from}
          </span>
        )}
      </div>
    </div>
  );
}
