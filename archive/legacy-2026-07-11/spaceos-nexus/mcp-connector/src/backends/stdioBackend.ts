/**
 * STDIO Backend - Spawns and manages MCP servers via stdio
 */

import { spawn, ChildProcess } from 'child_process';
import { Backend, BackendConfig, MCPRequest, MCPResponse } from '../types';
import readline from 'readline';

interface PendingRequest {
  resolve: (response: MCPResponse) => void;
  reject: (error: Error) => void;
  timeout: NodeJS.Timeout;
}

export class StdioBackend implements Backend {
  private process?: ChildProcess;
  private name: string;
  private command: string;
  private args: string[];
  private env?: Record<string, string>;
  private autoRestart: boolean;
  private timeout: number;

  private requestId = 0;
  private pendingRequests = new Map<number | string, PendingRequest>();
  private lineBuffer = '';
  private started = false;
  private stopping = false;

  constructor(name: string, config: BackendConfig) {
    this.name = name;
    this.command = config.command!;
    this.args = config.args || [];
    this.env = config.env;
    this.autoRestart = config.auto_restart ?? true;
    this.timeout = config.timeout || 30000;
  }

  getName(): string {
    return this.name;
  }

  async start(): Promise<void> {
    if (this.started) {
      console.log(`[${this.name}] Already started`);
      return;
    }

    return new Promise((resolve, reject) => {
      console.log(`[${this.name}] Starting: ${this.command} ${this.args.join(' ')}`);

      this.process = spawn(this.command, this.args, {
        env: { ...process.env, ...this.env },
        stdio: ['pipe', 'pipe', 'pipe'],
      });

      // Read stdout line by line
      const rl = readline.createInterface({
        input: this.process.stdout!,
        crlfDelay: Infinity,
      });

      rl.on('line', (line) => {
        this.handleLine(line);
      });

      // Handle stderr (log warnings)
      this.process.stderr?.on('data', (data) => {
        const msg = data.toString().trim();
        if (msg) {
          console.warn(`[${this.name}] stderr:`, msg);
        }
      });

      // Handle process exit
      this.process.on('exit', (code) => {
        console.warn(`[${this.name}] Process exited with code ${code}`);
        this.started = false;

        // Reject all pending requests
        for (const [id, pending] of this.pendingRequests) {
          clearTimeout(pending.timeout);
          pending.reject(new Error(`Backend ${this.name} exited unexpectedly`));
          this.pendingRequests.delete(id);
        }

        // Auto-restart if enabled and not intentionally stopping
        if (this.autoRestart && !this.stopping) {
          console.log(`[${this.name}] Auto-restarting in 5s...`);
          setTimeout(() => this.start(), 5000);
        }
      });

      // Handle spawn error
      this.process.on('error', (error) => {
        console.error(`[${this.name}] Spawn error:`, error.message);
        reject(error);
      });

      // Give process time to initialize
      setTimeout(() => {
        if (this.process && !this.process.killed) {
          this.started = true;
          console.log(`[${this.name}] Started successfully`);
          resolve();
        } else {
          reject(new Error(`Failed to start backend ${this.name}`));
        }
      }, 2000);
    });
  }

  async stop(): Promise<void> {
    this.stopping = true;
    if (this.process && !this.process.killed) {
      this.process.kill('SIGTERM');
      this.started = false;
      console.log(`[${this.name}] Stopped`);
    }
  }

  private handleLine(line: string): void {
    if (!line.trim()) return;

    try {
      const response = JSON.parse(line) as MCPResponse;

      if (response.id !== undefined) {
        const pending = this.pendingRequests.get(response.id);
        if (pending) {
          clearTimeout(pending.timeout);
          pending.resolve(response);
          this.pendingRequests.delete(response.id);
        }
      }
    } catch (error) {
      // Not valid JSON, might be initialization message
      console.log(`[${this.name}] Non-JSON output:`, line.substring(0, 100));
    }
  }

  async call(request: MCPRequest): Promise<MCPResponse> {
    if (!this.process || !this.started) {
      throw new Error(`Backend ${this.name} not started`);
    }

    const requestId = ++this.requestId;
    const mcpRequest: MCPRequest = {
      ...request,
      id: requestId,
    };

    return new Promise((resolve, reject) => {
      const timeout = setTimeout(() => {
        this.pendingRequests.delete(requestId);
        reject(new Error(`Timeout waiting for ${this.name} response (${this.timeout}ms)`));
      }, this.timeout);

      this.pendingRequests.set(requestId, { resolve, reject, timeout });

      try {
        this.process!.stdin!.write(JSON.stringify(mcpRequest) + '\n');
      } catch (error) {
        clearTimeout(timeout);
        this.pendingRequests.delete(requestId);
        reject(error);
      }
    });
  }

  async healthCheck(): Promise<boolean> {
    return this.process !== undefined && !this.process.killed && this.started;
  }

  async getTools(): Promise<string[]> {
    // Could send tools/list request to get available tools
    return [];
  }
}
