import { FSMState, AgentSession, WorkflowDefinition, SessionHistory } from '../metadata/FSMSchema';

const session: AgentSession = {
    session_id: 'uuid-1234',
    domain: 'engineering',
    role_name: 'backend_developer',
    workflow_id: 'agile-epic-lifecycle-v1',
    current_state: 'INITIALIZED',
    created_at: new Date().toISOString(),
    updated_at: new Date().toISOString()
};

const history: SessionHistory = {
    id: 1,
    session_id: 'uuid-1234',
    state_before: 'INITIALIZED',
    state_after: 'IN_PROGRESS',
    action: 'start_work',
    timestamp: new Date().toISOString()
};

console.log('Session state:', session.current_state);
console.log('History transition:', history.state_before, '->', history.state_after);
console.log('TypeScript types verification: SUCCESS');
