// src/types/kernel.types.ts
// TypeScript mirrors of C# DTOs — keep in sync with SpaceOS.Kernel.Application DTOs.

// ─── Common ──────────────────────────────────────────────────────────────────

export interface PagedList<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export type FsmState =
  | 'BACKLOG_READY'
  | 'IN_DEV'
  | 'CODE_REVIEW'
  | 'QA_WAITING'
  | 'QA_IN_PROGRESS'
  | 'ARCHITECT_SIGNOFF'
  | 'WAITING_FOR_INPUT'
  | 'ESCALATED'
  | 'CLOSED_DONE'
  | 'CLOSED_BLOCKED';

export type TradeType =
  | 'Joinery'
  | 'MEP'
  | 'Electrical'
  | 'Architecture'
  | 'Generic';

export type WorkStationStatus =
  | 'Idle'
  | 'Active'
  | 'Maintenance'
  | 'Offline';

// ─── Tenant ──────────────────────────────────────────────────────────────────

export interface TenantDto {
  id: string;
  name: string;
}

export interface CreateTenantRequest {
  name: string;
}

export interface UpdateTenantNameRequest {
  name: string;
}

// ─── Facility ────────────────────────────────────────────────────────────────

export interface FacilityDto {
  id: string;
  tenantId: string;
  name: string;
}

export interface CreateFacilityRequest {
  tenantId: string;
  name: string;
}

export interface RenameFacilityRequest {
  name: string;
}

// ─── WorkStation ─────────────────────────────────────────────────────────────

export interface WorkStationDto {
  id: string;
  facilityId: string;
  name: string;
  type: string;
  status: WorkStationStatus;
}

export interface RegisterWorkStationRequest {
  facilityId: string;
  name: string;
  type: string;
}

export interface UpdateWorkStationStatusRequest {
  status: WorkStationStatus;
}

// ─── SpaceLayer ───────────────────────────────────────────────────────────────

export interface SpaceLayerDto {
  id: string;
  facilityId: string;
  tradeType: TradeType;
  isExternalNode: boolean;
  externalSourceUrl: string | null;
  intentDataJson: string | null;
  lastStateHash: string;
}

export interface RegisterSpaceLayerRequest {
  facilityId: string;
  tradeType: TradeType;
  isExternalNode?: boolean;
  externalSourceUrl?: string;
  intentDataJson?: string;
}

export interface UpdateSpaceLayerIntentRequest {
  intentDataJson: string;
}

// ─── FlowEpic ─────────────────────────────────────────────────────────────────

export interface FlowEpicDto {
  id: string;
  facilityId: string;
  workStationId: string;
  title: string;
  state: FsmState;
  assigneeTenantId: string | null;
  guestTenantId: string | null;
  delegatedAt: string | null;
}

export interface CreateFlowEpicRequest {
  facilityId: string;
  workStationId: string;
  title: string;
}

export interface UpdateFlowEpicTitleRequest {
  title: string;
}

export interface DelegateFlowEpicRequest {
  guestTenantId: string;
}

// ─── Audit ────────────────────────────────────────────────────────────────────

export interface AuditEventDto {
  id: string;
  tenantId: string;
  eventType: string;
  payload: string;
  stateHash: string;
  occurredAt: string;
}

// ─── Auth ─────────────────────────────────────────────────────────────────────

export interface TokenRequest {
  username: string;
  password: string;
}

export interface TokenResponse {
  token: string;
  expiresAt: string;
}

// ─── Spatial BIM (Phase 3A) ───────────────────────────────────────────────────

export type PagedResult<T> = PagedList<T>;

export interface RegisterPhysicalSpaceRequest {
  facilityId: string;
  name: string;
}

export interface SpaceResponse {
  id: string;
  facilityId: string;
  name: string;
}

export interface RegisterSpatialElementRequest {
  name: string;
}

export interface ElementResponse {
  id: string;
  spaceId: string;
  name: string;
}

export interface LinkTaskToElementRequest {
  flowTaskId: string;
}

// Note: elementType is intentionally absent — ADR-008 (security decision)
export interface SpatialSnapshotResponse {
  spaceId: string;
  at: string;
  elements: Array<{
    id: string;
    name: string;
    linkedFlowTaskIds: string[];
  }>;
}

export interface TimelineQueryParams {
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}

export interface SpatialTimelineEvent {
  id: string;
  spaceId: string;
  eventType: string;
  occurredAt: string;
}

// ─── Snapshot + Escrow (Phase 3B) ─────────────────────────────────────────────

export interface SnapshotDto {
  id: string;
  aggregateId: string;
  at: string;
  stateHash: string;
  payload: string;
}

export interface SnapshotVersionDto {
  id: string;
  aggregateId: string;
  at: string;
  stateHash: string;
}

export interface ChainVerificationDto {
  isValid: boolean;
  firstBrokenAt: string | null;
  totalRecordsChecked: number;
  wormStorageAvailable: boolean;
  diagnosticMessage: string | null;
}

export interface ProofReceiptDto {
  id: string;
  taskId: string;
  proofHash: string;
  receivedAt: string;
}
