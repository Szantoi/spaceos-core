import { useEffect, useState, useCallback } from 'react';

export interface AutonomousCycle {
  id: number;
  timestamp: string;
  conductor_status: string;
  dispatched_task: string | null;
  result: string;
  duration_ms: number;
}

export interface ChartDataPoint {
  hour: string;
  dispatched: number;
  skipped: number;
}

export interface AutonomousData {
  currentCycle: number;
  nextScheduled: string;
  skipRate: string;
  history: AutonomousCycle[];
  chartData: ChartDataPoint[];
}

export function useAutonomous(authToken: string | null, enableSSE = true) {
  const [data, setData] = useState<AutonomousData | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchData = useCallback(async () => {
    if (!authToken) {
      setError('Not authenticated');
      setIsLoading(false);
      return;
    }

    try {
      setIsLoading(true);
      setError(null);

      const response = await fetch('/api/autonomous/history', {
        headers: {
          'Authorization': `Bearer ${authToken}`,
        },
      });

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }

      const result = await response.json();
      setData(result);
    } catch (err) {
      console.error('Failed to fetch autonomous data:', err);
      setError(err instanceof Error ? err.message : 'Unknown error');
    } finally {
      setIsLoading(false);
    }
  }, [authToken]);

  // Initial fetch
  useEffect(() => {
    fetchData();
  }, [fetchData]);

  // SSE real-time updates
  useEffect(() => {
    if (!enableSSE || !authToken) return;

    const eventSource = new EventSource('/api/stream');

    eventSource.addEventListener('autonomous', (e) => {
      try {
        const update = JSON.parse(e.data);
        setData(update);
      } catch (err) {
        console.error('Failed to parse autonomous SSE event:', err);
      }
    });

    eventSource.onerror = (err) => {
      console.error('SSE error:', err);
    };

    return () => {
      eventSource.close();
    };
  }, [enableSSE, authToken]);

  return {
    data,
    isLoading,
    error,
    refresh: fetchData,
  };
}
