import { useEffect } from 'react';
import type { DiscoveryItem, DeliveryMessage } from '../../types/kanban';

interface CardModalProps {
  isOpen: boolean;
  item: DiscoveryItem | DeliveryMessage | null;
  onClose: () => void;
  track: 'discovery' | 'delivery';
}

export function CardModal({ isOpen, item, onClose, track }: CardModalProps) {
  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        onClose();
      }
    };

    if (isOpen) {
      document.addEventListener('keydown', handleEscape);
    }

    return () => {
      document.removeEventListener('keydown', handleEscape);
    };
  }, [isOpen, onClose]);

  if (!isOpen || !item) return null;

  const isDelivery = track === 'delivery';

  return (
    <div
      className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50"
      onClick={onClose}
    >
      <div
        className="bg-[var(--bg-secondary)] border border-[var(--border)] rounded-lg p-6 max-w-2xl w-full max-h-[80vh] overflow-y-auto"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-start justify-between mb-4">
          <h3 className="text-xl font-semibold text-[var(--text-primary)]">
            {item.title || item.id}
          </h3>
          <button
            onClick={onClose}
            className="text-[var(--text-secondary)] hover:text-[var(--text-primary)] text-2xl leading-none"
          >
            &times;
          </button>
        </div>

        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <span className="text-sm text-[var(--text-muted)]">ID</span>
              <div className="text-[var(--text-primary)]">{item.id}</div>
            </div>
            {!isDelivery && 'status' in item && (
              <div>
                <span className="text-sm text-[var(--text-muted)]">Status</span>
                <div className="text-[var(--text-primary)]">{item.status || '-'}</div>
              </div>
            )}
            {isDelivery && 'type' in item && (
              <div>
                <span className="text-sm text-[var(--text-muted)]">Type</span>
                <div className="text-[var(--text-primary)]">{item.type || '-'}</div>
              </div>
            )}
            <div>
              <span className="text-sm text-[var(--text-muted)]">Priority</span>
              <div className="text-[var(--text-primary)]">{item.priority || '-'}</div>
            </div>
            {!isDelivery && 'complexity' in item && (
              <div>
                <span className="text-sm text-[var(--text-muted)]">Complexity</span>
                <div className="text-[var(--text-primary)]">{item.complexity || '-'}</div>
              </div>
            )}
            {!isDelivery && 'assignee' in item && (
              <div>
                <span className="text-sm text-[var(--text-muted)]">Assignee</span>
                <div className="text-[var(--text-primary)]">{item.assignee || '-'}</div>
              </div>
            )}
            {isDelivery && 'from' in item && (
              <>
                <div>
                  <span className="text-sm text-[var(--text-muted)]">From</span>
                  <div className="text-[var(--text-primary)]">{item.from || '-'}</div>
                </div>
                <div>
                  <span className="text-sm text-[var(--text-muted)]">To</span>
                  <div className="text-[var(--text-primary)]">{item.to || '-'}</div>
                </div>
                <div>
                  <span className="text-sm text-[var(--text-muted)]">Status</span>
                  <div className="text-[var(--text-primary)]">{item.status || '-'}</div>
                </div>
                <div>
                  <span className="text-sm text-[var(--text-muted)]">Model</span>
                  <div className="text-[var(--text-primary)]">{item.model || '-'}</div>
                </div>
                <div>
                  <span className="text-sm text-[var(--text-muted)]">Created</span>
                  <div className="text-[var(--text-primary)]">
                    {item.created ? new Date(item.created).toLocaleDateString('hu-HU') : '-'}
                  </div>
                </div>
                {item.ref && (
                  <div>
                    <span className="text-sm text-[var(--text-muted)]">Reference</span>
                    <div className="text-[var(--text-primary)]">{item.ref}</div>
                  </div>
                )}
              </>
            )}
          </div>

          {!isDelivery && 'dor' in item && item.dor && (
            <div className="pt-4 border-t border-[var(--border)]">
              <span className="text-sm text-[var(--text-muted)]">Definition of Ready</span>
              <div className="flex items-center gap-2 mt-1">
                <span
                  className={`font-semibold ${
                    item.dor.ready ? 'text-[var(--accent-green)]' : 'text-[var(--accent-yellow)]'
                  }`}
                >
                  {item.dor.ready ? 'READY' : 'NOT READY'}
                </span>
                <span className="text-[var(--text-secondary)]">
                  ({item.dor.met}/{item.dor.total} criteria met)
                </span>
              </div>
            </div>
          )}

          <div className="pt-4 border-t border-[var(--border)]">
            <span className="text-sm text-[var(--text-muted)]">Path</span>
            <div className="text-xs text-[var(--text-secondary)] font-mono mt-1 break-all">
              {item.path}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
