// Discovery Track Types
export interface DiscoveryItem {
  id: string;
  title: string;
  path: string;
  status: 'ideas' | 'selected' | 'debate' | 'consensus' | 'queue';
  priority?: 'critical' | 'high' | 'medium' | 'low';
  complexity?: 'simple' | 'medium' | 'complex';
  assignee?: string;
  terminal?: string;
  dor?: DefinitionOfReady;
}

export interface DefinitionOfReady {
  ready: boolean;
  total: number;
  met: number;
  criteria?: Record<string, boolean>;
}

export interface DiscoveryColumn {
  ideas: DiscoveryItem[];
  selected: DiscoveryItem[];
  debate: DiscoveryItem[];
  consensus: DiscoveryItem[];
  queue: DiscoveryItem[];
}

export interface DiscoveryBoard {
  columns: DiscoveryColumn;
  totals: {
    ideas: number;
    selected: number;
    debate: number;
    consensus: number;
    queue: number;
  };
}

// Delivery Track Types
export interface DeliveryMessage {
  id: string;
  title: string;
  path: string;
  from: string;
  to: string;
  type: 'task' | 'question' | 'done' | 'blocked' | 'escalation';
  priority: 'critical' | 'high' | 'medium' | 'low';
  status: 'UNREAD' | 'READ' | 'PROCESSING' | 'DONE';
  model?: 'opus' | 'sonnet' | 'haiku';
  created: string;
  ref?: string;
}

export interface DeliverySwimlane {
  terminal: string;
  sessionActive: boolean;
  columns: {
    inbox: DeliveryMessage[];
    active: DeliveryMessage[];
    review: DeliveryMessage[];
    done: DeliveryMessage[];
  };
  totals: {
    inbox: number;
    active: number;
    review: number;
    done: number;
    archive: number;
  };
}

export interface DeliveryBoard {
  swimlanes: DeliverySwimlane[];
  activeSessions: string[];
  totals: {
    inbox: number;
    active: number;
    review: number;
    done: number;
  };
}

// Kanban Board Snapshot
export interface KanbanSnapshot {
  timestamp: string;
  discovery: DiscoveryBoard;
  delivery: DeliveryBoard;
}

// Metrics
export interface KanbanMetrics {
  period_days: number;
  breakdown: string;
  discovery: {
    wip: number;
    throughput?: {
      items_per_day: string;
    };
  };
  delivery: {
    wip: number;
    throughput?: {
      items_per_day: string;
    };
  };
  timestamp: string;
}

// SSE Event Types
export interface SSEEvent {
  type: 'connected' | 'heartbeat' | 'board_update' | 'review_started' | 'card_moved';
  timestamp: string;
  discovery?: DiscoveryBoard;
  delivery?: DeliveryBoard;
  item_path?: string;
  track?: string;
  from_column?: string;
  to_column?: string;
  terminal?: string;
}
