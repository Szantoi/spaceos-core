import { useEffect, useState, useCallback } from 'react';
import type { SSEEvent, KanbanSnapshot } from '../types/kanban';

export function useSSE(
  onBoardUpdate: (updates: Partial<KanbanSnapshot>) => void
) {
  const [isConnected, setIsConnected] = useState(false);
  const [eventSource, setEventSource] = useState<EventSource | null>(null);

  const connect = useCallback(() => {
    const url = '/api/kanban/events';
    const es = new EventSource(url);

    es.onopen = () => {
      console.log('SSE connected');
      setIsConnected(true);
    };

    es.onerror = (err) => {
      console.error('SSE error:', err);
      setIsConnected(false);
      es.close();

      // Reconnect after 5 seconds
      setTimeout(connect, 5000);
    };

    es.onmessage = (e) => {
      try {
        const event: SSEEvent = JSON.parse(e.data);

        if (event.type === 'board_update') {
          const updates: Partial<KanbanSnapshot> = {};

          if (event.discovery) {
            updates.discovery = event.discovery;
          }

          if (event.delivery) {
            updates.delivery = event.delivery;
          }

          if (Object.keys(updates).length > 0) {
            onBoardUpdate(updates);
          }
        }
      } catch (err) {
        console.error('Failed to parse SSE event:', err);
      }
    };

    setEventSource(es);
  }, [onBoardUpdate]);

  useEffect(() => {
    connect();

    return () => {
      if (eventSource) {
        eventSource.close();
      }
    };
  }, [connect]);

  const disconnect = useCallback(() => {
    if (eventSource) {
      eventSource.close();
      setEventSource(null);
      setIsConnected(false);
    }
  }, [eventSource]);

  return {
    isConnected,
    disconnect,
    reconnect: connect,
  };
}
