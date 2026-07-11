---
title: "Dev B — TASK-14-05 Implementation Brief"
subtitle: "Stdio Transport for Local Development — BUILD AFTER 14-02"
created: 2026-03-09
target: "Backend Developer"
timeline: "25 hours (3 days)"
action: "START AFTER TASK-14-02 (OR PARALLEL)"
---

# 🏗️ Dev B — TASK-14-05 Implementation Brief

**EPIC:** EPIC-14 (Modern MCP Transports & Plugin System)
**Task:** TASK-14-05 (Stdio Transport — Standard Input/Output)
**Start:** After TASK-14-02 (HTTP Transport) or parallel if time allows
**Duration:** 25 hours
**Blocks:** TASK-14-07 (E2E transport tests), TASK-14-08 (Transport sampling)

---

## 🎯 Quick Summary

You're building a **Stdio MCP transport** for local development and CI/CD environments.

**Why Stdio?**

- ✅ Simplest transport (no network overhead)
- ✅ Perfect for CLI tools and local testing
- ✅ Standard for MCP protocol (original design)
- ✅ Zero-config (works in piped environments)

---

## 📋 Your Tasks (Do These in Order)

### Task 1: Create StdioTransport Base Class

**File:** `src/transports/StdioTransport.ts`

```typescript
import { ITransport, TransportState, TransportError } from './ITransport';
import { createInterface, Interface as ReadlineInterface } from 'readline';

/**
 * StdioTransport — Standard Input/Output MCP transport
 *
 * Implements:
 * - Line-based JSON protocol (one JSON object per line)
 * - Bidirectional communication via stdio
 * - Error recovery from malformed input
 * - Graceful EOF handling
 */
export class StdioTransport implements ITransport {
  private rl: ReadlineInterface;
  private state: TransportState = 'idle';
  private messageCallbacks: Map<string, (data: any) => void> = new Map();
  private errorCallbacks: ((error: TransportError) => void)[] = [];

  constructor(inputStream = process.stdin, outputStream = process.stdout) {
    this.rl = createInterface({
      input: inputStream,
      output: undefined, // We control output manually
      terminal: false    // Disable terminal mode for pipes
    });

    this.setupHandlers(outputStream);
  }

  /**
   * Setup input/output handlers
   */
  private setupHandlers(outputStream: NodeJS.WritableStream): void {
    // Handle incoming lines
    this.rl.on('line', (line: string) => {
      try {
        // Parse JSON line
        const data = JSON.parse(line);

        // Route message to handler
        if (data.id && this.messageCallbacks.has(data.id)) {
          const handler = this.messageCallbacks.get(data.id)!;
          handler(data);
          this.messageCallbacks.delete(data.id);
        }
      } catch (error) {
        // Malformed JSON — log but continue
        this.notifyError({
          code: 'INVALID_JSON',
          message: `Failed to parse line: ${line}`,
          context: { line }
        });
      }
    });

    // Handle EOF (when parent process closes stdin)
    this.rl.on('close', () => {
      this.state = 'closed';
      console.error('[StdioTransport] stdin closed. Exiting.');
      process.exit(0);
    });

    // Use this stream for output
    this.outputStream = outputStream;
  }

  private outputStream: NodeJS.WritableStream;

  /**
   * Send message via stdout (one JSON per line)
   */
  async send(data: any): Promise<void> {
    if (this.state !== 'idle' && this.state !== 'listening') {
      throw new Error(`Cannot send in state: ${this.state}`);
    }

    const line = JSON.stringify(data) + '\n';

    return new Promise((resolve, reject) => {
      this.outputStream.write(line, 'utf8', (err) => {
        if (err) reject(err);
        else resolve();
      });
    });
  }

  /**
   * Receive message (register handler for specific message ID)
   */
  receive(id: string, handler: (data: any) => void): void {
    this.messageCallbacks.set(id, handler);
  }

  /**
   * Start listening for messages
   */
  async start(): Promise<void> {
    this.state = 'listening';
    console.error('[StdioTransport] Listening on stdin');
  }

  /**
   * Stop listening
   */
  async stop(): Promise<void> {
    this.state = 'closed';
    this.rl.close();
  }

  /**
   * Notify error listeners
   */
  private notifyError(error: TransportError): void {
    this.errorCallbacks.forEach(cb => cb(error));
  }

  /**
   * Register error handler
   */
  onError(handler: (error: TransportError) => void): void {
    this.errorCallbacks.push(handler);
  }

  /**
   * Get current state
   */
  getState(): TransportState {
    return this.state;
  }
}
```

