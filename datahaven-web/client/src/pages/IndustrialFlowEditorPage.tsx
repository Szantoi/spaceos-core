import { useState, useEffect } from 'react';
import { useAuth } from '../hooks/useAuth';
import { useEpicGraph } from '../hooks/useEpicGraph';
import { EpicGraph } from '../components/Graph/EpicGraph';
import { NodeDetails } from '../components/Graph/NodeDetails';
import type { GraphNode } from '../types/graph';

/**
 * IndustrialFlowEditorPage
 *
 * Flow editor and epic dependency graph visualization.
 * Features:
 * - Mermaid diagram rendering
 * - Critical path highlighting
 * - Node details panel
 * - Status indicators
 */
export function IndustrialFlowEditorPage() {
  const { authToken } = useAuth();
  const {
    graph,
    mermaidCode,
    criticalPath,
    isLoading,
    error,
    loadAll,
  } = useEpicGraph(authToken);

  const [selectedNode, setSelectedNode] = useState<GraphNode | null>(null);

  // Load data on mount
  useEffect(() => {
    if (authToken) {
      loadAll();
    }
  }, [authToken, loadAll]);

  // Handle node click (simplified for now - would need Mermaid click events)
  const handleNodeClick = (nodeId: string) => {
    if (!graph) return;
    const node = graph.nodes.find(n => n.id === nodeId);
    if (node) {
      setSelectedNode(node);
    }
  };

  return (
    <div className="h-full overflow-hidden bg-gray-50">
      {/* Header */}
      <div className="bg-white border-b border-gray-200 px-6 py-4">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">Flow Editor</h1>
            <p className="text-sm text-gray-500 mt-1">
              Epic dependency graph and workflow visualization
            </p>
          </div>

          {/* Stats */}
          {graph && (
            <div className="flex items-center gap-6">
              <div className="text-center">
                <div className="text-2xl font-bold text-gray-900">{graph.nodes.length}</div>
                <div className="text-xs text-gray-500 uppercase tracking-wide">Nodes</div>
              </div>
              {criticalPath && (
                <div className="text-center">
                  <div className="text-2xl font-bold text-red-600">{criticalPath.path.length}</div>
                  <div className="text-xs text-gray-500 uppercase tracking-wide">Critical Path</div>
                </div>
              )}
            </div>
          )}
        </div>

        {/* Refresh Button */}
        <div className="mt-4 flex items-center gap-4">
          <button
            onClick={loadAll}
            disabled={isLoading}
            className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium rounded-lg transition disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {isLoading ? 'Loading...' : 'Refresh Graph'}
          </button>

          {error && (
            <div className="text-sm text-red-600">
              Error: {error}
            </div>
          )}
        </div>
      </div>

      {/* Main Content */}
      <div className="relative h-full">
        {isLoading && !mermaidCode ? (
          <div className="flex items-center justify-center h-full">
            <div className="text-center">
              <div className="animate-spin rounded-full h-12 w-12 border-4 border-blue-500 border-t-transparent mx-auto mb-4"></div>
              <p className="text-gray-600">Loading workflow graph...</p>
            </div>
          </div>
        ) : error ? (
          <div className="flex items-center justify-center h-full">
            <div className="bg-red-50 border border-red-200 rounded-lg p-6 max-w-md">
              <h3 className="text-lg font-semibold text-red-900 mb-2">Error Loading Graph</h3>
              <p className="text-sm text-red-700">{error}</p>
              <button
                onClick={loadAll}
                className="mt-4 px-4 py-2 bg-red-600 hover:bg-red-700 text-white text-sm font-medium rounded-lg transition"
              >
                Retry
              </button>
            </div>
          </div>
        ) : (
          <div className="h-full p-6 overflow-auto">
            {/* Legend */}
            <div className="mb-4 bg-white border border-gray-200 rounded-lg p-4 inline-block">
              <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-2">
                Status Legend
              </h3>
              <div className="flex items-center gap-4">
                <div className="flex items-center gap-2">
                  <div className="w-3 h-3 rounded-full bg-yellow-300"></div>
                  <span className="text-xs text-gray-700">Pending</span>
                </div>
                <div className="flex items-center gap-2">
                  <div className="w-3 h-3 rounded-full bg-blue-400"></div>
                  <span className="text-xs text-gray-700">In Progress</span>
                </div>
                <div className="flex items-center gap-2">
                  <div className="w-3 h-3 rounded-full bg-green-400"></div>
                  <span className="text-xs text-gray-700">Completed</span>
                </div>
                <div className="flex items-center gap-2">
                  <div className="w-3 h-3 rounded-full bg-red-400"></div>
                  <span className="text-xs text-gray-700">Blocked</span>
                </div>
              </div>
            </div>

            {/* Graph Visualization */}
            <div className="bg-white border border-gray-200 rounded-lg p-6">
              {mermaidCode ? (
                <EpicGraph mermaidCode={mermaidCode} />
              ) : (
                <div className="flex items-center justify-center h-64">
                  <p className="text-gray-500">No graph data available</p>
                </div>
              )}
            </div>

            {/* Node List (for clicking) */}
            {graph && graph.nodes.length > 0 && (
              <div className="mt-6 bg-white border border-gray-200 rounded-lg p-6">
                <h3 className="text-sm font-semibold text-gray-900 mb-4">
                  Nodes ({graph.nodes.length})
                </h3>
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
                  {graph.nodes.map((node) => (
                    <button
                      key={node.id}
                      onClick={() => handleNodeClick(node.id)}
                      className="text-left p-3 bg-gray-50 hover:bg-gray-100 border border-gray-200 rounded-lg transition"
                    >
                      <div className="flex items-center justify-between mb-1">
                        <span className="text-xs font-mono text-gray-500">{node.id}</span>
                        <span
                          className="w-2 h-2 rounded-full"
                          style={{
                            backgroundColor: {
                              pending: '#FCD34D',
                              active: '#60A5FA',
                              done: '#34D399',
                              blocked: '#F87171',
                            }[node.status]
                          }}
                        ></span>
                      </div>
                      <div className="text-sm font-medium text-gray-900">{node.name}</div>
                      <div className="text-xs text-gray-500 mt-1">
                        {node.depends_on.length} deps • {node.triggers.length} triggers
                      </div>
                    </button>
                  ))}
                </div>
              </div>
            )}
          </div>
        )}

        {/* Node Details Panel */}
        <NodeDetails
          node={selectedNode}
          onClose={() => setSelectedNode(null)}
        />
      </div>
    </div>
  );
}
