import { useState, useCallback, useEffect, useRef } from 'react';
import type { DashboardData } from '../types/dashboard';

// Use server-side aggregation endpoint
const API_URL = '/api/dashboard';

export function useDashboard(authToken: string | null, autoRefresh = true) {
  const [data, setData] = useState<DashboardData | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Use ref to track mounted state
  const mountedRef = useRef(true);

  // Load all dashboard data from server endpoint
  const loadDashboard = useCallback(async () => {
    if (!authToken) return;

    setIsLoading(true);
    setError(null);

    try {
      const res = await fetch(API_URL, {
        headers: {
          'Authorization': `Bearer ${authToken}`,
          'Content-Type': 'application/json'
        }
      });

      if (!res.ok) {
        throw new Error(`Failed to fetch dashboard: ${res.status}`);
      }

      const dashboardData = await res.json();

      // Only update state if still mounted
      if (mountedRef.current) {
        setData(dashboardData);
      }
    } catch (err) {
      if (mountedRef.current) {
        const message = err instanceof Error ? err.message : 'Failed to load dashboard';
        setError(message);
      }
    } finally {
      if (mountedRef.current) {
        setIsLoading(false);
      }
    }
  }, [authToken]);

  // Auto-refresh effect
  useEffect(() => {
    mountedRef.current = true;

    if (autoRefresh) {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      loadDashboard();
      const interval = setInterval(loadDashboard, 30000);
      return () => {
        mountedRef.current = false;
        clearInterval(interval);
      };
    }

    return () => {
      mountedRef.current = false;
    };
  }, [loadDashboard, autoRefresh]);

  return {
    data,
    isLoading,
    error,
    refresh: loadDashboard,
  };
}
