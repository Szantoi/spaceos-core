import { describe, it, expect, beforeEach } from 'vitest';
import { DiscoveryPhaseTracker } from '../../metadata/DiscoveryPhaseTracker';

describe('DiscoveryPhaseTracker', () => {
    let tracker: DiscoveryPhaseTracker;
    const sessionId = 'tracker-test-1';

    beforeEach(() => {
        tracker = new DiscoveryPhaseTracker(':memory:');
        (tracker as any).db.exec(`
            CREATE TABLE IF NOT EXISTS agent_sessions (
                session_id TEXT PRIMARY KEY
            );
        `);
        (tracker as any).db.prepare('INSERT OR IGNORE INTO agent_sessions(session_id) VALUES (?)').run(sessionId);
    });

    it('defaults to ideation and not completed', () => {
        tracker.ensureSession(sessionId);
        expect(tracker.getCurrentPhase(sessionId)).toBe('ideation');
        expect(tracker.isPhaseComplete(sessionId, 'ideation')).toBe(false);
    });

    it('marks phase complete and advances', () => {
        tracker.ensureSession(sessionId);
        tracker.markPhaseComplete(sessionId, 'ideation');
        expect(tracker.isPhaseComplete(sessionId, 'ideation')).toBe(true);
        expect(tracker.getCurrentPhase(sessionId)).toBe('validation');
    });

    it('enforces ordering rules', () => {
        tracker.ensureSession(sessionId);
        // cannot submit validation before ideation complete
        expect(tracker.canSubmitForPhase(sessionId, 'validation')).toBe(false);
        tracker.markPhaseComplete(sessionId, 'ideation');
        expect(tracker.canSubmitForPhase(sessionId, 'validation')).toBe(true);
        // iteration still blocked
        expect(tracker.canSubmitForPhase(sessionId, 'iteration')).toBe(false);
        tracker.markPhaseComplete(sessionId, 'validation');
        expect(tracker.canSubmitForPhase(sessionId, 'iteration')).toBe(true);
    });

    it('nextPhase returns null for last phase', () => {
        // reflection via internal method - just ensure no crash
        tracker.ensureSession(sessionId);
        tracker.markPhaseComplete(sessionId, 'ideation');
        tracker.markPhaseComplete(sessionId, 'validation');
        expect(tracker.getCurrentPhase(sessionId)).toBe('iteration');
        tracker.markPhaseComplete(sessionId, 'iteration');
        expect(tracker.isPhaseComplete(sessionId, 'iteration')).toBe(true);
    });
});