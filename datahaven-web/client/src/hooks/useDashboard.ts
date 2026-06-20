import { useState, useCallback, useEffect, useRef } from 'react';
import type { DashboardData, Terminal, ServiceHealth, DashboardStats, TerminalState } from '../types/dashboard';

const KNOWLEDGE_URL = 'http://localhost:3456';

// Module-level fetch functions (stable references)
function getHeaders(authToken: string | null): HeadersInit {
  return authToken
    ? { 'Authorization': `Bearer ${authToken}`, 'Content-Type': 'application/json' }
    : { 'Content-Type': 'application/json' };
}

async function fetchHealth(): Promise<ServiceHealth> {
  const res = await fetch(`${KNOWLEDGE_URL}/health`);
  if (!res.ok) throw new Error('Health check failed');
  return res.json();
}

async function fetchTerminals(headers: HeadersInit): Promise<Terminal[]> {
  const res = await fetch(`${KNOWLEDGE_URL}/mcp`, {
    method: 'POST',
    headers,
    body: JSON.stringify({
      jsonrpc: '2.0',
      method: 'tools/call',
      params: { name: 'list_terminals', arguments: {} },
      id: 1,
    }),
  });

  if (!res.ok) throw new Error('Failed to fetch terminals');
  const json = await res.json();

  if (json.error) throw new Error(json.error.message);

  const content = json.result?.content?.[0]?.text;
  if (!content) return [];

  try {
    const parsed = JSON.parse(content);
    return parsed.terminals || [];
  } catch {
    return [];
  }
}

async function fetchTerminalStatus(
  terminal: string,
  headers: HeadersInit
): Promise<{ state: TerminalState; taskId?: string }> {
  try {
    const res = await fetch(`${KNOWLEDGE_URL}/mcp`, {
      method: 'POST',
      headers,
      body: JSON.stringify({
        jsonrpc: '2.0',
        method: 'tools/call',
        params: { name: 'get_terminal_status', arguments: { terminal } },
        id: 2,
      }),
    });

    if (!res.ok) return { state: 'OFFLINE' };
    const json = await res.json();

    if (json.error) return { state: 'OFFLINE' };

    const content = json.result?.content?.[0]?.text;
    if (!content) return { state: 'IDLE' };

    const parsed = JSON.parse(content);
    return {
      state: parsed.state?.toUpperCase() || 'IDLE',
      taskId: parsed.taskId,
    };
  } catch {
    return { state: 'OFFLINE' };
  }
}

async function fetchInboxCount(terminal: string, headers: HeadersInit): Promise<number> {
  try {
    const res = await fetch(`${KNOWLEDGE_URL}/mcp`, {
      method: 'POST',
      headers,
      body: JSON.stringify({
        jsonrpc: '2.0',
        method: 'tools/call',
        params: { name: 'list_inbox', arguments: { terminal, status: 'UNREAD' } },
        id: 3,
      }),
    });

    if (!res.ok) return 0;
    const json = await res.json();

    if (json.error) return 0;

    const content = json.result?.content?.[0]?.text;
    if (!content) return 0;

    const parsed = JSON.parse(content);
    return parsed.count || parsed.messages?.length || 0;
  } catch {
    return 0;
  }
}

export function useDashboard(authToken: string | null, autoRefresh = true) {
  const [data, setData] = useState<DashboardData | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Use ref to track mounted state
  const mountedRef = useRef(true);

  // Load all dashboard data
  const loadDashboard = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    const headers = getHeaders(authToken);

    try {
      // Fetch health and terminals in parallel
      const [health, terminalsList] = await Promise.all([
        fetchHealth(),
        fetchTerminals(headers),
      ]);

      // Fetch status and inbox counts for each terminal
      const terminalsWithStatus: Terminal[] = await Promise.all(
        terminalsList.map(async (t: any) => {
          const [status, unreadCount] = await Promise.all([
            fetchTerminalStatus(t.id || t.name, headers),
            fetchInboxCount(t.id || t.name, headers),
          ]);

          return {
            id: t.id || t.name,
            name: t.name || t.id,
            type: t.type || 'product',
            state: status.state,
            taskId: status.taskId,
            unreadCount,
            workdir: t.workdir,
          };
        })
      );

      // Calculate stats
      const stats: DashboardStats = {
        totalTerminals: terminalsWithStatus.length,
        activeTerminals: terminalsWithStatus.filter(t => t.state === 'WORKING').length,
        idleTerminals: terminalsWithStatus.filter(t => t.state === 'IDLE').length,
        offlineTerminals: terminalsWithStatus.filter(t => t.state === 'OFFLINE').length,
        totalUnread: terminalsWithStatus.reduce((sum, t) => sum + t.unreadCount, 0),
        documentsIndexed: health.documents,
      };

      // Only update state if still mounted
      if (mountedRef.current) {
        setData({
          terminals: terminalsWithStatus,
          stats,
          serviceHealth: health,
          timestamp: new Date().toISOString(),
        });
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
