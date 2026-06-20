/**
 * Terminal Status Tracker
 *
 * Tracks whether terminals are idle or working to avoid
 * disturbing active sessions with wake-up notifications.
 */

export type TerminalState = 'idle' | 'working';

interface TerminalInfo {
  state: TerminalState;
  lastActivity: Date;
  currentTask?: string;
}

// In-memory terminal status (could be persisted to file if needed)
const terminalStatus: Map<string, TerminalInfo> = new Map();

// Idle timeout: if no heartbeat for 10 minutes, consider idle
const IDLE_TIMEOUT_MS = 10 * 60 * 1000;

/**
 * Register terminal as working (starting a task)
 */
export function registerWorking(terminal: string, taskId?: string): void {
  terminalStatus.set(terminal, {
    state: 'working',
    lastActivity: new Date(),
    currentTask: taskId,
  });
  console.log(`[TerminalStatus] ${terminal} -> WORKING${taskId ? ` (${taskId})` : ''}`);
}

/**
 * Register terminal as idle (finished task or session ended)
 */
export function registerIdle(terminal: string): void {
  const existing = terminalStatus.get(terminal);
  terminalStatus.set(terminal, {
    state: 'idle',
    lastActivity: new Date(),
    currentTask: undefined,
  });
  if (existing?.state === 'working') {
    console.log(`[TerminalStatus] ${terminal} -> IDLE`);
  }
}

/**
 * Update heartbeat (terminal is still active)
 */
export function heartbeat(terminal: string): void {
  const existing = terminalStatus.get(terminal);
  if (existing) {
    existing.lastActivity = new Date();
  } else {
    // First heartbeat, assume working
    registerWorking(terminal);
  }
}

/**
 * Check if terminal is currently working
 */
export function isWorking(terminal: string): boolean {
  const info = terminalStatus.get(terminal);
  if (!info) return false;

  // Check if timed out (no activity for IDLE_TIMEOUT_MS)
  const now = new Date();
  const elapsed = now.getTime() - info.lastActivity.getTime();
  if (elapsed > IDLE_TIMEOUT_MS) {
    // Auto-transition to idle
    info.state = 'idle';
    info.currentTask = undefined;
    console.log(`[TerminalStatus] ${terminal} -> IDLE (timeout)`);
    return false;
  }

  return info.state === 'working';
}

/**
 * Check if terminal should receive wake-up notification
 * Returns true if terminal is idle and should be woken up
 */
export function shouldWakeUp(terminal: string): boolean {
  return !isWorking(terminal);
}

/**
 * Get status of all terminals
 */
export function getAllStatus(): Record<string, TerminalInfo & { terminal: string }> {
  const result: Record<string, TerminalInfo & { terminal: string }> = {};
  for (const [terminal, info] of terminalStatus.entries()) {
    // Update state based on timeout
    isWorking(terminal); // This will auto-transition if needed
    result[terminal] = { ...info, terminal };
  }
  return result;
}

/**
 * Get single terminal status
 */
export function getStatus(terminal: string): TerminalInfo | null {
  const info = terminalStatus.get(terminal);
  if (!info) return null;
  // Update state based on timeout
  isWorking(terminal);
  return info;
}
