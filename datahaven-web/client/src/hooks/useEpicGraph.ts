import { useState, useCallback, useRef, useEffect } from 'react';
import type { WorkflowGraph, CriticalPathResult, ParallelGroupsResult } from '../types/graph';

const BASE_URL = '/api/graph';

export function useEpicGraph(authToken: string | null) {
  const [graph, setGraph] = useState<WorkflowGraph | null>(null);
  const [mermaidCode, setMermaidCode] = useState<string | null>(null);
  const [criticalPath, setCriticalPath] = useState<CriticalPathResult | null>(null);
  const [parallelGroups, setParallelGroups] = useState<ParallelGroupsResult | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const mountedRef = useRef(true);

  /**
   * Fetch epic graph data
   */
  const loadGraph = useCallback(async () => {
    if (!authToken) return;

    setIsLoading(true);
    setError(null);

    try {
      const res = await fetch(`${BASE_URL}/epics`, {
        headers: {
          'Authorization': `Bearer ${authToken}`,
          'Content-Type': 'application/json'
        }
      });

      if (!res.ok) {
        throw new Error(`Failed to fetch graph: ${res.status}`);
      }

      const graphData = await res.json();

      if (mountedRef.current) {
        setGraph(graphData);
      }
    } catch (err) {
      if (mountedRef.current) {
        const message = err instanceof Error ? err.message : 'Failed to load graph';
        setError(message);
      }
    } finally {
      if (mountedRef.current) {
        setIsLoading(false);
      }
    }
  }, [authToken]);

  /**
   * Fetch Mermaid diagram code
   */
  const loadMermaid = useCallback(async () => {
    if (!authToken) return;

    try {
      const res = await fetch(`${BASE_URL}/mermaid/epic/EPICS`, {
        headers: {
          'Authorization': `Bearer ${authToken}`,
        }
      });

      if (!res.ok) {
        throw new Error(`Failed to fetch mermaid: ${res.status}`);
      }

      const mermaidText = await res.text();

      if (mountedRef.current) {
        setMermaidCode(mermaidText);
      }
    } catch (err) {
      console.error('Failed to load mermaid diagram:', err);
    }
  }, [authToken]);

  /**
   * Fetch critical path
   */
  const loadCriticalPath = useCallback(async () => {
    if (!authToken) return;

    try {
      const res = await fetch(`${BASE_URL}/critical-path/epic/EPICS`, {
        headers: {
          'Authorization': `Bearer ${authToken}`,
          'Content-Type': 'application/json'
        }
      });

      if (!res.ok) {
        throw new Error(`Failed to fetch critical path: ${res.status}`);
      }

      const pathData = await res.json();

      if (mountedRef.current) {
        setCriticalPath(pathData);
      }
    } catch (err) {
      console.error('Failed to load critical path:', err);
    }
  }, [authToken]);

  /**
   * Fetch parallel groups
   */
  const loadParallelGroups = useCallback(async () => {
    if (!authToken) return;

    try {
      const res = await fetch(`${BASE_URL}/parallel/epic/EPICS`, {
        headers: {
          'Authorization': `Bearer ${authToken}`,
          'Content-Type': 'application/json'
        }
      });

      if (!res.ok) {
        throw new Error(`Failed to fetch parallel groups: ${res.status}`);
      }

      const groupsData = await res.json();

      if (mountedRef.current) {
        setParallelGroups(groupsData);
      }
    } catch (err) {
      console.error('Failed to load parallel groups:', err);
    }
  }, [authToken]);

  /**
   * Load all data
   */
  const loadAll = useCallback(async () => {
    await Promise.all([
      loadGraph(),
      loadMermaid(),
      loadCriticalPath(),
      loadParallelGroups(),
    ]);
  }, [loadGraph, loadMermaid, loadCriticalPath, loadParallelGroups]);

  useEffect(() => {
    mountedRef.current = true;
    return () => {
      mountedRef.current = false;
    };
  }, []);

  return {
    graph,
    mermaidCode,
    criticalPath,
    parallelGroups,
    isLoading,
    error,
    loadGraph,
    loadMermaid,
    loadCriticalPath,
    loadParallelGroups,
    loadAll,
  };
}