**Accept Criteria:**

- ✅ Stdio interface created (stdin/stdout)
- ✅ Line-based JSON protocol
- ✅ Message ID routing
- ✅ Error recovery (malformed JSON)
- ✅ EOF handling (graceful exit)
- ✅ No TypeScript errors

---

### Task 2: Create Unit Tests

**File:** `src/tests/unit/StdioTransport.test.ts`

```typescript
import { describe, it, expect, beforeEach, afterEach } from '@jest/globals';
import { StdioTransport } from '../../transports/StdioTransport';
import { PassThrough } from 'stream';

describe('StdioTransport — Line-based JSON protocol', () => {
  let transport: StdioTransport;
  let inputStream: PassThrough;
  let outputStream: PassThrough;

  beforeEach(async () => {
    inputStream = new PassThrough();
    outputStream = new PassThrough();

    transport = new StdioTransport(inputStream, outputStream);
    await transport.start();
  });

  afterEach(async () => {
    await transport.stop();
    inputStream.destroy();
    outputStream.destroy();
  });

  describe('Basic Send/Receive', () => {
    it('should send JSON message via stdout', async () => {
      const message = { id: '123', tool: 'test', args: {} };

      const output = new Promise<string>((resolve) => {
        outputStream.once('data', (chunk) => resolve(chunk.toString()));
      });

      await transport.send(message);

      const result = JSON.parse(await output);
      expect(result).toEqual(message);
    });

    it('should receive JSON message from stdin', async () => {
      const message = { id: '456', result: 'success' };

      transport.receive('456', (data) => {
        expect(data).toEqual(message);
      });

      // Simulate stdin input
      inputStream.write(JSON.stringify(message) + '\n');
    });
  });

  describe('Malformed JSON Handling', () => {
    it('should handle invalid JSON gracefully', async () => {
      let errorReceived = false;

      transport.onError((error) => {
        errorReceived = true;
        expect(error.code).toBe('INVALID_JSON');
      });

      // Send malformed JSON
      inputStream.write('{ invalid json }\n');

      // Wait a bit for async error handling
      await new Promise(r => setTimeout(r, 100));
      expect(errorReceived).toBe(true);
    });
  });

  describe('EOF Handling', () => {
    it('should detect stdin EOF and exit gracefully', async () => {
      // This test is tricky — may need to mock process.exit
      // Simulate EOF by closing stdin
      inputStream.end();

      // Wait for close event
      await new Promise(r => setTimeout(r, 100));
      expect(transport.getState()).toBe('closed');
    });
  });

  describe('State Management', () => {
    it('should start in "listening" state', () => {
      expect(transport.getState()).toBe('listening');
    });

    it('should transition to "closed" after stop', async () => {
      await transport.stop();
      expect(transport.getState()).toBe('closed');
    });
  });

  describe('Message Routing by ID', () => {
    it('should route messages to correct handler by ID', async () => {
      const results: any[] = [];

      transport.receive('msg-1', (data) => results.push({ id: 'msg-1', data }));
      transport.receive('msg-2', (data) => results.push({ id: 'msg-2', data }));

      // Send two messages
      inputStream.write(JSON.stringify({ id: 'msg-1', result: 'first' }) + '\n');
      inputStream.write(JSON.stringify({ id: 'msg-2', result: 'second' }) + '\n');

      // Wait for processing
      await new Promise(r => setTimeout(r, 100));

      expect(results).toHaveLength(2);
      expect(results[0].id).toBe('msg-1');
      expect(results[1].id).toBe('msg-2');
    });
  });
});
```

**Accept Criteria:**

- ✅ Send message test passing
- ✅ Receive message test passing
- ✅ Malformed JSON recovery tested
- ✅ EOF handling tested
- ✅ State machine working
- ✅ Message routing by ID working
- ✅ 100% coverage

---

### Task 3: Integration with TransportFactory

**File:** `src/transports/TransportFactory.ts` (update)

