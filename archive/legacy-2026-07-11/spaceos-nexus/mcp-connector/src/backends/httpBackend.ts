/**
 * HTTP Backend - Proxies MCP requests to HTTP-based MCP servers
 */

import axios, { AxiosInstance, AxiosError } from 'axios';
import { Backend, BackendConfig, MCPRequest, MCPResponse } from '../types';

export class HttpBackend implements Backend {
  private client: AxiosInstance;
  private name: string;
  private url: string;
  private healthCheckPath?: string;
  private timeout: number;

  constructor(name: string, config: BackendConfig) {
    this.name = name;
    this.url = config.url!;
    this.healthCheckPath = config.health_check;
    this.timeout = config.timeout || 30000;

    this.client = axios.create({
      baseURL: this.url,
      headers: config.headers || {},
      timeout: this.timeout,
    });
  }

  getName(): string {
    return this.name;
  }

  async call(request: MCPRequest): Promise<MCPResponse> {
    try {
      const response = await this.client.post('', request);
      return response.data as MCPResponse;
    } catch (error) {
      const axiosError = error as AxiosError;
      console.error(`[${this.name}] Request failed:`, axiosError.message);

      // Return MCP error response
      return {
        jsonrpc: '2.0',
        error: {
          code: -32603,
          message: `Backend ${this.name} error: ${axiosError.message}`,
        },
        id: request.id,
      };
    }
  }

  async healthCheck(): Promise<boolean> {
    if (!this.healthCheckPath) {
      // No health check configured, assume healthy
      return true;
    }

    try {
      const healthUrl = this.url.replace(/\/mcp$/, '') + this.healthCheckPath;
      const response = await axios.get(healthUrl, { timeout: 5000 });
      return response.status === 200;
    } catch (error) {
      const axiosError = error as AxiosError;
      console.error(`[${this.name}] Health check failed:`, axiosError.message);
      return false;
    }
  }

  async getTools(): Promise<string[]> {
    // HTTP backends don't expose tools list directly
    // Tools are defined in config.yaml routing section
    return [];
  }
}
