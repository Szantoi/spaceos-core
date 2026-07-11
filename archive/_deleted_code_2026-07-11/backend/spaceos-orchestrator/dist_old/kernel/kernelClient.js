"use strict";
// src/kernel/kernelClient.ts
// Typed HTTP client for Kernel query endpoints — uses native fetch with full error map.
// ONLY this file calls Kernel query API via fetch (existing kernel.action.ts uses axios for mutations).
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || (function () {
    var ownKeys = function(o) {
        ownKeys = Object.getOwnPropertyNames || function (o) {
            var ar = [];
            for (var k in o) if (Object.prototype.hasOwnProperty.call(o, k)) ar[ar.length] = k;
            return ar;
        };
        return ownKeys(o);
    };
    return function (mod) {
        if (mod && mod.__esModule) return mod;
        var result = {};
        if (mod != null) for (var k = ownKeys(mod), i = 0; i < k.length; i++) if (k[i] !== "default") __createBinding(result, mod, k[i]);
        __setModuleDefault(result, mod);
        return result;
    };
})();
Object.defineProperty(exports, "__esModule", { value: true });
exports.kernelClient = exports.KernelClient = exports.KernelClientError = exports.KernelErrorCode = void 0;
exports.setKernelJwt = setKernelJwt;
const env_1 = require("../config/env");
// ─── Error codes ─────────────────────────────────────────────────────────────
var KernelErrorCode;
(function (KernelErrorCode) {
    KernelErrorCode["AuthExpired"] = "ERR_TOOL_AUTH_EXPIRED";
    KernelErrorCode["RateLimited"] = "ERR_TOOL_RATE_LIMITED";
    KernelErrorCode["Unavailable"] = "ERR_KERNEL_UNAVAILABLE";
    KernelErrorCode["Timeout"] = "ERR_KERNEL_TIMEOUT";
    KernelErrorCode["BadRequest"] = "ERR_TOOL_BAD_REQUEST";
    KernelErrorCode["Unknown"] = "ERR_KERNEL_UNKNOWN";
})(KernelErrorCode || (exports.KernelErrorCode = KernelErrorCode = {}));
class KernelClientError extends Error {
    code;
    httpStatus;
    constructor(code, httpStatus, message) {
        super(message);
        this.code = code;
        this.httpStatus = httpStatus;
        this.name = 'KernelClientError';
    }
}
exports.KernelClientError = KernelClientError;
// ─── Client ──────────────────────────────────────────────────────────────────
class KernelClient {
    baseUrl;
    jwtProvider;
    constructor(baseUrl, jwtProvider) {
        this.baseUrl = baseUrl;
        this.jwtProvider = jwtProvider;
    }
    /**
     * GET a Kernel query endpoint.
     * Uses AbortSignal.timeout(10_000) — throws KernelClientError on any failure.
     * No raw HTTP status codes leak to the caller.
     */
    async get(path, params) {
        let response;
        try {
            const url = new URL(path, this.baseUrl);
            if (params) {
                Object.entries(params).forEach(([k, v]) => {
                    if (v != null)
                        url.searchParams.set(k, String(v));
                });
            }
            response = await fetch(url, {
                headers: { Authorization: `Bearer ${this.jwtProvider()}` },
                signal: AbortSignal.timeout(10_000),
            });
        }
        catch (err) {
            if (err instanceof DOMException && err.name === 'TimeoutError') {
                throw new KernelClientError(KernelErrorCode.Timeout, 0, 'Kernel request timed out after 10s');
            }
            throw new KernelClientError(KernelErrorCode.Unavailable, 0, 'Kernel unreachable');
        }
        if (response.ok)
            return response.json();
        // Exhaustive HTTP status mapping — no raw status codes leak to chat
        switch (response.status) {
            case 400: throw new KernelClientError(KernelErrorCode.BadRequest, 400, 'Invalid query parameters');
            case 401: throw new KernelClientError(KernelErrorCode.AuthExpired, 401, 'Session expired. Please log in again.');
            case 429: throw new KernelClientError(KernelErrorCode.RateLimited, 429, 'Too many requests. Retry shortly.');
            case 503: throw new KernelClientError(KernelErrorCode.Unavailable, 503, 'Kernel temporarily unavailable.');
            default: throw new KernelClientError(KernelErrorCode.Unknown, response.status, `Unexpected Kernel response: ${response.status}`);
        }
    }
    /**
     * POST to a Kernel mutation endpoint.
     * Uses AbortSignal.timeout(10_000) — throws KernelClientError on any failure.
     */
    async post(path, body) {
        let response;
        try {
            const url = new URL(path, this.baseUrl);
            response = await fetch(url, {
                method: 'POST',
                headers: {
                    Authorization: `Bearer ${this.jwtProvider()}`,
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(body),
                signal: AbortSignal.timeout(10_000),
            });
        }
        catch (err) {
            if (err instanceof DOMException && err.name === 'TimeoutError') {
                throw new KernelClientError(KernelErrorCode.Timeout, 0, 'Kernel request timed out after 10s');
            }
            throw new KernelClientError(KernelErrorCode.Unavailable, 0, 'Kernel unreachable');
        }
        if (response.ok) {
            const text = await response.text();
            return (text ? JSON.parse(text) : undefined);
        }
        switch (response.status) {
            case 400: throw new KernelClientError(KernelErrorCode.BadRequest, 400, 'Invalid request body');
            case 401: throw new KernelClientError(KernelErrorCode.AuthExpired, 401, 'Session expired. Please log in again.');
            case 429: throw new KernelClientError(KernelErrorCode.RateLimited, 429, 'Too many requests. Retry shortly.');
            case 503: throw new KernelClientError(KernelErrorCode.Unavailable, 503, 'Kernel temporarily unavailable.');
            default: throw new KernelClientError(KernelErrorCode.Unknown, response.status, `Unexpected Kernel response: ${response.status}`);
        }
    }
    // ─── Spatial BIM methods (Phase 3A) ──────────────────────────────────────────
    async registerSpace(body) {
        return this.post('/api/spaces', body);
    }
    async registerElement(spaceId, body) {
        return this.post(`/api/spaces/${spaceId}/elements`, body);
    }
    async linkTask(elementId, body) {
        await this.post(`/api/elements/${elementId}/links`, body);
    }
    async getSpatialSnapshot(spaceId, at) {
        return this.get(`/api/spaces/${spaceId}/timeline`, { at });
    }
    async getSpatialEvents(spaceId, params) {
        return this.get(`/api/spaces/${spaceId}/timeline/events`, params);
    }
    // ─── Snapshot + Escrow methods (Phase 3B) ────────────────────────────────────
    async getSnapshot(aggregateId, at) {
        return this.get(`/api/snapshots/${aggregateId}`, { at });
    }
    async getSnapshotVersions(aggregateId, page, pageSize) {
        return this.get(`/api/snapshots/${aggregateId}/versions`, { page, pageSize });
    }
    async verifyChain(from, to) {
        return this.get('/api/audit-events/verify-chain', { from, to });
    }
    async uploadProof(taskId, stream, contentType) {
        let response;
        try {
            const { Readable } = await Promise.resolve().then(() => __importStar(require('stream')));
            const webStream = Readable.toWeb(stream);
            const url = new URL(`/api/tasks/${taskId}/proof`, this.baseUrl);
            response = await fetch(url, {
                method: 'POST',
                headers: {
                    Authorization: `Bearer ${this.jwtProvider()}`,
                    'Content-Type': contentType,
                },
                body: webStream,
                signal: AbortSignal.timeout(120_000),
            });
        }
        catch (err) {
            if (err instanceof DOMException && err.name === 'TimeoutError') {
                throw new KernelClientError(KernelErrorCode.Timeout, 0, 'Kernel request timed out after 120s');
            }
            throw new KernelClientError(KernelErrorCode.Unavailable, 0, 'Kernel unreachable');
        }
        if (response.ok) {
            const text = await response.text();
            return (text ? JSON.parse(text) : undefined);
        }
        switch (response.status) {
            case 400: throw new KernelClientError(KernelErrorCode.BadRequest, 400, 'Invalid proof upload');
            case 401: throw new KernelClientError(KernelErrorCode.AuthExpired, 401, 'Session expired. Please log in again.');
            case 415: throw new KernelClientError(KernelErrorCode.BadRequest, 415, 'Unsupported media type');
            case 429: throw new KernelClientError(KernelErrorCode.RateLimited, 429, 'Too many requests. Retry shortly.');
            case 503: throw new KernelClientError(KernelErrorCode.Unavailable, 503, 'Kernel temporarily unavailable.');
            default: throw new KernelClientError(KernelErrorCode.Unknown, response.status, `Unexpected Kernel response: ${response.status}`);
        }
    }
}
exports.KernelClient = KernelClient;
// ─── Module-level singleton — JWT set once per request via setKernelJwt() ────
let _currentJwt = '';
function setKernelJwt(token) {
    _currentJwt = token;
}
exports.kernelClient = new KernelClient(env_1.env.KERNEL_BASE_URL, () => _currentJwt);
//# sourceMappingURL=kernelClient.js.map