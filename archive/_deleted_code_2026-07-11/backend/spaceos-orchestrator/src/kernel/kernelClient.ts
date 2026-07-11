// src/kernel/kernelClient.ts
// Typed HTTP client for Kernel query endpoints — uses native fetch with full error map.
// ONLY this file calls Kernel query API via fetch (existing kernel.action.ts uses axios for mutations).

import { env } from '../config/env';
import type {
  RegisterPhysicalSpaceRequest, SpaceResponse,
  RegisterSpatialElementRequest, ElementResponse,
  LinkTaskToElementRequest,
  SpatialSnapshotResponse,
  TimelineQueryParams, SpatialTimelineEvent, PagedResult,
  SnapshotDto, SnapshotVersionDto, PagedList,
  ChainVerificationDto, ProofReceiptDto,
} from '../types/kernel.types';

// ─── Error codes ─────────────────────────────────────────────────────────────

export enum KernelErrorCode {
  AuthExpired  = 'ERR_TOOL_AUTH_EXPIRED',
  RateLimited  = 'ERR_TOOL_RATE_LIMITED',
  Unavailable  = 'ERR_KERNEL_UNAVAILABLE',
  Timeout      = 'ERR_KERNEL_TIMEOUT',
  BadRequest   = 'ERR_TOOL_BAD_REQUEST',
  Unknown      = 'ERR_KERNEL_UNKNOWN',
}

export class KernelClientError extends Error {
  constructor(
    public readonly code: KernelErrorCode,
    public readonly httpStatus: number,
    message: string,
  ) {
    super(message);
    this.name = 'KernelClientError';
  }
}

// ─── Client ──────────────────────────────────────────────────────────────────

export class KernelClient {
  constructor(
    private readonly baseUrl: string,
    private readonly jwtProvider: () => string,
  ) {}

  /**
   * GET a Kernel query endpoint.
   * Uses AbortSignal.timeout(10_000) — throws KernelClientError on any failure.
   * No raw HTTP status codes leak to the caller.
   */
  async get<T>(path: string, params?: Record<string, unknown>): Promise<T> {
    let response: Response;

    try {
      const url = new URL(path, this.baseUrl);
      if (params) {
        Object.entries(params).forEach(([k, v]) => {
          if (v != null) url.searchParams.set(k, String(v));
        });
      }

      response = await fetch(url, {
        headers: { Authorization: `Bearer ${this.jwtProvider()}` },
        signal: AbortSignal.timeout(10_000),
      });
    } catch (err: unknown) {
      if (err instanceof DOMException && err.name === 'TimeoutError') {
        throw new KernelClientError(
          KernelErrorCode.Timeout,
          0,
          'Kernel request timed out after 10s',
        );
      }
      throw new KernelClientError(
        KernelErrorCode.Unavailable,
        0,
        'Kernel unreachable',
      );
    }

    if (response.ok) return response.json() as Promise<T>;

    // Exhaustive HTTP status mapping — no raw status codes leak to chat
    switch (response.status) {
      case 400: throw new KernelClientError(KernelErrorCode.BadRequest,  400, 'Invalid query parameters');
      case 401: throw new KernelClientError(KernelErrorCode.AuthExpired, 401, 'Session expired. Please log in again.');
      case 429: throw new KernelClientError(KernelErrorCode.RateLimited, 429, 'Too many requests. Retry shortly.');
      case 503: throw new KernelClientError(KernelErrorCode.Unavailable, 503, 'Kernel temporarily unavailable.');
      default:  throw new KernelClientError(
                  KernelErrorCode.Unknown,
                  response.status,
                  `Unexpected Kernel response: ${response.status}`,
                );
    }
  }

  /**
   * POST to a Kernel mutation endpoint.
   * Uses AbortSignal.timeout(10_000) — throws KernelClientError on any failure.
   */
  async post<T>(path: string, body: unknown): Promise<T> {
    let response: Response;

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
    } catch (err: unknown) {
      if (err instanceof DOMException && err.name === 'TimeoutError') {
        throw new KernelClientError(KernelErrorCode.Timeout, 0, 'Kernel request timed out after 10s');
      }
      throw new KernelClientError(KernelErrorCode.Unavailable, 0, 'Kernel unreachable');
    }

    if (response.ok) {
      const text = await response.text();
      return (text ? JSON.parse(text) : undefined) as T;
    }

