import { randomUUID } from 'node:crypto';

export interface SamplingOption {
    label: string;
    value: string;
}

export interface SamplingRequest {
    prompt: string;
    options: SamplingOption[];
    timeoutMs?: number;
}

export interface SamplingResult {
    requestId: string;
    selected: string[];
    error?: string;
    needsClarification?: boolean;
}

export interface PendingSamplingRequest {
    requestId: string;
    sessionId: string;
    prompt: string;
    options: SamplingOption[];
    createdAt: number;
    timeoutMs: number;
}

type PendingEntry = {
    request: PendingSamplingRequest;
    resolve: (result: SamplingResult) => void;
    timeoutHandle: NodeJS.Timeout;
};

export class SamplingService {
    private readonly pending = new Map<string, PendingEntry>();

    constructor(private readonly defaultTimeoutMs: number = 5000) { }

    public async requestSampling(sessionId: string, request: SamplingRequest): Promise<SamplingResult> {
        const timeoutMs = request.timeoutMs ?? this.defaultTimeoutMs;
        const requestId = randomUUID();

        const pendingRequest: PendingSamplingRequest = {
            requestId,
            sessionId,
            prompt: request.prompt,
            options: request.options,
            createdAt: Date.now(),
            timeoutMs
        };

        return new Promise<SamplingResult>((resolve) => {
            const timeoutHandle = setTimeout(() => {
                if (!this.pending.has(requestId)) {
                    return;
                }
                this.pending.delete(requestId);
                resolve({
                    requestId,
                    selected: [],
                    error: `Sampling timed out after ${timeoutMs}ms`,
                    needsClarification: true
                });
            }, timeoutMs);

            this.pending.set(requestId, {
                request: pendingRequest,
                resolve,
                timeoutHandle
            });
        });
    }

    private findPendingEntry(requestId: string): PendingEntry | undefined {
        // Prefer exact key lookup but fallback to scanning when keys mismatch due to
        // potential serialization/normalization differences.
        const byKey = this.pending.get(requestId);
        if (byKey) {
            return byKey;
        }

        return Array.from(this.pending.values()).find(entry => entry.request.requestId === requestId);
    }

    public resolveSampling(requestId: string, selected: string[]): boolean {
        const pendingEntry = this.findPendingEntry(requestId);
        if (!pendingEntry) {
            return false;
        }

        clearTimeout(pendingEntry.timeoutHandle);
        this.pending.delete(pendingEntry.request.requestId);
        pendingEntry.resolve({
            requestId: pendingEntry.request.requestId,
            selected
        });
        return true;
    }

    public listPending(sessionId?: string): PendingSamplingRequest[] {
        const all = Array.from(this.pending.values()).map(entry => entry.request);
        if (!sessionId) {
            return all;
        }
        return all.filter(entry => entry.sessionId === sessionId);
    }
}
