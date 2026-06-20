// Nightwatch - TypeScript equivalent of nightwatch.sh
// Main dispatcher that runs all watch modules

import { watchPriority } from './watchPriority';
import { watchDone } from './watchDone';
import { watchStuck } from './watchStuck';
import { watchIdle } from './watchIdle';
import { log, telegram } from './common';

export interface NightwatchResult {
  timestamp: string;
  priority: { checked: number; started: string[] };
  done: { found: number; triggered: string[] };
  stuck: { processed: number; nudged: Array<{ session: string; reason: string }> };
  idle: { processed: number; shutdown: number };
  durationMs: number;
}

export async function runNightwatch(): Promise<NightwatchResult> {
  const startTime = Date.now();
  const timestamp = new Date().toISOString();

  await log('Nightwatch ciklus indult');

  // 1. Priority sessions first
  const priorityResult = await watchPriority();

  // 2. DONE processing
  const doneResult = await watchDone();

  // 3. Stuck detection
  const stuckResult = await watchStuck();

  // 4. Idle shutdown last
  const idleResult = await watchIdle();

  const durationMs = Date.now() - startTime;

  const result: NightwatchResult = {
    timestamp,
    priority: priorityResult,
    done: doneResult,
    stuck: stuckResult,
    idle: idleResult,
    durationMs,
  };

  await log(`Nightwatch kész: ${durationMs}ms`);

  return result;
}

// Scheduled runner - runs every 2 minutes
let intervalId: NodeJS.Timeout | null = null;

export function startNightwatchScheduler(intervalMs = 120000): void {
  if (intervalId) {
    console.log('[Nightwatch] Scheduler already running');
    return;
  }

  console.log(`[Nightwatch] Scheduler starting (interval: ${intervalMs}ms)`);

  // Run immediately
  runNightwatch()
    .then(result => console.log('[Nightwatch] Initial run complete'))
    .catch(err => console.error('[Nightwatch] Initial run error:', err));

  // Then run on interval
  intervalId = setInterval(async () => {
    try {
      const result = await runNightwatch();

      // Log summary
      const summary = [
        `priority:${result.priority.started.length}`,
        `done:${result.done.triggered.length}`,
        `stuck:${result.stuck.nudged.length}`,
        `idle:${result.idle.shutdown}`,
      ].join(' ');

      console.log(`[Nightwatch] ${summary} (${result.durationMs}ms)`);
    } catch (err) {
      console.error('[Nightwatch] Cycle error:', err);
    }
  }, intervalMs);
}

export function stopNightwatchScheduler(): void {
  if (intervalId) {
    clearInterval(intervalId);
    intervalId = null;
    console.log('[Nightwatch] Scheduler stopped');
  }
}

// Run standalone
if (require.main === module) {
  runNightwatch().then(result => {
    console.log('\n=== Nightwatch Results ===');
    console.log(`Timestamp: ${result.timestamp}`);
    console.log(`Duration: ${result.durationMs}ms`);
    console.log('\nPriority:');
    console.log(`  Checked: ${result.priority.checked}`);
    console.log(`  Started: ${result.priority.started.join(', ') || 'none'}`);
    console.log('\nDone:');
    console.log(`  Found: ${result.done.found}`);
    console.log(`  Triggered: ${result.done.triggered.join(', ') || 'none'}`);
    console.log('\nStuck:');
    console.log(`  Processed: ${result.stuck.processed}`);
    console.log(`  Nudged: ${result.stuck.nudged.map(n => `${n.session}(${n.reason})`).join(', ') || 'none'}`);
    console.log('\nIdle:');
    console.log(`  Processed: ${result.idle.processed}`);
    console.log(`  Shutdown: ${result.idle.shutdown}`);
  });
}
