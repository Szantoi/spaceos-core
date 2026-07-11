/**
 * src/mcp/index.ts
 *
 * Central export point for MCP server utilities and services.
 * Enables cleaner imports across the codebase.
 */

// Notification debouncer (AC-1..6 of TASK-14-10)
export { NotificationDebouncer } from './notifications/NotificationDebouncer';
export type {
  INotification,
  NotificationDebouncerOptions,
  NotificationDebouncerStats,
  ToolNotification,
  ResourceNotification,
} from './notifications/NotificationTypes';

// RBAC & middleware
export { RbacFilter } from './RbacFilter';

// MCP router & server
export { createMcpRouter } from './mcpRouter';
export { createMcpServerRouter, createPluginManager } from './mcpServer';

// Session management
export { SessionManager } from './SessionManager';

// Schema & validation
export { SchemaVersionManager } from './SchemaVersionManager';
export { InputValidator } from './InputValidator';

// Error handling
export * from './ErrorCodes';
export * from './ErrorResponses';

// Transports (AC-14-01: Transport Abstraction Foundation)
export { HTTPTransport } from './transports/HTTPTransport';
export { StdioTransport } from './transports/StdioTransport';
export { TransportFactory, ConfigurationError } from './transports/TransportFactory';
export { ErrorDiagnoser } from './transports/ErrorDiagnoser';
export type {
  ITransport,
  BaseTransport,
  TransportConfig,
  TransportErrorContext,
  TransportInfo,
} from './transports/ITransport';
export { TransportType, TransportState, TransportError } from './transports/ITransport';
