// Type definitions for discovery tools

export interface RequestContextPhaseInput {
    phase?: 'ideation' | 'validation' | 'iteration' | 'delivery_handoff';
}

export interface RequestContextPhaseOutput {
    workflow_template: string;
    artifact_templates: Array<{ name: string; content: string }>;
    phase_checklist: string[];
    available_tools: string[];
}

export interface ReferencePriorDiscoveryInput {
    search_text: string;
    phase?: 'ideation' | 'validation' | 'iteration';
    limit?: number;
}

export interface ReferencePriorDiscoveryEpisode {
    episode_id: string;
    phase: string;
    summary: string;
    artifacts: Array<{ title: string; type: string }>;
    similarity_score: number;
}

export interface ReferencePriorDiscoveryOutput {
    episodes: ReferencePriorDiscoveryEpisode[];
    total_found: number;
    fallback_used?: boolean;
}

export type DiscoveryOutcomeType = 'VALIDATED_IDEA' | 'REJECTED_IDEA' | 'LEARNING' | 'HANDOFF';

export interface TrackBlockerInput {
    blocker_text: string;
    severity: 'HIGH' | 'MEDIUM' | 'LOW';
    blocking_phase: 'ideation' | 'validation' | 'iteration';
}

export interface QueryBlockersInput {
    session_id?: string;
    phase?: 'ideation' | 'validation' | 'iteration';
}

export interface BlockerRecord {
    id: number;
    session_id: string;
    phase: string;
    severity: string;
    text: string;
    created_at: string;
}