    switch (response.status) {
      case 400: throw new KernelClientError(KernelErrorCode.BadRequest,  400, 'Invalid request body');
      case 401: throw new KernelClientError(KernelErrorCode.AuthExpired, 401, 'Session expired. Please log in again.');
      case 429: throw new KernelClientError(KernelErrorCode.RateLimited, 429, 'Too many requests. Retry shortly.');
      case 503: throw new KernelClientError(KernelErrorCode.Unavailable, 503, 'Kernel temporarily unavailable.');
      default:  throw new KernelClientError(
                  KernelErrorCode.Unknown,
                  response.status,
                  `Unexpected Kernel response: ${response.status}`,
                );
    }
  }

  // ─── Spatial BIM methods (Phase 3A) ──────────────────────────────────────────

  async registerSpace(body: RegisterPhysicalSpaceRequest): Promise<SpaceResponse> {
    return this.post<SpaceResponse>('/api/spaces', body);
  }

  async registerElement(spaceId: string, body: RegisterSpatialElementRequest): Promise<ElementResponse> {
    return this.post<ElementResponse>(`/api/spaces/${spaceId}/elements`, body);
  }

  async linkTask(elementId: string, body: LinkTaskToElementRequest): Promise<void> {
    await this.post<void>(`/api/elements/${elementId}/links`, body);
  }

  async getSpatialSnapshot(spaceId: string, at: string): Promise<SpatialSnapshotResponse> {
    return this.get<SpatialSnapshotResponse>(`/api/spaces/${spaceId}/timeline`, { at });
  }

  async getSpatialEvents(spaceId: string, params: TimelineQueryParams): Promise<PagedResult<SpatialTimelineEvent>> {
    return this.get<PagedResult<SpatialTimelineEvent>>(
      `/api/spaces/${spaceId}/timeline/events`,
      params as Record<string, unknown>,
    );
  }

  // ─── Snapshot + Escrow methods (Phase 3B) ────────────────────────────────────

  async getSnapshot(aggregateId: string, at: string): Promise<SnapshotDto> {
    return this.get<SnapshotDto>(`/api/snapshots/${aggregateId}`, { at });
  }

  async getSnapshotVersions(aggregateId: string, page: number, pageSize: number): Promise<PagedList<SnapshotVersionDto>> {
    return this.get<PagedList<SnapshotVersionDto>>(`/api/snapshots/${aggregateId}/versions`, { page, pageSize });
  }

  async verifyChain(from: string, to: string): Promise<ChainVerificationDto> {
    return this.get<ChainVerificationDto>('/api/audit-events/verify-chain', { from, to });
  }

  async uploadProof(taskId: string, stream: NodeJS.ReadableStream, contentType: string): Promise<ProofReceiptDto> {
    let response: Response;
    try {
      const { Readable } = await import('stream');
      const webStream = Readable.toWeb(stream as import('stream').Readable) as ReadableStream<Uint8Array>;
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
    } catch (err: unknown) {
      if (err instanceof DOMException && err.name === 'TimeoutError') {
        throw new KernelClientError(KernelErrorCode.Timeout, 0, 'Kernel request timed out after 120s');
      }
      throw new KernelClientError(KernelErrorCode.Unavailable, 0, 'Kernel unreachable');
    }
    if (response.ok) {
      const text = await response.text();
      return (text ? JSON.parse(text) : undefined) as ProofReceiptDto;
    }
    switch (response.status) {
      case 400: throw new KernelClientError(KernelErrorCode.BadRequest,  400, 'Invalid proof upload');
      case 401: throw new KernelClientError(KernelErrorCode.AuthExpired, 401, 'Session expired. Please log in again.');
      case 415: throw new KernelClientError(KernelErrorCode.BadRequest,  415, 'Unsupported media type');
      case 429: throw new KernelClientError(KernelErrorCode.RateLimited, 429, 'Too many requests. Retry shortly.');
      case 503: throw new KernelClientError(KernelErrorCode.Unavailable, 503, 'Kernel temporarily unavailable.');
      default:  throw new KernelClientError(
                  KernelErrorCode.Unknown,
                  response.status,
                  `Unexpected Kernel response: ${response.status}`,
                );
    }
  }
}

// ─── Module-level singleton — JWT set once per request via setKernelJwt() ────

let _currentJwt = '';

export function setKernelJwt(token: string): void {
  _currentJwt = token;
}

export const kernelClient = new KernelClient(
  env.KERNEL_BASE_URL,
  () => _currentJwt,
);
