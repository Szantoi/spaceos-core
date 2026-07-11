// vitest.config.ts
import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    globals: false,
    environment: 'node',
    include: ['src/**/*.test.ts'],
    env: {
      NODE_ENV:         'test',
      PORT:             '3000',
      KERNEL_BASE_URL:  'http://localhost:5001',
      JWT_ALGORITHM:    'ES256',
      LLM_PROVIDER:     'mock',
      MAX_TOOL_ITERATIONS: '5',
      OPENAI_API_KEY:   'test-openai-key',
      OPENAI_BASE_URL:  'https://api.example.com/v1',
      OPENAI_MODEL:     'test-model',
    },
    coverage: {
      provider: 'v8',
      reporter: ['text', 'lcov'],
      include: ['src/**/*.ts'],
      exclude: ['src/**/*.test.ts', 'src/index.ts'],
    },
  },
});