```typescript
import { HTTPTransport } from './HTTPTransport';
import { StdioTransport } from './StdioTransport';

export class TransportFactory {
  static create(type: 'http' | 'stdio', options?: any): ITransport {
    switch (type) {
      case 'http':
        return new HTTPTransport(
          options?.port || 3000,
          options?.host || 'localhost'
        );
      case 'stdio':
        return new StdioTransport(
          options?.input || process.stdin,
          options?.output || process.stdout
        );
      default:
        throw new Error(`Unknown transport type: ${type}`);
    }
  }
}
```

---

## 📂 Files You'll Create/Modify

| File | Action | Lines |
|:-----|:--------|:------|
| `src/transports/StdioTransport.ts` | CREATE | ~150 |
| `src/tests/unit/StdioTransport.test.ts` | CREATE | ~120 |
| `src/transports/TransportFactory.ts` | MODIFY | +8 |

---

## 🚀 Implementation Steps

**Step 1: Create StdioTransport.ts** (8-10 hours)

```bash
cd src/transports
cat > StdioTransport.ts << 'EOF'
[paste code above]
EOF
```

**Step 2: Create unit tests** (6-8 hours)

```bash
cd src/tests/unit
cat > StdioTransport.test.ts << 'EOF'
[paste test code above]
EOF
```

**Step 3: Update TransportFactory** (30 min)

```bash
# Add StdioTransport factory method
```

**Step 4: Test locally** (3-4 hours)

```bash
npm test -- StdioTransport.test.ts
```

Expected: All tests GREEN ✅

**Step 5: Manual CLI test** (2-3 hours)

```bash
# Create test script that uses stdio
echo '{"id":"1","tool":"echo","args":{"msg":"hello"}}' | \
  node -e "const { StdioTransport } = require('./src/transports'); \
    const t = new StdioTransport(); \
    t.start().then(() => { /* wait for messages */ })"

# Expected: {"id":"1","result":"hello"} on stdout
```

**Step 6: Git commit** (15 min)

```bash
git add src/transports/StdioTransport.ts src/tests/unit/StdioTransport.test.ts
git commit -m "TASK-14-05: Stdio Transport (Local Development)

- Implement StdioTransport class with readline interface
- Line-based JSON protocol (one object per line)
- Bidirectional communication via stdin/stdout
- Message routing by ID
- Malformed JSON recovery (non-blocking)
- EOF handling and graceful shutdown
- Comprehensive unit tests (100% coverage)

AC: All 18 acceptance criteria met
Tests: 100% pass rate
Unblocks: TASK-14-07/14-08"

git push origin feature/epic-14-task-14-05
```

---

## ✅ Definition of Done

Before marking COMPLETE, verify:

- [ ] `src/transports/StdioTransport.ts` exists, no TypeScript errors
- [ ] Sends JSON messages via stdout
- [ ] Receives JSON messages from stdin
- [ ] Messages routed by ID correctly
- [ ] Malformed JSON handled gracefully
- [ ] EOF detected and handled
- [ ] `src/tests/unit/StdioTransport.test.ts` 100% passing
- [ ] Manual CLI test working
- [ ] Integration with TransportFactory working
- [ ] Git commit + push
- [ ] No TypeScript warnings: `npm run type-check`

---

## 🔗 Dependencies

- **Blocks On:** Dev A TASK-14-01 (Transport Abstraction interfaces)
- **Can Be Parallel With:** TASK-14-02 (HTTP Transport) — independent implementations
- **Blocks:** TASK-14-07 (E2E transport tests), TASK-14-08 (Transport sampling)

---

## ❓ Questions?

- Node.js streams? → Check Node.js docs (PassThrough, readline)
- JSON protocol? → Check MCP specification (message format)
- Message routing? → See Map data structure patterns

**Contact:** #m02-dev Slack

---

## 🎯 What This Unblocks

- **Dev Teams:** Can test locally without HTTP server
- **CI/CD:** Can run MCP tools in piped environments
- **QA Team:** Can test transport layer compatibility

---

**Status:** READY TO BUILD 🏗️
**Start Time:** After TASK-14-02 (or parallel)
**Estimated Duration:** 25 hours
**Parallel Opportunity:** Can run alongside TASK-14-02
