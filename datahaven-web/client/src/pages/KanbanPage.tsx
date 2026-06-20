import { useEffect } from 'react';
import { useKanban } from '../hooks/useKanban';
import { KanbanBoard } from '../components/Kanban/KanbanBoard';

interface KanbanPageProps {
  authToken: string | null;
  updateBoard: (updates: any) => void;
}

export function KanbanPage({ authToken }: KanbanPageProps) {
  const {
    boardData,
    metrics,
    isLoading,
    error,
    loadBoard,
    loadMetrics,
    getDiscoveryItem,
    getDeliveryMessage,
  } = useKanban(authToken);

  useEffect(() => {
    loadBoard();
    loadMetrics();
  }, [loadBoard, loadMetrics]);

  const handleRefresh = () => {
    loadBoard();
    loadMetrics();
  };

  const handleItemClick = async (track: 'discovery' | 'delivery', path: string) => {
    if (track === 'discovery') {
      const item = await getDiscoveryItem(path);
      console.log('Discovery item:', item);
    } else {
      const message = await getDeliveryMessage(path);
      console.log('Delivery message:', message);
    }
  };

  if (isLoading && !boardData) {
    return (
      <div className="text-center py-20">
        <div className="text-[var(--text-primary)] text-xl">Loading kanban board...</div>
      </div>
    );
  }

  if (error && !boardData) {
    return (
      <div className="text-center py-20">
        <div className="text-[var(--accent-red)] text-xl">Error: {error}</div>
        <button
          onClick={handleRefresh}
          className="mt-4 px-6 py-2 bg-[var(--accent)] text-white rounded hover:bg-[var(--accent-hover)] transition"
        >
          Retry
        </button>
      </div>
    );
  }

  return (
    <KanbanBoard
      discovery={boardData?.discovery || null}
      delivery={boardData?.delivery || null}
      metrics={metrics}
      onRefresh={handleRefresh}
      onItemClick={handleItemClick}
    />
  );
}
