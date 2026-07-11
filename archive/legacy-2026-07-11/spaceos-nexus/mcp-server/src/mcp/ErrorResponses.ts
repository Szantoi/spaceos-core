import { ErrorCodes } from './ErrorCodes';

/**
 * Standardized error response factory for MCP tools.
 * Ensures LLM-friendly, structured error data as per EPIC-11 requirements.
 */
export class ErrorResponses {
    static badRequest(message: string, details?: any) {
        return this.createError(ErrorCodes.INVALID_INPUT, message, details);
    }

    static unauthorized(message: string, details?: any) {
        return this.createError(ErrorCodes.UNAUTHORIZED, message, details);
    }

    static notFound(message: string, details?: any) {
        return this.createError(ErrorCodes.NOT_FOUND, message, details);
    }

    static internalError(message: string, details?: any) {
        return this.createError(ErrorCodes.INTERNAL_ERROR, message, details);
    }

    static contextError(message: string, details?: any) {
        return this.createError(ErrorCodes.CONTEXT_ERROR, message, details);
    }

    static forbidden(message: string, details?: any) {
        return this.createError(ErrorCodes.FORBIDDEN, message, details);
    }

    static serviceUnavailable(message: string, details?: any) {
        return this.createError(ErrorCodes.SERVICE_UNAVAILABLE, message, details);
    }

    static unauthorizedTrackAccess(toolName: string, currentTrack: string, requiredTrack: string) {
        return this.createError(
            ErrorCodes.FORBIDDEN,
            `Tool '${toolName}' is not available in the '${currentTrack}' track. It belongs to the '${requiredTrack}' track.`,
            { toolName, currentTrack, requiredTrack, code: 'unauthorized_track_access' }
        );
    }

    static invalidTrack(message: string, details?: any) {
        return this.createError(ErrorCodes.TRACK_ERROR, message, details);
    }

