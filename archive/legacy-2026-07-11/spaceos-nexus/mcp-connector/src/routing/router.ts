/**
 * Router - Tool routing and permission management
 */

import { ConnectorConfig } from '../types';

export class Router {
  private routing: Record<string, string>;
  private permissions: Record<string, string[] | { backends?: string[]; tools?: string[] }>;

  constructor(config: ConnectorConfig) {
    this.routing = config.routing;
    this.permissions = config.permissions;
  }

  /**
   * Get backend name for a tool
   */
  getBackend(toolName: string): string | null {
    return this.routing[toolName] || null;
  }

  /**
   * Check if terminal has permission to use a tool
   */
  hasPermission(terminal: string, toolName: string): boolean {
    const terminalPerms = this.permissions[terminal];

    // No permission config = deny
    if (!terminalPerms) {
      console.warn(`[Router] Terminal '${terminal}' has no permission config`);
      return false;
    }

    // Array format: ["*"] or ["tool1", "tool2"]
    if (Array.isArray(terminalPerms)) {
      return terminalPerms.includes('*') || terminalPerms.includes(toolName);
    }

    // Object format: { backends: [...], tools: [...] }
    const tools = terminalPerms.tools || [];
    if (tools.includes('*') || tools.includes(toolName)) {
      return true;
    }

    // Check backend access
    const backends = terminalPerms.backends || [];
    const targetBackend = this.routing[toolName];
    if (targetBackend && backends.includes(targetBackend)) {
      return true;
    }

    return false;
  }

  /**
   * Get all tools for a terminal (based on permissions)
   */
  getTerminalTools(terminal: string): string[] {
    const terminalPerms = this.permissions[terminal];
    if (!terminalPerms) return [];

    if (Array.isArray(terminalPerms)) {
      if (terminalPerms.includes('*')) {
        return Object.keys(this.routing);
      }
      return terminalPerms.filter((t) => this.routing[t]);
    }

    const tools = terminalPerms.tools || [];
    if (tools.includes('*')) {
      return Object.keys(this.routing);
    }

    // Add tools from allowed backends
    const backends = terminalPerms.backends || [];
    const backendTools = Object.entries(this.routing)
      .filter(([_, backend]) => backends.includes(backend))
      .map(([tool, _]) => tool);

    return [...new Set([...tools, ...backendTools])].filter((t) => t !== '*');
  }

  /**
   * Update routing (for hot-reload)
   */
  updateRouting(routing: Record<string, string>): void {
    this.routing = routing;
    console.log('[Router] Routing updated');
  }

  /**
   * Update permissions (for hot-reload)
   */
  updatePermissions(permissions: ConnectorConfig['permissions']): void {
    this.permissions = permissions;
    console.log('[Router] Permissions updated');
  }

  /**
   * Get all registered tools
   */
  getAllTools(): string[] {
    return Object.keys(this.routing);
  }

  /**
   * Get routing table (for debugging)
   */
  getRoutingTable(): Record<string, string> {
    return { ...this.routing };
  }
}
