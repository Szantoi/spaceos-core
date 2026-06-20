// Dashboard Types

export type TerminalState = 'IDLE' | 'WORKING' | 'OFFLINE';

export interface Terminal {
  id: string;
  name: string;
  type: 'priority' | 'product' | 'support';
  state: TerminalState;
  taskId?: string;
  lastActivity?: string;
  unreadCount: number;
  workdir?: string;
}

export interface ServiceHealth {
  status: 'ok' | 'degraded' | 'down';
  vectorBackend: string;
  embeddingBackend: string;
  documents: number;
  port: number;
}

export interface DashboardStats {
  totalTerminals: number;
  activeTerminals: number;
  idleTerminals: number;
  offlineTerminals: number;
  totalUnread: number;
  documentsIndexed: number;
}

export interface DashboardData {
  terminals: Terminal[];
  stats: DashboardStats;
  serviceHealth: ServiceHealth;
  timestamp: string;
}

// Terminal categories
export const TERMINAL_CATEGORIES: Record<string, { label: string; terminals: string[] }> = {
  priority: {
    label: 'Priority',
    terminals: ['root', 'conductor'],
  },
  product: {
    label: 'Product',
    terminals: ['kernel', 'orchestrator', 'fe', 'joinery', 'abstractions', 'cutting', 'inventory', 'procurement', 'sales', 'identity'],
  },
  support: {
    label: 'Support',
    terminals: ['infra', 'e2e', 'architect', 'librarian', 'nexus'],
  },
};
