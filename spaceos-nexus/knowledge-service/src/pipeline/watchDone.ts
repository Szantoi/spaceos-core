// Watch Done - TypeScript equivalent of watch-done.sh
// Scans for DONE outbox messages and triggers reviewer
// 2026-06-24: Optimized to use messageRegistry DB instead of filesystem scan
// 2026-06-24: Switched to terminal-based review (Architect + Librarian) instead of API calls

import * as path from 'path';
import {
  getState,
  setState,
  log,
} from './common';
import { handleDoneReview } from './reviewer';
import { handleTerminalReview } from './terminalReviewer';
import { runPipeline } from './pipeline';

// Use terminal-based review by default (no API key needed)
// Set REVIEW_MODE=api to use old Anthropic API based review
const USE_TERMINAL_REVIEW = process.env.REVIEW_MODE !== 'api';
import { queryMessages } from '../messageRegistry';

/**
 * Find UNREAD DONE messages using DB query instead of filesystem scan.
 * This reduces ~50 fs.readFile operations per cycle to 1 DB query.
 */
function findUnreadDones(): string[] {
  // Query DB for UNREAD messages of type 'done' in outbox
  const messages = queryMessages({
    box: 'outbox',
    type: 'done',
    status: 'UNREAD',
  });

  // Return file paths for compatibility with existing review pipeline
  return messages
    .filter(m => m.filePath) // Ensure filePath exists
    .map(m => m.filePath);
}

export async function watchDone(): Promise<{ found: number; triggered: string[] }> {
  const now = Math.floor(Date.now() / 1000);
  const triggered: string[] = [];

  const unreadDones = findUnreadDones(); // Now synchronous DB query

  if (unreadDones.length === 0) {
    return { found: 0, triggered: [] };
  }

  for (const donePath of unreadDones) {
    const basename = path.basename(donePath, '.md');
    const reviewKey = `review_${basename}`;
    const lastReview = await getState(reviewKey);

    // Review only triggers once per DONE file
    if (!lastReview || lastReview === '0') {
      await log(`[watchDone] Review triggerelve: ${basename}`);
      await setState(reviewKey, String(now));

      // Run reviewer - terminal-based (Architect + Librarian) or API-based
      try {
        const reviewResult = USE_TERMINAL_REVIEW
          ? await handleTerminalReview(donePath)
          : await handleDoneReview(donePath);

        await log(`[watchDone] Review mode: ${USE_TERMINAL_REVIEW ? 'terminal' : 'api'}`);

        if (reviewResult.approved) {
          await log(`[watchDone] APPROVED: ${basename} → running pipeline`);

          // Run post-review pipeline
          const pipelineResult = await runPipeline(donePath);
          await log(`[watchDone] Pipeline complete: archived=${pipelineResult.archived}, notified=${pipelineResult.notified}`);
        } else {
          await log(`[watchDone] REJECTED: ${basename} → reject inbox created at ${reviewResult.resultPath}`);
        }

        triggered.push(basename);
      } catch (error) {
        const errorMsg = error instanceof Error ? error.message : 'Unknown error';
        await log(`[watchDone] Reviewer hiba: ${errorMsg}`);
        // Clear state so it can retry on next run
        await setState(reviewKey, '0');
      }
    }
  }

  return { found: unreadDones.length, triggered };
}

// Run standalone
if (require.main === module) {
  watchDone().then(result => {
    console.log(`[watchDone] Found: ${result.found}, Triggered: ${result.triggered.length}`);
    result.triggered.forEach(t => console.log(`  - ${t}`));
  });
}
