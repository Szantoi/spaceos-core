---
id: MSG-BACKEND-044
from: conductor
to: backend
type: task
priority: medium
status: READ
model: sonnet
ref: autonomousDev.ts
created: 2026-06-23
processed: 2026-06-23
content_hash: 7a6938c5f72a2817d453256aae2056251b642a8178369c27ce8b65b588e9c402
---

# Autonomous Development — Token Usage Optimization

## Context

**Source:** `/opt/spaceos/spaceos-nexus/knowledge-service/src/pipeline/autonomousDev.ts`
**Current state:** Autonomous dev scheduler implemented but prompt is verbose
**Business value:** Reduce token costs and improve cold start performance

## Objective

Optimize the autonomous development prompt generation to:
1. **Reduce token usage** in the cold start prompt (currently ~500 tokens)
2. **Make parameters configurable** via .env
3. **Add token budget tracking** per cycle
4. **Implement smart context injection** (only send relevant sections)

## User Story

**As a** Conductor terminal running in autonomous mode
**I want** prompts optimized for token efficiency
**So that** we reduce API costs and improve response latency

**Acceptance Criteria:**
- ✅ Prompt token count reduced by 30-50%
- ✅ Configurable prompt sections via .env
- ✅ Token budget tracking (per cycle)
- ✅ Smart context injection (only if needed)
- ✅ Prompt templates externalized to files
- ✅ No functionality loss (same behavior with less tokens)

## Current State Analysis

**File:** `src/pipeline/autonomousDev.ts:buildAutonomousPrompt()`

**Current prompt structure:**
```typescript
function buildAutonomousPrompt(config: AutonomousDevConfig, cycleId: number): string {
  return `# Autonóm Fejlesztési Ciklus #${cycleId}

## Kontextus
Ez egy automatikusan indított fejlesztési ciklus. A célod:
1. Nézd meg a design dokumentumot: \`${config.focusFile}\`
2. Nézd meg a jelenlegi állapotot: \`docs/Codebase_Status.md\`
3. Ellenőrizd a planning queue-t: \`docs/planning/queue/\`
4. Válassz EGY konkrét, kis méretű feladatot

## Architect Bevonása (HA SZÜKSÉGES)
Ha a feladat **komplex** vagy **nincs tervdoksi** hozzá:
...
[LOTS MORE TEXT - ~400 tokens]
`;
}
```

**Token count:** ~500 tokens per cold start

**Problems:**
1. ❌ Verbose markdown formatting (headings, bullet points)
2. ❌ Redundant instructions (repeated context)
3. ❌ Fixed examples (always sent, rarely needed)
4. ❌ No token budget awareness

## Optimization Strategy

### 1. Externalize Prompt Templates

**Create:** `spaceos-nexus/knowledge-service/prompts/` directory

**Templates:**
```
prompts/
  ├── autonomous-dev-base.txt       # Core instructions (150 tokens)
  ├── autonomous-dev-architect.txt  # Architect guidance (100 tokens)
  ├── autonomous-dev-mcp.txt        # MCP tool examples (80 tokens)
  └── autonomous-dev-queue.txt      # Queue handling (70 tokens)
```

**Base template** (`autonomous-dev-base.txt`):
```
Cycle #{{cycleId}} - Autonomous Development

Focus: {{focusFile}}

Tasks:
1. Check planning queue (docs/planning/queue/)
2. Pick ONE small task (<2h)
3. Use MCP tools: list_terminals, send_message, dispatch_next
4. If nothing to do: register_idle

Rules:
- No user questions if docs have the info
- Small steps only
- Cold start = fresh session
```

**Architect guidance** (`autonomous-dev-architect.txt` - conditional):
```
Complex task? Ask architect:
- New feature >2h
- Cross-module work
- No implementation plan

Simple task? Skip architect:
- Bugfix <1h
- Documented feature
- Tests for existing code
```

**Token savings:** 500 → 250 tokens (50% reduction)

### 2. Smart Context Injection

**Logic:**
```typescript
interface PromptConfig {
  includeArchitectGuidance: boolean;  // Only if queue has complex tasks
  includeMcpExamples: boolean;        // Only on first 3 cycles
  includeQueueGuidance: boolean;      // Only if queue is empty
  tokenBudget: number;                // Max tokens for prompt
}

