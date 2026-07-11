/**
 * Backend Manager - Manages all MCP backends lifecycle
 */

import { Backend, BackendConfig, MCPRequest, MCPResponse, ConnectorConfig } from '../types';
import { HttpBackend } from './httpBackend';
import { StdioBackend } from './stdioBackend';

export class BackendManager {
  private backends = new Map<string, Backend>();
  private config: ConnectorConfig;

  constructor(config: ConnectorConfig) {
    this.config = config;
  }

  async initialize(): Promise<void> {
    console.log('[BackendManager] Initializing backends...');

    for (const [name, backendConfig] of Object.entries(this.config.backends)) {
      try {
        const backend = this.createBackend(name, backendConfig);
        this.backends.set(name, backend);

        // Start STDIO backends
        if (backendConfig.type === 'stdio' && backend.start) {
          await backend.start();
        }

        console.log(`[BackendManager] ${name} (${backendConfig.type}) initialized`);
      } catch (error) {
        console.error(`[BackendManager] Failed to initialize ${name}:`, error);
        throw error;
      }
    }

    console.log(`[BackendManager] ${this.backends.size} backends ready`);
  }

  private createBackend(name: string, config: BackendConfig): Backend {
    switch (config.type) {
      case 'http':
        return new HttpBackend(name, config);
      case 'stdio':
        return new StdioBackend(name, config);
      default:
        throw new Error(`Unknown backend type: ${(config as BackendConfig).type}`);
    }
  }

  getBackend(name: string): Backend | undefined {
    return this.backends.get(name);
  }

  async call(backendName: string, request: MCPRequest): Promise<MCPResponse> {
    const backend = this.backends.get(backendName);
    if (!backend) {
      return {
        jsonrpc: '2.0',
        error: {
          code: -32602,
          message: `Backend not found: ${backendName}`,
        },
        id: request.id,
      };
    }

    return backend.call(request);
  }

  async healthCheck(): Promise<Record<string, boolean>> {
    const health: Record<string, boolean> = {};

    for (const [name, backend] of this.backends) {
      try {
        health[name] = await backend.healthCheck();
      } catch (error) {
        health[name] = false;
      }
    }

    return health;
  }

  async shutdown(): Promise<void> {
    console.log('[BackendManager] Shutting down backends...');

    for (const [name, backend] of this.backends) {
      if (backend.stop) {
        try {
          await backend.stop();
          console.log(`[BackendManager] ${name} stopped`);
        } catch (error) {
          console.error(`[BackendManager] Error stopping ${name}:`, error);
        }
      }
    }
  }

  getBackendNames(): string[] {
    return Array.from(this.backends.keys());
  }
}
