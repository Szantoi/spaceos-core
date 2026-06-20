import { useState, useCallback } from 'react';
import type { KanbanSnapshot, KanbanMetrics, DiscoveryItem, DeliveryMessage } from '../types/kanban';

export function useKanban(authToken: string | null) {
  const [boardData, setBoardData] = useState<KanbanSnapshot | null>(null);
  const [metrics, setMetrics] = useState<KanbanMetrics | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const headers: HeadersInit = authToken ? { 'Authorization': `Bearer ${authToken}` } : {};

  const loadBoard = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const res = await fetch('/api/kanban/snapshot', { headers });

      if (!res.ok) {
        throw new Error(`HTTP ${res.status}`);
      }

      const data: KanbanSnapshot = await res.json();
      setBoardData(data);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to load board';
      setError(message);
    } finally {
      setIsLoading(false);
    }
  }, [authToken]);

  const loadMetrics = useCallback(async (periodDays = 7) => {
    try {
      const res = await fetch(`/api/kanban/metrics?period=${periodDays}`, { headers });

      if (!res.ok) {
        throw new Error(`HTTP ${res.status}`);
      }

      const data: KanbanMetrics = await res.json();
      setMetrics(data);
    } catch (err) {
      console.error('Failed to load metrics:', err);
    }
  }, [authToken]);

  const getDiscoveryItem = useCallback(async (path: string): Promise<DiscoveryItem | null> => {
    try {
      const res = await fetch(`/api/kanban/discovery/item/${encodeURIComponent(path)}`, { headers });

      if (!res.ok) {
        throw new Error(`HTTP ${res.status}`);
      }

      return await res.json();
    } catch (err) {
      console.error('Failed to get discovery item:', err);
      return null;
    }
  }, [authToken]);

  const getDeliveryMessage = useCallback(async (path: string): Promise<DeliveryMessage | null> => {
    try {
      const res = await fetch(`/api/kanban/delivery/message/${encodeURIComponent(path)}`, { headers });

      if (!res.ok) {
        throw new Error(`HTTP ${res.status}`);
      }

      return await res.json();
    } catch (err) {
      console.error('Failed to get delivery message:', err);
      return null;
    }
  }, [authToken]);

  const updateBoard = useCallback((updates: Partial<KanbanSnapshot>) => {
    setBoardData(prev => prev ? { ...prev, ...updates } : null);
  }, []);

  return {
    boardData,
    metrics,
    isLoading,
    error,
    loadBoard,
    loadMetrics,
    getDiscoveryItem,
    getDeliveryMessage,
    updateBoard,
  };
}