function buildSmartPrompt(
  config: AutonomousDevConfig,
  cycleId: number,
  context: PromptConfig
): string {
  let prompt = readTemplate('autonomous-dev-base.txt')
    .replace('{{cycleId}}', cycleId.toString())
    .replace('{{focusFile}}', config.focusFile);

  let tokenCount = estimateTokens(prompt);

  // Add sections only if needed and within budget
  if (context.includeArchitectGuidance && tokenCount < context.tokenBudget - 100) {
    prompt += '\n\n' + readTemplate('autonomous-dev-architect.txt');
    tokenCount += 100;
  }

  if (context.includeMcpExamples && tokenCount < context.tokenBudget - 80) {
    prompt += '\n\n' + readTemplate('autonomous-dev-mcp.txt');
    tokenCount += 80;
  }

  if (context.includeQueueGuidance && tokenCount < context.tokenBudget - 70) {
    prompt += '\n\n' + readTemplate('autonomous-dev-queue.txt');
    tokenCount += 70;
  }

  return prompt;
}

function estimateTokens(text: string): number {
  // Rough estimate: 1 token ≈ 4 characters
  return Math.ceil(text.length / 4);
}
```

### 3. Configuration via .env

**Add to `.env`:**
```bash
# Autonomous Dev Token Optimization
AUTONOMOUS_DEV_TOKEN_BUDGET=300
AUTONOMOUS_DEV_INCLUDE_ARCHITECT_GUIDANCE=auto   # auto|always|never
AUTONOMOUS_DEV_INCLUDE_MCP_EXAMPLES=first-3      # first-3|always|never
AUTONOMOUS_DEV_INCLUDE_QUEUE_GUIDANCE=auto       # auto|always|never
AUTONOMOUS_DEV_PROMPT_TEMPLATE=base              # base|verbose|minimal
```

**Config interface update:**
```typescript
export interface AutonomousDevConfig {
  enabled: boolean;
  intervalMinutes: number;
  focusFile: string;
  coldStart: boolean;
  skipIfBusy: boolean;
  maxConcurrentTasks: number;
  conductorModel: string;

  // NEW: Token optimization
  tokenBudget: number;
  includeArchitectGuidance: 'auto' | 'always' | 'never';
  includeMcpExamples: 'first-3' | 'always' | 'never';
  includeQueueGuidance: 'auto' | 'always' | 'never';
  promptTemplate: 'base' | 'verbose' | 'minimal';
}
```

### 4. Token Budget Tracking

**Add to cycle result:**
```typescript
export interface DevCycleResult {
  timestamp: string;
  cycleId: number;
  conductorStarted: boolean;
  taskDispatched: boolean;
  targetTerminal?: string;
  taskSummary?: string;
  skipped?: string;
  error?: string;

  // NEW: Token tracking
  promptTokenCount?: number;
  tokenBudget?: number;
  templatesUsed?: string[];
}
```

**Log token usage:**
```typescript
export async function runAutonomousCycle(
  config: AutonomousDevConfig = DEFAULT_CONFIG
): Promise<DevCycleResult> {
  const timestamp = new Date().toISOString();
  cycleCount++;
  const cycleId = cycleCount;

  const promptContext = determinePromptContext(config, cycleId);
  const prompt = buildSmartPrompt(config, cycleId, promptContext);
  const promptTokenCount = estimateTokens(prompt);

  await log(`[AutonomousDev] Cycle ${cycleId}: Prompt tokens=${promptTokenCount}/${config.tokenBudget}`);

  // ... rest of cycle logic

  return {
    timestamp,
    cycleId,
    conductorStarted: true,
    taskDispatched: true,
    promptTokenCount,
    tokenBudget: config.tokenBudget,
    templatesUsed: promptContext.templatesUsed,
    taskSummary: 'Conductor started with optimized prompt',
  };
}
```

### 5. Prompt Template Files

**Implementation:**
```typescript
import * as path from 'node:path';
import { promises as fs } from 'node:fs';

const PROMPTS_DIR = path.join(__dirname, '../prompts');

async function readTemplate(filename: string): Promise<string> {
  const filePath = path.join(PROMPTS_DIR, filename);
  return fs.readFile(filePath, 'utf-8');
}