    /**
     * Internal helper to create structured MPC error responses.
     * Includes legacy proxies for backward compatibility with older result structures.
     */
    public static createError(code: string, message: string, details?: any): MpcErrorResponse {
        const error = {
            isError: true,
            success: false as const,
            error: {
                code,
                message,
                details,
                timestamp: new Date().toISOString()
            },
            content: [
                {
                    type: 'text' as const,
                    text: `[${code}] ${message}${details ? ' - ' + JSON.stringify(details) : ''}`
                }
            ]
        } as any;

        // Legacy proxies for 'code' and 'message' properties
        Object.defineProperty(error, 'code', {
            get: function () { return this.error.code; },
            set: function (v) { this.error.code = v; },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(error, 'message', {
            get: function () { return this.error.message; },
            set: function (v) { this.error.message = v; },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(error, 'details', {
            get: function () { return this.error.details; },
            set: function (v) { this.error.details = v; },
            enumerable: true,
            configurable: true
        });

        return error as MpcErrorResponse;
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// BOOTSTRAP ERRORS (Legacy / Internal support for BootstrapService)
// ═════════════════════════════════════════════════════════════════════════════

export interface BootstrapError {
    success: false;
    code: string;
    message: string;
    field?: string;
    details?: Record<string, unknown>;
}

export interface MpcErrorResponse {
    isError: true;
    error: {
        code: string;
        message: string;
        details?: any;
        timestamp: string;
    };
    content: Array<{ type: 'text'; text: string }>;
    // Backward compatibility getters
    code: string;
    message: string;
    success: false;
    details?: any;
}

export function errorInvalidDomain(domain: string, reason?: string): BootstrapError {
    // message must mention "format" for OWASP validation tests (see owasp-injection.test.ts)
    // include the actual value for diagnostics as well
    const base = `Invalid domain format${reason ? ` (${reason})` : ''}`;
    return { success: false, code: 'INVALID_DOMAIN', field: 'domain', message: `${base}: ${domain}`, details: { domain } };
}

export function errorInvalidRole(role: string, reason?: string): BootstrapError {
    // message must mention "format" for OWASP validation tests
    const base = `Invalid role format${reason ? ` (${reason})` : ''}`;
    return { success: false, code: 'INVALID_ROLE', field: 'role', message: `${base}: ${role}`, details: { role } };
}

export function errorRoleNotFound(domain: string, role: string): BootstrapError {
    return { success: false, code: 'ROLE_NOT_FOUND', message: `Role not found: ${domain}/${role}`, details: { domain, role } };
}

export function errorDatabaseError(message: string, details?: Record<string, unknown>): BootstrapError {
    return { success: false, code: 'DATABASE_ERROR', message, details };
}

export function errorSessionCreationFailed(message: string, details?: Record<string, unknown>): BootstrapError {
    return { success: false, code: 'SESSION_CREATION_FAILED', message, details };
}

export function errorPayloadTooLarge(message: string, details?: Record<string, unknown>): BootstrapError {
    return { success: false, code: 'PAYLOAD_TOO_LARGE', message, details };
}

export function errorUnknownError(message: string, details?: Record<string, unknown>): BootstrapError {
    return { success: false, code: 'UNKNOWN_ERROR', message, details };
}

/**
 * Unified type guard for BootstrapError or MPC error response.
 * Handles legacy flat BootstrapError objects as well as structured
 * `MpcErrorResponse` produced by `ErrorResponses.createError()`.
 */
export function isBootstrapError(value: any): value is BootstrapError | MpcErrorResponse {
    if (!value) return false;
    // flat legacy bootstrap error
    if (value.success === false && value.code && value.message) return true;
    // structured MPC error
    if (value.isError === true && value.error && value.error.code) return true;
    return false;
}

/**
 * Error factory for missing/not found session.
 * Single implementation covers both bootstrap and MPC shapes.
 */
export function errorSessionNotFound(
    sessionId: string,
    details?: Record<string, unknown>
): BootstrapError;
export function errorSessionNotFound(
    sessionId: string,
    details?: Record<string, unknown>
): MpcErrorResponse;
export function errorSessionNotFound(
    sessionId: string,
    details?: Record<string, unknown>
): BootstrapError | MpcErrorResponse {
    const error = (ErrorResponses as any).createError(
        ErrorCodes.NOT_FOUND,
        `Session not found: ${sessionId}`,
        { sessionId, ...details }
    );
    error.error.code = 'SESSION_NOT_FOUND';
    return error as any;
}

/**
 * Error factory for RBAC permission violations.
 */
export function errorPermissionDenied(
    toolName: string,
    role: string,
    requiredRoles?: string[]
): BootstrapError;
export function errorPermissionDenied(
    toolName: string,
    role: string,
    requiredRoles?: string[]
): MpcErrorResponse;
export function errorPermissionDenied(
    toolName: string,
    role: string,
    requiredRoles?: string[]
): BootstrapError | MpcErrorResponse {
    const error = (ErrorResponses as any).createError(
        ErrorCodes.FORBIDDEN,
        `Role '${role}' is not allowed to use tool '${toolName}'`,
        { toolName, role, requiredRoles }
    );
    error.error.code = 'PERMISSION_DENIED';
    return error as any;
}

/**
 * Error factory for schema validation failures.
 */
export function errorSchemaValidationError(
    message: string,
    details?: Record<string, unknown>
): BootstrapError;
export function errorSchemaValidationError(
    message: string,
    details?: Record<string, unknown>
): MpcErrorResponse;
export function errorSchemaValidationError(
    message: string,
    details?: Record<string, unknown>
): BootstrapError | MpcErrorResponse {
    const error = (ErrorResponses as any).createError(
        ErrorCodes.INVALID_INPUT,
        `Schema validation failed: ${message}`,
        details
    );
    error.error.code = 'SCHEMA_VALIDATION_ERROR';
    return error as any;
}

/**
 * Error factory for invalid FSM transitions (AC-5).
 */
export function errorFsmInvalidTransition(message: string): MpcErrorResponse {
    const error = (ErrorResponses as any).createError(
        ErrorCodes.INVALID_INPUT,
        message,
        {}
    );
    error.error.code = 'FSM_INVALID_TRANSITION';

    return error;
}

/**
 * Error factory for database lock issues (AC-6).
 */
export function errorDatabaseLocked(message: string): MpcErrorResponse {
    const error = (ErrorResponses as any).createError(
        ErrorCodes.INTERNAL_ERROR,
        message,
        {}
    );
    error.error.code = 'DATABASE_LOCKED';

    return error;
}

/**
 * Error factory for internal server errors (AC-7).
 */
export function errorInternalServerError(message: string): MpcErrorResponse {
    const error = (ErrorResponses as any).createError(
        ErrorCodes.INTERNAL_ERROR,
        message,
        {}
    );
    error.error.code = 'INTERNAL_SERVER_ERROR';

    return error;
}
