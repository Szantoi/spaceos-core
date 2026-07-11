import { RbacFilter } from '../mcp/RbacFilter';
import { StateChangeEvent, PostHook } from './EventDispatcher';
import { SessionState } from './types';

// Built-in post-hook: RBAC cache invalidation
export class RbacCacheInvalidateHook implements PostHook {
    constructor(private rbac: RbacFilter) { }

    run(event: StateChangeEvent, _result: SessionState): void {
        // Állapotváltáskor érvénytelenítjük a cache-t,
        // mert a jogosultságok függhetnek az FSM állapottól.
        this.rbac.invalidateCache(event.sessionId);
    }
}
