import type { ReactNode } from 'react';

interface KanbanColumnProps {
  title: string;
  count: number;
  children: ReactNode;
}

export function KanbanColumn({ title, count, children }: KanbanColumnProps) {
  return (
    <div className="bg-[var(--bg-secondary)] border border-[var(--border)] rounded-lg min-w-[280px] flex-1">
      <div className="border-b border-[var(--border)] px-4 py-3 flex items-center justify-between">
        <span className="font-medium text-[var(--text-primary)]">{title}</span>
        <span className="px-2 py-0.5 text-xs rounded bg-[var(--bg-tertiary)] text-[var(--text-secondary)]">
          {count}
        </span>
      </div>
      <div className="p-3 space-y-2 max-h-[600px] overflow-y-auto">
        {children}
      </div>
    </div>
  );
}
