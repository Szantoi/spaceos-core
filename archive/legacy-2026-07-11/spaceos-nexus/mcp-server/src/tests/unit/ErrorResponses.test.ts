import { describe, it, expect } from 'vitest';
import { ErrorResponses } from '../../mcp/ErrorResponses';
import { ErrorCodes } from '../../mcp/ErrorCodes';

describe('ErrorResponses', () => {
    it('creates badRequest response', () => {
        const result = ErrorResponses.badRequest('Invalid payload', { field: 'name' });
        expect(result.isError).toBe(true);
        expect(result.error.code).toBe(ErrorCodes.INVALID_INPUT);
        expect(result.error.message).toBe('Invalid payload');
        expect(result.error.details.field).toBe('name');
        expect(result.content[0].text).toContain('[INVALID_INPUT] Invalid payload');
        expect(result.content[0].text).toContain('{"field":"name"}');
    });

    it('creates unauthorized response', () => {
        const result = ErrorResponses.unauthorized('No access');
        expect(result.isError).toBe(true);
        expect(result.error.code).toBe(ErrorCodes.UNAUTHORIZED);
        expect(result.error.message).toBe('No access');
        expect(result.error.details).toBeUndefined();
    });

    it('creates notFound response', () => {
        const result = ErrorResponses.notFound('Resource missing');
        expect(result.isError).toBe(true);
        expect(result.error.code).toBe(ErrorCodes.NOT_FOUND);
        expect(result.error.message).toBe('Resource missing');
    });

    it('creates internalError response', () => {
        const result = ErrorResponses.internalError('Server crashed');
        expect(result.isError).toBe(true);
        expect(result.error.code).toBe(ErrorCodes.INTERNAL_ERROR);
        expect(result.error.message).toBe('Server crashed');
    });

    it('creates contextError response', () => {
        const result = ErrorResponses.contextError('Context invalid');
        expect(result.isError).toBe(true);
        expect(result.error.code).toBe(ErrorCodes.CONTEXT_ERROR);
        expect(result.error.message).toBe('Context invalid');
    });

    it('creates forbidden response', () => {
        const result = ErrorResponses.forbidden('Action denied');
        expect(result.isError).toBe(true);
        expect(result.error.code).toBe(ErrorCodes.FORBIDDEN);
        expect(result.error.message).toBe('Action denied');
    });

    it('creates serviceUnavailable response', () => {
        const result = ErrorResponses.serviceUnavailable('Down for maintenance');
        expect(result.isError).toBe(true);
        expect(result.error.code).toBe(ErrorCodes.SERVICE_UNAVAILABLE);
        expect(result.error.message).toBe('Down for maintenance');
    });

    it('creates unauthorizedTrackAccess response', () => {
        const result = ErrorResponses.unauthorizedTrackAccess('test_tool', 'discovery', 'delivery');
        expect(result.isError).toBe(true);
        expect(result.error.code).toBe(ErrorCodes.FORBIDDEN);
        expect(result.error.message).toContain("Tool 'test_tool' is not available in the 'discovery' track");
        expect(result.error.details).toEqual({
            toolName: 'test_tool',
            currentTrack: 'discovery',
            requiredTrack: 'delivery',
            code: 'unauthorized_track_access'
        });
    });

    it('creates invalidTrack response', () => {
        const result = ErrorResponses.invalidTrack('Track mismatch');
        expect(result.isError).toBe(true);
        expect(result.error.code).toBe(ErrorCodes.TRACK_ERROR);
        expect(result.error.message).toBe('Track mismatch');
    });
});
