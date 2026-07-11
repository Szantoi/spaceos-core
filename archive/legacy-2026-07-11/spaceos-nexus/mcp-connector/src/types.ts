/**
 * MCP Connector Types
 */

export interface BackendConfig {
  type: 'http' | 'stdio';
  url?: string;
  command?: string;
  args?: string[];
  headers?: Record<string, string>;
  env?: Record<string, string>;
  health_check?: string;
  timeout?: number;
  auto_restart?: boolean;
  health_check_interval?: number;
}

export interface AuthConfig {
  enabled: boolean;
  tokens: Record<string, string>;
}

export interface AuditConfig {
  enabled: boolean;
  log_file: string;
  log_format: 'json' | 'text';
  log_level: string;
  metrics_enabled: boolean;
  metrics_port?: number;
}

export interface PerformanceConfig {
  request_timeout: number;
  max_concurrent_requests: number;
  circuit_breaker: {
    enabled: boolean;
    failure_threshold: number;
    timeout: number;
    half_open_after: number;
  };
}

export interface ConnectorConfig {
  version: string;
  backends: Record<string, BackendConfig>;
  routing: Record<string, string>;
  permissions: Record<string, string[] | { backends?: string[]; tools?: string[] }>;
  authentication?: AuthConfig;
  audit: AuditConfig;
  performance?: PerformanceConfig;
}

export interface MCPRequest {
  jsonrpc: '2.0';
  method: string;
  params?: {
    name?: string;
    arguments?: Record<string, unknown>;
    [key: string]: unknown;
  };
  id?: number | string;
}

export interface MCPResponse {
  jsonrpc: '2.0';
  result?: unknown;
  error?: {
    code: number;
    message: string;
    data?: unknown;
  };
  id?: number | string;
}

export interface AuditEntry {
  timestamp: string;
  terminal: string;
  tool: string;
  backend: string;
  latency: number;
  status: 'success' | 'error' | 'permission_denied';
  error?: string;
  requestId?: string | number;
}

export interface Backend {
  getName(): string;
  call(request: MCPRequest): Promise<MCPResponse>;
  healthCheck(): Promise<boolean>;
  getTools(): Promise<string[]>;
  start?(): Promise<void>;
  stop?(): Promise<void>;
}
