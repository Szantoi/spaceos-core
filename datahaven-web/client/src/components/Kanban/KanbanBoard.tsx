import { useState } from 'react';
import type { DiscoveryBoard, DeliveryBoard, DiscoveryItem, DeliveryMessage, KanbanMetrics } from '../../types/kanban';
import { KanbanColumn } from './KanbanColumn';
import { KanbanCard } from './KanbanCard';
import { CardModal } from './CardModal';
import { MetricsBar } from './MetricsBar';

interface KanbanBoardProps {
  discovery: DiscoveryBoard | null;
  delivery: DeliveryBoard | null;
  metrics: KanbanMetrics | null;
  onRefresh: () => void;
  onItemClick: (track: 'discovery' | 'delivery', path: string) => Promise<void>;
}

const TERMINALS = [
  'kernel', 'orch', 'fe', 'joinery', 'abstractions', 'cutting',
  'inventory', 'procurement', 'sales', 'identity', 'infra', 'e2e',
  'architect', 'librarian', 'nexus', 'root', 'conductor'
];

export function KanbanBoard({ discovery, delivery, metrics, onRefresh, onItemClick: _onItemClick }: KanbanBoardProps) {
  // Note: onItemClick is available for future use when card modal triggers API refresh
  void _onItemClick;
  const [currentTrack, setCurrentTrack] = useState<'all' | 'discovery' | 'delivery'>('all');
  const [terminalFilter, setTerminalFilter] = useState('');
  const [modalItem, setModalItem] = useState<DiscoveryItem | DeliveryMessage | null>(null);
  const [modalTrack, setModalTrack] = useState<'discovery' | 'delivery'>('discovery');
  const [isModalOpen, setIsModalOpen] = useState(false);

  const openModal = (item: DiscoveryItem | DeliveryMessage, track: 'discovery' | 'delivery') => {
    setModalItem(item);
    setModalTrack(track);
    setIsModalOpen(true);
  };

  const closeModal = () => {
    setIsModalOpen(false);
    setModalItem(null);
  };

  // Calculate metrics (safely handle missing totals)
  const discoveryWip = discovery?.totals ? Object.values(discovery.totals).reduce((a, b) => a + b, 0) : 0;
  const deliveryWip = delivery?.totals ? (delivery.totals.inbox + delivery.totals.active) : 0;
  const activeSessions = delivery?.activeSessions?.length || 0;

  // Filter delivery swimlanes
  const filteredSwimlanes = delivery?.swimlanes?.filter(
    s => !terminalFilter || s.terminal === terminalFilter
  ).sort((a, b) => {
    if (a.sessionActive !== b.sessionActive) return b.sessionActive ? 1 : -1;
    return (b.totals?.inbox || 0) - (a.totals?.inbox || 0);
  }) || [];

  return (
    <div>
      {/* Controls */}
      <div className="mb-6 flex items-center justify-between">
        <div className="flex gap-2">
          <button
            onClick={() => setCurrentTrack('all')}
            className={`px-4 py-2 rounded transition ${
              currentTrack === 'all'
                ? 'bg-[var(--accent)] text-white'
                : 'bg-[var(--bg-secondary)] text-[var(--text-secondary)] hover:text-[var(--text-primary)]'
            }`}
          >
            All Tracks
          </button>
          <button
            onClick={() => setCurrentTrack('discovery')}
            className={`px-4 py-2 rounded transition ${
              currentTrack === 'discovery'
                ? 'bg-[var(--accent)] text-white'
                : 'bg-[var(--bg-secondary)] text-[var(--text-secondary)] hover:text-[var(--text-primary)]'
            }`}
          >
            Discovery
          </button>
          <button
            onClick={() => setCurrentTrack('delivery')}
            className={`px-4 py-2 rounded transition ${
              currentTrack === 'delivery'
                ? 'bg-[var(--accent)] text-white'
                : 'bg-[var(--bg-secondary)] text-[var(--text-secondary)] hover:text-[var(--text-primary)]'
            }`}
          >
            Delivery
          </button>
        </div>
        <div className="flex gap-3">
          <select
            value={terminalFilter}
            onChange={(e) => setTerminalFilter(e.target.value)}
            className="px-3 py-2 bg-[var(--bg-secondary)] border border-[var(--border)] rounded text-[var(--text-primary)] focus:outline-none focus:border-[var(--accent)]"
          >
            <option value="">All Terminals</option>
            {TERMINALS.map(t => (
              <option key={t} value={t}>{t}</option>
            ))}
          </select>
          <button
            onClick={onRefresh}
            className="px-4 py-2 bg-[var(--bg-secondary)] border border-[var(--border)] rounded text-[var(--text-primary)] hover:bg-[var(--bg-tertiary)] transition"
          >
            Refresh
          </button>
        </div>
      </div>

      {/* Metrics */}
      <MetricsBar
        discoveryWip={discoveryWip}
        deliveryWip={deliveryWip}
        activeSessions={activeSessions}
        metrics={metrics}
      />

      {/* Discovery Track */}
      {(currentTrack === 'all' || currentTrack === 'discovery') && discovery?.columns && (
        <div className="mb-8">
          <h2 className="text-xl font-semibold text-[var(--text-primary)] mb-4">Discovery Track</h2>
          <div className="flex gap-4 overflow-x-auto pb-4">
            <KanbanColumn title="Ideas" count={discovery.totals?.ideas || 0}>
              {(discovery.columns.ideas?.length || 0) === 0 ? (
                <div className="text-center text-[var(--text-muted)] py-4">No items</div>
              ) : (
                discovery.columns.ideas.map((item) => (
                  <KanbanCard
                    key={item.id}
                    item={item}
                    onClick={() => openModal(item, 'discovery')}
                  />
                ))
              )}
            </KanbanColumn>
            <KanbanColumn title="Selected" count={discovery.totals?.selected || 0}>
              {(discovery.columns.selected?.length || 0) === 0 ? (
                <div className="text-center text-[var(--text-muted)] py-4">No items</div>
              ) : (
                discovery.columns.selected.map((item) => (
                  <KanbanCard
                    key={item.id}
                    item={item}
                    onClick={() => openModal(item, 'discovery')}
                  />
                ))
              )}
            </KanbanColumn>
            <KanbanColumn title="Debate" count={discovery.totals?.debate || 0}>
              {(discovery.columns.debate?.length || 0) === 0 ? (
                <div className="text-center text-[var(--text-muted)] py-4">No items</div>
              ) : (
                discovery.columns.debate.map((item) => (
                  <KanbanCard
                    key={item.id}
                    item={item}
                    onClick={() => openModal(item, 'discovery')}
                  />
                ))
              )}
            </KanbanColumn>
            <KanbanColumn title="Consensus" count={discovery.totals?.consensus || 0}>
              {(discovery.columns.consensus?.length || 0) === 0 ? (
                <div className="text-center text-[var(--text-muted)] py-4">No items</div>
              ) : (
                discovery.columns.consensus.map((item) => (
                  <KanbanCard
                    key={item.id}
                    item={item}
                    onClick={() => openModal(item, 'discovery')}
                  />
                ))
              )}
            </KanbanColumn>
            <KanbanColumn title="Queue" count={discovery.totals?.queue || 0}>
              {(discovery.columns.queue?.length || 0) === 0 ? (
                <div className="text-center text-[var(--text-muted)] py-4">No items</div>
              ) : (
                discovery.columns.queue.map((item) => (
                  <KanbanCard
                    key={item.id}
                    item={item}
                    onClick={() => openModal(item, 'discovery')}
                  />
                ))
              )}
            </KanbanColumn>
          </div>
        </div>
      )}

      {/* Delivery Track */}
      {(currentTrack === 'all' || currentTrack === 'delivery') && delivery?.swimlanes && (
        <div>
          <h2 className="text-xl font-semibold text-[var(--text-primary)] mb-4">Delivery Track</h2>
          <div className="space-y-4">
            {filteredSwimlanes.length === 0 ? (
              <div className="text-center text-[var(--text-muted)] py-8 bg-[var(--bg-secondary)] border border-[var(--border)] rounded-lg">
                No terminals match filter
              </div>
            ) : (
              filteredSwimlanes.map((swimlane) => (
                <div key={swimlane.terminal} className="bg-[var(--bg-secondary)] border border-[var(--border)] rounded-lg p-4">
                  <div className="flex items-center justify-between mb-4">
                    <div className="flex items-center gap-3">
                      <h3 className="font-semibold text-[var(--text-primary)] uppercase">
                        {swimlane.terminal}
                      </h3>
                      <span
                        className={`px-2 py-0.5 text-xs rounded ${
                          swimlane.sessionActive
                            ? 'bg-[var(--accent-green)] text-white'
                            : 'bg-[var(--text-muted)] text-white'
                        }`}
                      >
                        {swimlane.sessionActive ? 'Active' : 'Idle'}
                      </span>
                    </div>
                    <div className="text-sm text-[var(--text-secondary)] flex gap-4">
                      <span>Inbox: {swimlane.totals?.inbox || 0}</span>
                      <span>Active: {swimlane.totals?.active || 0}</span>
                      <span>Archive: {swimlane.totals?.archive || 0}</span>
                    </div>
                  </div>
                  <div className="grid grid-cols-4 gap-4">
                    {(['inbox', 'active', 'review', 'done'] as const).map((col) => (
                      <div key={col}>
                        <div className="text-sm text-[var(--text-secondary)] mb-2 flex items-center justify-between">
                          <span className="capitalize">{col}</span>
                          <span>{swimlane.columns?.[col]?.length || 0}</span>
                        </div>
                        <div className="space-y-2">
                          {(swimlane.columns?.[col] || []).slice(0, 10).map((msg) => (
                            <KanbanCard
                              key={msg.id}
                              item={msg}
                              onClick={() => openModal(msg, 'delivery')}
                              compact
                            />
                          ))}
                          {(swimlane.columns?.[col]?.length || 0) > 10 && (
                            <div className="text-xs text-center text-[var(--text-muted)] py-2">
                              +{(swimlane.columns?.[col]?.length || 0) - 10} more
                            </div>
                          )}
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      )}

      {/* Modal */}
      <CardModal
        isOpen={isModalOpen}
        item={modalItem}
        onClose={closeModal}
        track={modalTrack}
      />
    </div>
  );
}
