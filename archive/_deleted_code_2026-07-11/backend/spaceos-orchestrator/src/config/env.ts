// src/config/env.ts
import { z } from 'zod';

const envSchema = z.object({
  PORT:               z.string().default('3000'),
  NODE_ENV:           z.enum(['development', 'production', 'test']).default('development'),
  KERNEL_BASE_URL:    z.string().url(),
  LLM_PROVIDER:       z.enum(['anthropic', 'openai', 'mock']).default('mock'),
  ANTHROPIC_API_KEY:  z.string().optional(),
  OPENAI_API_KEY:     z.string().optional(),
  OPENAI_BASE_URL:    z.string().url().default('https://generativelanguage.googleapis.com/v1beta/openai'),
  OPENAI_MODEL:       z.string().default('gemini-2.0-flash'),
  MAX_TOOL_ITERATIONS: z.string().default('5').transform(Number),
  CORS_ORIGINS:        z.string().default('http://localhost:5173,http://localhost:3001'),

  // JWT / JWKS (Keycloak OIDC)
  JWKS_URI:            z.string().default(''),
  JWT_ISSUER:          z.string().default(''),
  JWT_AUDIENCE:        z.string().default('kernel-api'),

  // Test seeder infrastructure (test.route.ts)
  SPACEOS_TEST_ENDPOINTS_ENABLED__DANGER: z.string().optional(),
  TEST_SEEDER_SECRET:   z.string().optional(),
  TEST_TENANT_ALLOWLIST: z.string().optional(),
  KC_TOKEN_URL:              z.string().optional(),
  TEST_RUNNER_CLIENT_ID:     z.string().optional(),
  TEST_RUNNER_CLIENT_SECRET: z.string().optional(),

  // Module base URLs (used by test seeder for cross-module seed/reset)
  JOINERY_BASE_URL:        z.string().url().default('http://127.0.0.1:5002'),
  IDENTITY_BASE_URL:       z.string().url().default('http://127.0.0.1:5003'),
  INVENTORY_BASE_URL:      z.string().url().default('http://127.0.0.1:5004'),
  CUTTING_BASE_URL:        z.string().url().default('http://127.0.0.1:5004'),
  PROCUREMENT_BASE_URL:    z.string().url().default('http://127.0.0.1:5006'),

  // PostgreSQL connection (RAG knowledge base)
  DATABASE_URL:            z.string().optional(),
});

const parsed = envSchema.safeParse(process.env);

if (!parsed.success) {
  console.error('Invalid environment variables:');
  console.error(parsed.error.flatten().fieldErrors);
  process.exit(1);
}

export const env = parsed.data;
