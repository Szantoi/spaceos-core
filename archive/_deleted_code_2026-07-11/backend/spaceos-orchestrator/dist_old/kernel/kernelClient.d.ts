import type { RegisterPhysicalSpaceRequest, SpaceResponse, RegisterSpatialElementRequest, ElementResponse, LinkTaskToElementRequest, SpatialSnapshotResponse, TimelineQueryParams, SpatialTimelineEvent, PagedResult, SnapshotDto, SnapshotVersionDto, PagedList, ChainVerificationDto, ProofReceiptDto } from '../types/kernel.types';
export declare enum KernelErrorCode {
    AuthExpired = "ERR_TOOL_AUTH_EXPIRED",
    RateLimited = "ERR_TOOL_RATE_LIMITED",
    Unavailable = "ERR_KERNEL_UNAVAILABLE",
    Timeout = "ERR_KERNEL_TIMEOUT",
    BadRequest = "ERR_TOOL_BAD_REQUEST",
    Unknown = "ERR_KERNEL_UNKNOWN"
}
export declare class KernelClientError extends Error {
    readonly code: KernelErrorCode;
    readonly httpStatus: number;
    constructor(code: KernelErrorCode, httpStatus: number, message: string);
}
export declare class KernelClient {
    private readonly baseUrl;
    private readonly jwtProvider;
    constructor(baseUrl: string, jwtProvider: () => string);
    /**
     * GET a Kernel query endpoint.
     * Uses AbortSignal.timeout(10_000) — throws KernelClientError on any failure.
     * No raw HTTP status codes leak to the caller.
     */
    get<T>(path: string, params?: Record<string, unknown>): Promise<T>;
    /**
     * POST to a Kernel mutation endpoint.
     * Uses AbortSignal.timeout(10_000) — throws KernelClientError on any failure.
     */
    post<T>(path: string, body: unknown): Promise<T>;
    registerSpace(body: RegisterPhysicalSpaceRequest): Promise<SpaceResponse>;
    registerElement(spaceId: string, body: RegisterSpatialElementRequest): Promise<ElementResponse>;
    linkTask(elementId: string, body: LinkTaskToElementRequest): Promise<void>;
    getSpatialSnapshot(spaceId: string, at: string): Promise<SpatialSnapshotResponse>;
    getSpatialEvents(spaceId: string, params: TimelineQueryParams): Promise<PagedResult<SpatialTimelineEvent>>;
    getSnapshot(aggregateId: string, at: string): Promise<SnapshotDto>;
    getSnapshotVersions(aggregateId: string, page: number, pageSize: number): Promise<PagedList<SnapshotVersionDto>>;
    verifyChain(from: string, to: string): Promise<ChainVerificationDto>;
    uploadProof(taskId: string, stream: NodeJS.ReadableStream, contentType: string): Promise<ProofReceiptDto>;
}
export declare function setKernelJwt(token: string): void;
export declare const kernelClient: KernelClient;
//# sourceMappingURL=kernelClient.d.ts.map