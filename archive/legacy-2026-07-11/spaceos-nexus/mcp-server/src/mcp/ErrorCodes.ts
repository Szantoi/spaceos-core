/**
 * Standardized error codes for JoineryTech MCP Server.
 * Used for LLM-friendly error handling and standardized responses.
 */
export enum ErrorCodes {
    INVALID_INPUT = 'INVALID_INPUT',
    UNAUTHORIZED = 'UNAUTHORIZED',
    NOT_FOUND = 'NOT_FOUND',
    INTERNAL_ERROR = 'INTERNAL_ERROR',
    CONTEXT_ERROR = 'CONTEXT_ERROR',
    AUDIT_ERROR = 'AUDIT_ERROR',
    TRACK_ERROR = 'TRACK_ERROR',
    FORBIDDEN = 'FORBIDDEN',
    METHOD_NOT_ALLOWED = 'METHOD_NOT_ALLOWED',
    SERVICE_UNAVAILABLE = 'SERVICE_UNAVAILABLE'
}