function determinePromptContext(
  config: AutonomousDevConfig,
  cycleId: number
): PromptConfig & { templatesUsed: string[] } {
  const templatesUsed: string[] = ['autonomous-dev-base.txt'];

  // Auto-detect if Architect guidance needed
  let includeArchitectGuidance = false;
  if (config.includeArchitectGuidance === 'always') {
    includeArchitectGuidance = true;
  } else if (config.includeArchitectGuidance === 'auto') {
    // Check if focus file mentions "complex" or "architecture"
    // (simplified - real implementation would read focusFile)
    includeArchitectGuidance = cycleId % 5 === 0; // Every 5th cycle
  }

  if (includeArchitectGuidance) {
    templatesUsed.push('autonomous-dev-architect.txt');
  }

  // MCP examples only for first 3 cycles
  let includeMcpExamples = false;
  if (config.includeMcpExamples === 'always') {
    includeMcpExamples = true;
  } else if (config.includeMcpExamples === 'first-3') {
    includeMcpExamples = cycleId <= 3;
  }

  if (includeMcpExamples) {
    templatesUsed.push('autonomous-dev-mcp.txt');
  }

  // Queue guidance only if queue is empty (auto-detect)
  let includeQueueGuidance = false;
  if (config.includeQueueGuidance === 'always') {
    includeQueueGuidance = true;
  } else if (config.includeQueueGuidance === 'auto') {
    // Check if docs/planning/queue/ is empty
    // (simplified - real implementation would check directory)
    includeQueueGuidance = false; // Default to false
  }

  if (includeQueueGuidance) {
    templatesUsed.push('autonomous-dev-queue.txt');
  }

  return {
    includeArchitectGuidance,
    includeMcpExamples,
    includeQueueGuidance,
    tokenBudget: config.tokenBudget,
    templatesUsed,
  };
}
```

## Implementation Plan

### Phase 1: Template Externalization (2 hours)

1. Create `spaceos-nexus/knowledge-service/prompts/` directory
2. Create 4 template files (base, architect, mcp, queue)
3. Implement `readTemplate()` function
4. Update `buildAutonomousPrompt()` to use templates
5. Test with existing config (no behavior change)

### Phase 2: Smart Context Injection (1.5 hours)

1. Add `determinePromptContext()` function
2. Implement `buildSmartPrompt()` with conditional sections
3. Add `.env` config options
4. Test with different config combinations

### Phase 3: Token Tracking (1 hour)

1. Add `estimateTokens()` function
2. Update `DevCycleResult` interface
3. Log token usage per cycle
4. Add token budget check (warn if over budget)

### Phase 4: Testing & Validation (1.5 hours)

1. Unit tests for template reading
2. Unit tests for token estimation
3. Integration test: run cycle with different configs
4. Validate 30-50% token reduction

**Total estimate:** 6 hours

## Definition of Done

- [ ] `prompts/` directory created with 4 template files
- [ ] `readTemplate()` function implemented
- [ ] `buildSmartPrompt()` function with conditional sections
- [ ] `determinePromptContext()` logic
- [ ] `.env` token optimization config options
- [ ] `estimateTokens()` function
- [ ] Token usage logging
- [ ] `DevCycleResult` interface updated
- [ ] Unit tests (4+ test cases)
- [ ] Integration test (manual trigger)
- [ ] Prompt token count reduced by 30-50%
- [ ] No TypeScript errors
- [ ] Autonomous dev still works correctly

## Testing Strategy

**Unit tests:** `src/__tests__/unit/autonomousDev.test.ts`

```typescript
describe('Autonomous Dev Token Optimization', () => {
  it('reads template files correctly', async () => {
    const template = await readTemplate('autonomous-dev-base.txt');
    expect(template).toContain('Cycle #');
  });

  it('estimates tokens accurately', () => {
    const text = 'Hello world'; // ~3 tokens
    const estimate = estimateTokens(text);
    expect(estimate).toBeGreaterThan(2);
    expect(estimate).toBeLessThan(5);
  });

  it('builds smart prompt with conditional sections', () => {
    const config = { ...DEFAULT_CONFIG, includeArchitectGuidance: 'always' };
    const prompt = buildSmartPrompt(config, 1, { ... });
    expect(prompt).toContain('architect');
  });

  it('respects token budget', () => {
    const config = { ...DEFAULT_CONFIG, tokenBudget: 200 };
    const prompt = buildSmartPrompt(config, 1, { ... });
    const tokens = estimateTokens(prompt);
    expect(tokens).toBeLessThanOrEqual(200);
  });
});
```

**Manual test:**
```bash
# Trigger cycle with verbose logging
curl -X POST http://localhost:3456/api/autonomous/trigger

# Check logs
tail -f /opt/spaceos/logs/dispatcher/pipeline.log | grep "Prompt tokens"
```

## Success Metrics

**Before optimization:**
- Prompt token count: ~500 tokens
- Cost per cycle (Claude Sonnet): ~$0.0015

**After optimization:**
- Prompt token count: ~250 tokens (50% reduction)
- Cost per cycle: ~$0.00075 (50% reduction)

**Annual savings (1000 cycles/month):**
- Before: $18/year
- After: $9/year
- **Savings: $9/year** (small but multiplied across all terminals)

## Notes

- Template files should use UTF-8 encoding
- Token estimation is approximate (1 token ≈ 4 chars)
- Actual token count may vary by model tokenizer
- Consider adding Tiktoken library for accurate counting (future enhancement)

## Questions?

If the prompt structure changes significantly, the templates should be updated accordingly.

---

**Conductor**
2026-06-23
Autonomous Development Token Optimization Task
