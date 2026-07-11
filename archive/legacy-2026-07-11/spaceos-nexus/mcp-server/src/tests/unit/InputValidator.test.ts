import { describe, it, expect } from 'vitest';
import { InputValidator } from '../../mcp/InputValidator';

describe('InputValidator', () => {
    describe('validateDomain', () => {
        it('should pass for valid lowercase domain with hyphens', () => {
            expect(() => InputValidator.validateDomain('engineering')).not.toThrow();
            expect(() => InputValidator.validateDomain('qa-team')).not.toThrow();
            expect(() => InputValidator.validateDomain('cloud-infrastructure-services')).not.toThrow();
        });

        it('should pass for domain with exactly 1 character', () => {
            expect(() => InputValidator.validateDomain('a')).not.toThrow();
        });

        it('should pass for domain with exactly 64 characters', () => {
            const longDomain = 'a'.repeat(64);
            expect(() => InputValidator.validateDomain(longDomain)).not.toThrow();
        });

        it('should throw for domain exceeding 64 characters', () => {
            const tooLongDomain = 'a'.repeat(65);
            expect(() => InputValidator.validateDomain(tooLongDomain)).toThrow();
            try {
                InputValidator.validateDomain(tooLongDomain);
            } catch (e: any) {
                expect(e.code).toBe('INVALID_DOMAIN');
                expect(e.reason).toBe('too_long');
            }
        });

        it('should throw for empty domain', () => {
            expect(() => InputValidator.validateDomain('')).toThrow();
            expect(() => InputValidator.validateDomain(undefined)).toThrow();
            expect(() => InputValidator.validateDomain(null)).toThrow();
        });

        it('should throw for uppercase letters', () => {
            expect(() => InputValidator.validateDomain('Engineering')).toThrow();
            expect(() => InputValidator.validateDomain('ENGINEERING')).toThrow();
        });

        it('should throw for underscores', () => {
            expect(() => InputValidator.validateDomain('engineering_team')).toThrow();
        });

        it('should throw for special characters', () => {
            expect(() => InputValidator.validateDomain('domain!')).toThrow();
            expect(() => InputValidator.validateDomain('domain@site')).toThrow();
            expect(() => InputValidator.validateDomain('domain.com')).toThrow();
        });
    });

    describe('validateRole', () => {
        it('should pass for valid lowercase role with underscores', () => {
            expect(() => InputValidator.validateRole('backend_developer')).not.toThrow();
            expect(() => InputValidator.validateRole('orchestrator')).not.toThrow();
            expect(() => InputValidator.validateRole('lead_sre_engineer')).not.toThrow();
        });

        it('should pass for role with exactly 1 character', () => {
            expect(() => InputValidator.validateRole('r')).not.toThrow();
        });

        it('should pass for role with exactly 64 characters', () => {
            const longRole = 'r'.repeat(64);
            expect(() => InputValidator.validateRole(longRole)).not.toThrow();
        });

        it('should throw for role exceeding 64 characters', () => {
            const tooLongRole = 'r'.repeat(65);
            expect(() => InputValidator.validateRole(tooLongRole)).toThrow();
            try {
                InputValidator.validateRole(tooLongRole);
            } catch (e: any) {
                expect(e.code).toBe('INVALID_ROLE');
                expect(e.reason).toBe('too_long');
            }
        });

        it('should throw for hyphens in role', () => {
            expect(() => InputValidator.validateRole('backend-developer')).toThrow();
        });

        it('should throw for uppercase letters in role', () => {
            expect(() => InputValidator.validateRole('Backend_Developer')).toThrow();
        });

        it('should throw for spaces', () => {
            expect(() => InputValidator.validateRole('lead engineer')).toThrow();
        });
    });

    describe('validateBootstrapInput', () => {
        it('should validate both domain and role', () => {
            expect(() => InputValidator.validateBootstrapInput('engineering', 'backend_developer')).not.toThrow();
            expect(() => InputValidator.validateBootstrapInput('INVALID', 'backend_developer')).toThrow();
            expect(() => InputValidator.validateBootstrapInput('engineering', 'INVALID')).toThrow();
        });
    });
});
