#!/usr/bin/env node

/**
 * API Performance Benchmark Script
 * Tests response times for Datahaven API endpoints
 */

const endpoints = [
  { name: 'GET /api/planning/domain-focus', url: 'http://localhost:3456/api/planning/domain-focus' },
  { name: 'GET /api/graph/epics', url: 'http://localhost:3456/api/graph/epics' },
  { name: 'GET /api/graph/mermaid/epic/EPICS', url: 'http://localhost:3456/api/graph/mermaid/epic/EPICS' },
];

const RUNS = 20;

async function benchmark(endpoint) {
  const times = [];

  for (let i = 0; i < RUNS; i++) {
    const start = Date.now();
    try {
      const res = await fetch(endpoint.url);
      await res.text(); // Read full response
      const duration = Date.now() - start;
      times.push(duration);
    } catch (err) {
      console.error(`Error fetching ${endpoint.name}:`, err.message);
      return null;
    }
  }

  // Calculate statistics
  times.sort((a, b) => a - b);
  const avg = times.reduce((a, b) => a + b, 0) / times.length;
  const p50 = times[Math.floor(times.length * 0.5)];
  const p95 = times[Math.floor(times.length * 0.95)];
  const p99 = times[Math.floor(times.length * 0.99)];
  const min = times[0];
  const max = times[times.length - 1];

  return { avg, p50, p95, p99, min, max };
}

async function main() {
  console.log('API Performance Benchmark');
  console.log('='.repeat(80));
  console.log(`Runs per endpoint: ${RUNS}`);
  console.log('Target: GET <100ms (p95), PUT <300ms (p95)');
  console.log('='.repeat(80));
  console.log();

  for (const endpoint of endpoints) {
    const stats = await benchmark(endpoint);

    if (!stats) {
      console.log(`${endpoint.name}: FAILED`);
      continue;
    }

    const status = stats.p95 < 100 ? '✅' : '⚠️';

    console.log(`${status} ${endpoint.name}`);
    console.log(`   Min: ${stats.min}ms | Avg: ${stats.avg.toFixed(1)}ms | p50: ${stats.p50}ms | p95: ${stats.p95}ms | p99: ${stats.p99}ms | Max: ${stats.max}ms`);
    console.log();
  }
}

main().catch(console.error);
