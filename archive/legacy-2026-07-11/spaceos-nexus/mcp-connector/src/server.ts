/**
 * MCP Connector Server
 *
 * Aggregates multiple MCP servers into a single endpoint.
 * Provides routing, permission management, and audit logging.
 */

import express from 'express';
import cors from 'cors';
import { loadConfig, validateConfig, getDefaultPerformance } from './config/loader';
import { BackendManager } from './backends/manager';
import { Router } from './routing/router';
import { AuditLogger } from './audit/logger';
import { CircuitBreakerRegistry } from './health/circuitBreaker';
import { MCPRequest, MCPResponse, ConnectorConfig } from './types';
import path from 'path';

const CONFIG_PATH = process.env.CONFIG_PATH || './config.yaml';
const PORT = parseInt(process.env.PORT || '3457', 10);

async function main() {
  console.log('='.repeat(60));
  console.log('  MCP Connector - SpaceOS Central MCP Aggregator');
  console.log('='.repeat(60));

  // Load configuration
  const configPath = path.resolve(__dirname, '..', CONFIG_PATH);
  console.log(`[Config] Loading from: ${configPath}`);

  let config: ConnectorConfig;
  try {
    config = loadConfig(configPath);
    validateConfig(config);
    console.log(`[Config] Loaded successfully (v${config.version})`);
  } catch (error) {
    console.error('[Config] Failed to load:', error);
    process.exit(1);
  }

  // Initialize components
  const performance = config.performance || getDefaultPerformance();
  const backendManager = new BackendManager(config);
  const router = new Router(config);
  const auditLogger = new AuditLogger(config.audit);
  const circuitBreakers = new CircuitBreakerRegistry({
    failureThreshold: performance.circuit_breaker.failure_threshold,
    timeout: performance.circuit_breaker.timeout,
    halfOpenAfter: performance.circuit_breaker.half_open_after,
  });

  // Initialize backends
  try {
    await backendManager.initialize();
  } catch (error) {
    console.error('[BackendManager] Initialization failed:', error);
    process.exit(1);
  }

  // Create Express app
  const app = express();
  app.use(cors());
  app.use(express.json({ limit: '50mb' }));

  // MCP endpoint
  app.post('/mcp', async (req, res) => {
    const terminal = req.headers['x-terminal'] as string;
    const authHeader = req.headers['authorization'] as string;
    const mcpRequest = req.body as MCPRequest;

    const startTime = Date.now();
    let toolName = 'unknown';
    let backendName = 'unknown';

    try {
      // Validate terminal header
      if (!terminal) {
        throw new Error('Missing X-Terminal header');
      }

      // Extract tool name from MCP request
      // MCP tools/call format: { method: 'tools/call', params: { name: 'tool_name', arguments: {...} } }
      if (mcpRequest.method === 'tools/call') {
        toolName = mcpRequest.params?.name || 'unknown';
      } else if (mcpRequest.method === 'tools/list') {
        // Tools list request - return all tools for terminal
        const tools = router.getTerminalTools(terminal);
        const toolDefinitions = tools.map((name) => ({
          name,
          description: `Tool routed via MCP Connector`,
          inputSchema: { type: 'object' as const },
        }));

        const response: MCPResponse = {
          jsonrpc: '2.0',
          result: { tools: toolDefinitions },
          id: mcpRequest.id,
        };

        auditLogger.log({
          terminal,
          tool: 'tools/list',
          backend: 'connector',
          latency: Date.now() - startTime,
          status: 'success',
        });

        return res.json(response);
      } else {
        throw new Error(`Unknown MCP method: ${mcpRequest.method}`);
      }

      // Permission check
      if (!router.hasPermission(terminal, toolName)) {
        auditLogger.log({
          terminal,
          tool: toolName,
          backend: 'denied',
          latency: Date.now() - startTime,
          status: 'permission_denied',
        });

        const response: MCPResponse = {
          jsonrpc: '2.0',
          error: {
            code: -32600,
            message: `Permission denied: terminal '${terminal}' cannot use tool '${toolName}'`,
          },
          id: mcpRequest.id,
        };
        return res.status(403).json(response);
      }

      // Get backend for tool
      backendName = router.getBackend(toolName) || 'unknown';
      if (backendName === 'unknown') {
        throw new Error(`No backend configured for tool: ${toolName}`);
      }

      // Get circuit breaker for backend
      const circuitBreaker = circuitBreakers.getBreaker(backendName);

      // Execute with circuit breaker
      const result = await circuitBreaker.execute(async () => {
        return backendManager.call(backendName, mcpRequest);
      });

      // Log success
      auditLogger.log({
        terminal,
        tool: toolName,
        backend: backendName,
        latency: Date.now() - startTime,
        status: 'success',
        requestId: mcpRequest.id,
      });

      res.json(result);
    } catch (error: unknown) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error';

      // Log error
      auditLogger.log({
        terminal: terminal || 'unknown',
        tool: toolName,
        backend: backendName,
        latency: Date.now() - startTime,
        status: 'error',
        error: errorMessage,
        requestId: mcpRequest.id,
      });

      const response: MCPResponse = {
        jsonrpc: '2.0',
        error: {
          code: -32603,
          message: errorMessage,
        },
        id: mcpRequest.id,
      };

      res.status(500).json(response);
    }
  });

  // Health endpoint
  app.get('/health', async (req, res) => {
    const backendHealth = await backendManager.healthCheck();
    const circuitBreakerStats = circuitBreakers.getAllStats();

    const allHealthy = Object.values(backendHealth).every((h) => h);
    const anyOpen = Object.values(circuitBreakerStats).some((s) => s.state === 'open');

    const status = allHealthy && !anyOpen ? 'healthy' : anyOpen ? 'degraded' : 'unhealthy';

    res.status(status === 'healthy' ? 200 : 503).json({
      status,
      version: config.version,
      backends: backendHealth,
      circuitBreakers: circuitBreakerStats,
      uptime: process.uptime(),
    });
  });

  // Tools endpoint (for debugging)
  app.get('/tools', (req, res) => {
    const terminal = req.query.terminal as string;

    if (terminal) {
      const tools = router.getTerminalTools(terminal);
      res.json({ terminal, tools, count: tools.length });
    } else {
      res.json({ routing: router.getRoutingTable() });
    }
  });

  // Backends endpoint (for debugging)
  app.get('/backends', async (req, res) => {
    const health = await backendManager.healthCheck();
    res.json({
      backends: backendManager.getBackendNames(),
      health,
    });
  });

  // Graceful shutdown
  process.on('SIGTERM', async () => {
    console.log('[Server] SIGTERM received, shutting down...');
    await backendManager.shutdown();
    process.exit(0);
  });

  process.on('SIGINT', async () => {
    console.log('[Server] SIGINT received, shutting down...');
    await backendManager.shutdown();
    process.exit(0);
  });

  // Start server
  app.listen(PORT, () => {
    console.log('');
    console.log(`[Server] MCP Connector listening on http://localhost:${PORT}`);
    console.log(`[Server] Health check: http://localhost:${PORT}/health`);
    console.log(`[Server] Tools list: http://localhost:${PORT}/tools`);
    console.log(`[Server] Backends: http://localhost:${PORT}/backends`);
    console.log('');
    console.log(`[Server] Backends configured: ${backendManager.getBackendNames().join(', ')}`);
    console.log(`[Server] Tools routed: ${router.getAllTools().length}`);
    console.log('='.repeat(60));
  });
}

main().catch((error) => {
  console.error('[Server] Fatal error:', error);
  process.exit(1);
});
