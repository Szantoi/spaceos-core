# Librarian — Feldolgozási napló (PROCESSED_LOG)

**Utolsó feldolgozás:** 2026-04-20 · LIB-002 · ~70 új üzenet elemezve (összesen: ~200+)
**Kimenetek:** `docs/knowledge/` — 18 fájl frissítve (7 context + MODULE_BOUNDARIES + ADR_CATALOGUE + KNOWN_GOTCHAS + INDEX)

---

## Feldolgozási szabály

Következő Librarian futásnál csak azokat az üzeneteket kell olvasni,
amelyek **nem szerepelnek** ebben a listában. Egyszerű diff:

```bash
# Új, feldolgozatlan üzenetek keresése:
for f in docs/mailbox/*/archive/*.md docs/mailbox/*/outbox/*.md; do
  [ -f "$f" ] && grep -qF "$f" docs/mailbox/librarian/PROCESSED_LOG.md || echo "ÚJ: $f"
done
```

---

## abstractions (13 fájl)

- `docs/mailbox/abstractions/archive/2026-04-09_044_abstractions-phase-a-core-done.md`
- `docs/mailbox/abstractions/archive/2026-04-09_044_abstractions-phase-a-core.md`
- `docs/mailbox/abstractions/archive/2026-04-09_045_abstractions-phase-b-manufacturing-done.md`
- `docs/mailbox/abstractions/archive/2026-04-09_045_abstractions-phase-b-manufacturing.md`
- `docs/mailbox/abstractions/archive/2026-04-10_051_joinery-v2-track-c-e-done.md`
- `docs/mailbox/abstractions/archive/2026-04-10_051_joinery-v2-track-c-e.md`
- `docs/mailbox/abstractions/archive/2026-04-10_053_glass-componenttype-done.md`
- `docs/mailbox/abstractions/archive/2026-04-10_053_glass-componenttype.md`
- `docs/mailbox/abstractions/outbox/2026-04-15_001_modules-contracts-done.md`
- `docs/mailbox/abstractions/outbox/2026-04-15_002_dispatcher-pong.md`
- `docs/mailbox/abstractions/outbox/2026-04-15_003_sprint5-test-coverage-done.md`
- `docs/mailbox/abstractions/outbox/2026-04-15_004_security-review-done.md`
- `docs/mailbox/abstractions/outbox/2026-04-15_005_security-fixes-done.md`

## cutting (15 fájl)

- `docs/mailbox/cutting/outbox/2026-04-15_001_inventory-core-done.md`
- `docs/mailbox/cutting/outbox/2026-04-15_002_cutting-core-done.md`
- `docs/mailbox/cutting/outbox/2026-04-15_003_procurement-core-done.md`
- `docs/mailbox/cutting/outbox/2026-04-16_004_nesting-l1-done.md`
- `docs/mailbox/cutting/outbox/2026-04-16_005_panel-dimensions-fix-done.md`
- `docs/mailbox/cutting/outbox/2026-04-16_006_repo-split-done.md`
- `docs/mailbox/cutting/outbox/2026-04-16_007_tenant-claim-fix-done.md`
- `docs/mailbox/cutting/outbox/2026-04-16_008_tenant-session-interceptor-done.md`
- `docs/mailbox/cutting/outbox/2026-04-16_009_inventory-provider-stub-done.md`
- `docs/mailbox/cutting/outbox/2026-04-16_010_inventory-http-adapter-done.md`
- `docs/mailbox/cutting/outbox/2026-04-16_011_internal-delete-by-tenant-done.md`
- `docs/mailbox/cutting/outbox/2026-04-17_012_internal-delete-guc-fix-done.md`
- `docs/mailbox/cutting/outbox/2026-04-17_013_interceptor-skip-fix-done.md`
- `docs/mailbox/cutting/outbox/2026-04-17_014_closing-reset-fix-done.md`
- `docs/mailbox/cutting/outbox/2026-04-17_015_open-connection-fix-done.md`

## e2e (49 fájl)

- `docs/mailbox/e2e/archive/2026-04-11_002_full-rerun-superseded.md`
- `docs/mailbox/e2e/archive/2026-04-12_001_jwt-chain-fix-done-v3.md`
- `docs/mailbox/e2e/archive/2026-04-12_002_coverage-gap-analysis.md`
- `docs/mailbox/e2e/archive/2026-04-12_003_full-rerun-after-infra-fix.md`
- `docs/mailbox/e2e/archive/2026-04-12_003_full-rerun-done.md`
- `docs/mailbox/e2e/archive/2026-04-12_004_keycloak-auth-test-fix-done.md`
- `docs/mailbox/e2e/archive/2026-04-12_004_keycloak-auth-test-fix.md`
- `docs/mailbox/e2e/archive/2026-04-12_005_expansion-k1-done.md`
- `docs/mailbox/e2e/archive/2026-04-12_006_rerun-batch0-done.md`
- `docs/mailbox/e2e/archive/2026-04-13_007_batch2-done.md`
- `docs/mailbox/e2e/archive/2026-04-13_008_final-rerun-done.md`
- `docs/mailbox/e2e/outbox/2026-04-13_009_final-rerun-done.md`
- `docs/mailbox/e2e/outbox/2026-04-13_010_rerun-3645480-done.md`
- `docs/mailbox/e2e/outbox/2026-04-13_011_coverage-gap-report.md`
- `docs/mailbox/e2e/outbox/2026-04-13_012_rerun-d6b1bad-done.md`
- `docs/mailbox/e2e/outbox/2026-04-13_013_rerun-b270ccf-done.md`
- `docs/mailbox/e2e/outbox/2026-04-14_014_36-proof-done.md`
- `docs/mailbox/e2e/outbox/2026-04-14_015_rerun-plus-37-tools-done.md`
- `docs/mailbox/e2e/outbox/2026-04-14_016_34-abstractions-deep-done.md`
- `docs/mailbox/e2e/outbox/2026-04-14_017_05close-jwt-diagnosis-done.md`
- `docs/mailbox/e2e/outbox/2026-04-14_018_full-rerun-post-fixes-done.md`
- `docs/mailbox/e2e/outbox/2026-04-14_019_batch0-cleanup-e2e-fixes-done.md`
- `docs/mailbox/e2e/outbox/2026-04-14_020_24-summary-fixture-fix-done.md`
- `docs/mailbox/e2e/outbox/2026-04-14_021_full-rerun-post-portal-kernel-done.md`
- `docs/mailbox/e2e/outbox/2026-04-15_024_workflow-e2e-commands-suggestion.md`
- `docs/mailbox/e2e/outbox/2026-04-15_025_dispatcher-pong.md`
- `docs/mailbox/e2e/outbox/2026-04-15_026_sprint5-test-coverage-done.md`
- `docs/mailbox/e2e/outbox/2026-04-15_027_sprint5-run-done.md`
- `docs/mailbox/e2e/outbox/2026-04-15_028_security-review-done.md`
- `docs/mailbox/e2e/outbox/2026-04-15_029_security-fixes-rerun-blocked.md`
- `docs/mailbox/e2e/outbox/2026-04-15_030_fix-05close-502-done.md`
- `docs/mailbox/e2e/outbox/2026-04-15_031_cutting-smoke-blocked.md`
- `docs/mailbox/e2e/outbox/2026-04-15_032_cutting-smoke-done.md`
- `docs/mailbox/e2e/outbox/2026-04-16_033_cutting-full-coverage-done.md`
- `docs/mailbox/e2e/outbox/2026-04-16_034_rerun-post-cleanup-done.md`
- `docs/mailbox/e2e/outbox/2026-04-16_035_rerun-rls-cutting-activation-done.md`
- `docs/mailbox/e2e/outbox/2026-04-16_036_rerun-rls-proper-verification-blocked.md`
- `docs/mailbox/e2e/outbox/2026-04-16_037_rls-e2e-seed-fix-blocked.md`
- `docs/mailbox/e2e/outbox/2026-04-16_038_rerun-post-mapclaims-fix-done.md`
- `docs/mailbox/e2e/outbox/2026-04-16_039_rerun-cutting-flow-activation-done.md`
- `docs/mailbox/e2e/outbox/2026-04-16_040_rerun-cutting-flow-full-done.md`
- `docs/mailbox/e2e/outbox/2026-04-16_041_rerun-nesting-activation-done.md`
- `docs/mailbox/e2e/outbox/2026-04-16_042_rerun-nesting-full-done.md`
- `docs/mailbox/e2e/outbox/2026-04-16_043_rerun-nesting-activation-done.md`
- `docs/mailbox/e2e/outbox/2026-04-16_044_rerun-nesting-guc-fix-done.md`
- `docs/mailbox/e2e/outbox/2026-04-16_045_rerun-nesting-final-done.md`
- `docs/mailbox/e2e/outbox/2026-04-16_046_rerun-nesting-activation-final-done.md`
- `docs/mailbox/e2e/outbox/2026-04-17_047_full-rerun-post-sprint2-done.md`
- `docs/mailbox/e2e/outbox/2026-04-17_048_sprint3-smoke-test-done.md`

## fe (11 fájl)

- `docs/mailbox/fe/outbox/2026-04-16_001_scaffold-react-app-done.md`
- `docs/mailbox/fe/outbox/2026-04-16_002_door-order-dashboard-done.md`
- `docs/mailbox/fe/outbox/2026-04-16_003_order-completion-flow-done.md`
- `docs/mailbox/fe/outbox/2026-04-16_004_new-order-create-done.md`
- `docs/mailbox/fe/outbox/2026-04-16_005_order-status-timeline-filter-done.md`
- `docs/mailbox/fe/outbox/2026-04-16_006_production-readiness-done.md`
- `docs/mailbox/fe/outbox/2026-04-16_007_contract-tests-playwright-scaffold-done.md`
- `docs/mailbox/fe/outbox/2026-04-16_008_e2e-auth-flows-done.md`
- `docs/mailbox/fe/outbox/2026-04-17_009_playwright-e2e-infra-done.md`
- `docs/mailbox/fe/outbox/2026-04-17_010_e2e-seed-flows-done.md`
- `docs/mailbox/fe/outbox/2026-04-17_011_resettenant-fix-and-flow07-done.md`

## infra (97 fájl)

- `docs/mailbox/infra/archive/2026-04-09_049_keycloak-vps-setup.md`
- `docs/mailbox/infra/archive/2026-04-10_049_keycloak-vps-setup-done.md`
- `docs/mailbox/infra/archive/2026-04-10_056_keycloak-hostname-fix.md`
- `docs/mailbox/infra/archive/2026-04-11_056_keycloak-hostname-fix-done.md`
- `docs/mailbox/infra/archive/2026-04-11_057_keycloak-test-users.md`
- `docs/mailbox/infra/archive/2026-04-12_058_token-lifespan-done.md`
- `docs/mailbox/infra/archive/2026-04-12_059_migration-0028-and-env-blocked.md`
- `docs/mailbox/infra/archive/2026-04-12_059_migration-0028-and-env.md`
- `docs/mailbox/infra/archive/2026-04-12_060_migration-bypass-done.md`
- `docs/mailbox/infra/archive/2026-04-12_060_script-mapper-be01-fix-done.md`
- `docs/mailbox/infra/archive/2026-04-12_061_joinery-abstractions-vps-deploy-done.md`
- `docs/mailbox/infra/archive/2026-04-12_062_kernel-deploy-post-k060-done.md`
- `docs/mailbox/infra/archive/2026-04-13_063_kernel-deploy-done.md`
- `docs/mailbox/infra/outbox/2026-04-13_064_kernel-rollback-done.md`
- `docs/mailbox/infra/outbox/2026-04-13_065_kernel-deploy-316f603-done.md`
- `docs/mailbox/infra/outbox/2026-04-13_066_kernel-rollback2-done.md`
- `docs/mailbox/infra/outbox/2026-04-13_067_kernel-deploy-3645480-done.md`
- `docs/mailbox/infra/outbox/2026-04-13_068_kernel-rollback3-done.md`
- `docs/mailbox/infra/outbox/2026-04-13_069_kernel-deploy-d6b1bad-done.md`
- `docs/mailbox/infra/outbox/2026-04-13_070_kernel-deploy-b270ccf-done.md`
- `docs/mailbox/infra/outbox/2026-04-14_071_orch-deploy-b7b4581-done.md`
- `docs/mailbox/infra/outbox/2026-04-14_072_kernel-deploy-46d6352-done.md`
- `docs/mailbox/infra/outbox/2026-04-14_073_orch-deploy-ca00227-done.md`
- `docs/mailbox/infra/outbox/2026-04-14_074_kernel-46d6352-verify-done.md`
- `docs/mailbox/infra/outbox/2026-04-14_075_keycloak-tid-claim-mapper-done.md`
- `docs/mailbox/infra/outbox/2026-04-14_076_keycloak-kernel-api-tid-mapper-done.md`
- `docs/mailbox/infra/outbox/2026-04-14_077_keycloak-admin-ui-public-access-done.md`
- `docs/mailbox/infra/outbox/2026-04-14_078_portal-dist-stale-blocked.md`
- `docs/mailbox/infra/outbox/2026-04-14_079_kernel-deploy-37951c8-done.md`
- `docs/mailbox/infra/outbox/2026-04-14_080_orch-deploy-049c427-done.md`
- `docs/mailbox/infra/outbox/2026-04-14_081_portal-login-e2e-verified.md`
- `docs/mailbox/infra/outbox/2026-04-15_081_kernel-deploy-migration0029-blocked.md`
- `docs/mailbox/infra/outbox/2026-04-15_082_orch-deploy-f7ddb37-done.md`
- `docs/mailbox/infra/outbox/2026-04-15_083_kernel-deploy-migration0029-fix-done.md`
- `docs/mailbox/infra/outbox/2026-04-15_085_dispatcher-ping-pong.md`
- `docs/mailbox/infra/outbox/2026-04-15_086_orch-b3860ac-deploy-done.md`
- `docs/mailbox/infra/outbox/2026-04-15_087_kernel-migration0030-blocked.md`
- `docs/mailbox/infra/outbox/2026-04-15_088_kernel-migration0030-done.md`
- `docs/mailbox/infra/outbox/2026-04-15_089_audit-chain-genesis-hash-done.md`
- `docs/mailbox/infra/outbox/2026-04-15_090_portal-axios-cve-fix-done.md`
- `docs/mailbox/infra/outbox/2026-04-15_091_security-fixes-batch-blocked.md`
- `docs/mailbox/infra/outbox/2026-04-15_092_portal-dist-deploy-done.md`
- `docs/mailbox/infra/outbox/2026-04-15_093_sudoers-fix-health-check-done.md`
- `docs/mailbox/infra/outbox/2026-04-15_094_kernel-3dd0e31-deploy-done.md`
- `docs/mailbox/infra/outbox/2026-04-15_095_cutting-repo-setup-done.md`
- `docs/mailbox/infra/outbox/2026-04-15_096_genesis-hash-env-fix-done.md`
- `docs/mailbox/infra/outbox/2026-04-15_097_cutting-service-deploy-blocked.md`
- `docs/mailbox/infra/outbox/2026-04-15_098_cutting-service-operator-confirm-blocked.md`
- `docs/mailbox/infra/outbox/2026-04-15_099_cutting-services-confirmed-done.md`
- `docs/mailbox/infra/outbox/2026-04-15_100_orch-d825ab1-deploy-done.md`
- `docs/mailbox/infra/outbox/2026-04-16_101_joinery-cutting-stub-deploy-blocked.md`
- `docs/mailbox/infra/outbox/2026-04-16_102_cutting-modules-deploy-blocked.md`
- `docs/mailbox/infra/outbox/2026-04-16_103_cleanup-rename-deploy-blocked.md`
- `docs/mailbox/infra/outbox/2026-04-16_104_materialcatalog-seed-done.md`
- `docs/mailbox/infra/outbox/2026-04-16_105_kernel-a9d3803-deploy-done.md`
- `docs/mailbox/infra/outbox/2026-04-16_106_keycloak-test-admin-tid-fix-done.md`
- `docs/mailbox/infra/outbox/2026-04-16_107_kernel-694bc56-deploy-done.md`
- `docs/mailbox/infra/outbox/2026-04-16_108_orch-6566d2a-deploy-done.md`
- `docs/mailbox/infra/outbox/2026-04-16_109_cutting-audience-fix-done.md`
- `docs/mailbox/infra/outbox/2026-04-16_110_cutting-jwt-diagnosis-blocked.md`
- `docs/mailbox/infra/outbox/2026-04-16_111_cutting-modules-deploy-done.md`
- `docs/mailbox/infra/outbox/2026-04-16_112_cutting-db-schema-grant-done.md`
- `docs/mailbox/infra/outbox/2026-04-16_113_cutting-db-guc-fix-done.md`
- `docs/mailbox/infra/outbox/2026-04-16_114_cutting-interceptor-deploy-done.md`
- `docs/mailbox/infra/outbox/2026-04-16_115_cutting-stub-deploy-done.md`
- `docs/mailbox/infra/outbox/2026-04-16_116_kernel-e4f83ac-deploy-done.md`
- `docs/mailbox/infra/outbox/2026-04-16_117_cutting-http-adapter-deploy-done.md`
- `docs/mailbox/infra/outbox/2026-04-16_118_keycloak-portal-app-fe-dev-uris-done.md`
- `docs/mailbox/infra/outbox/2026-04-16_119_fe-repo-clone-done.md`
- `docs/mailbox/infra/outbox/2026-04-16_120_doorstar-portal-nginx-deploy-blocked.md`
- `docs/mailbox/infra/outbox/2026-04-16_121_doorstar-portal-cert-activate-blocked.md`
- `docs/mailbox/infra/outbox/2026-04-16_122_doorstar-portal-cert-dns-retry-blocked.md`
- `docs/mailbox/infra/outbox/2026-04-16_123_doorstar-portal-cert-now-done.md`
- `docs/mailbox/infra/outbox/2026-04-16_124_test-tenant-keycloak-setup-done.md`
- `docs/mailbox/infra/outbox/2026-04-16_125_test-tenant-kernel-db-seed-done.md`
- `docs/mailbox/infra/outbox/2026-04-16_126_test-env-vars-setup-done.md`
- `docs/mailbox/infra/outbox/2026-04-16_127_orchestrator-test-bff-deploy-blocked.md`
- `docs/mailbox/infra/outbox/2026-04-16_128_orchestrator-test-bff-deploy-retry-blocked.md`
- `docs/mailbox/infra/outbox/2026-04-16_129_orchestrator-test-bff-deploy-final-done.md`
- `docs/mailbox/infra/outbox/2026-04-16_130_bulk-module-deploy-internal-delete-blocked.md`
- `docs/mailbox/infra/outbox/2026-04-16_131_portal-csp-fix-done.md`
- `docs/mailbox/infra/outbox/2026-04-16_132_orchestrator-seed-profiles-deploy-done.md`
- `docs/mailbox/infra/outbox/2026-04-17_133_bulk-module-guc-fix-deploy-blocked.md`
- `docs/mailbox/infra/outbox/2026-04-17_134_bulk-module-interceptor-fix-deploy-blocked.md`
- `docs/mailbox/infra/outbox/2026-04-17_135_final-module-deploy-blocked.md`
- `docs/mailbox/infra/outbox/2026-04-17_135_final-module-deploy-done.md`
- `docs/mailbox/infra/outbox/2026-04-17_136_outbox-force-rls-fix-done.md`
- `docs/mailbox/infra/outbox/2026-04-17_137_final-openconn-deploy-and-verify-done.md`
- `docs/mailbox/infra/outbox/2026-04-17_138_portal-csp-auth-fix-done.md`
- `docs/mailbox/infra/outbox/2026-04-17_139_orch-077-deploy-blocked.md`
- `docs/mailbox/infra/outbox/2026-04-17_140_procurement-deploy-done.md`
- `docs/mailbox/infra/outbox/2026-04-17_141_orch-078-deploy-and-seed-verify-done.md`
- `docs/mailbox/infra/outbox/2026-04-17_142_orch-079-deploy-and-seed-verify-done.md`
- `docs/mailbox/infra/outbox/2026-04-17_143_kc-portal-app-localhost-redirect-done.md`
- `docs/mailbox/infra/outbox/2026-04-17_144_orch-080-redeploy-idempotency-done.md`
- `docs/mailbox/infra/outbox/2026-04-17_145_portal-sprint3-dist-deploy-blocked.md`
- `docs/mailbox/infra/outbox/2026-04-17_147_portal-sprint3-dist-deploy-done.md`

## inventory (5 fájl)

- `docs/mailbox/inventory/outbox/2026-04-16_001_internal-delete-by-tenant-done.md`
- `docs/mailbox/inventory/outbox/2026-04-16_002_internal-delete-guc-fix-done.md`
- `docs/mailbox/inventory/outbox/2026-04-17_003_interceptor-skip-fix-done.md`
- `docs/mailbox/inventory/outbox/2026-04-17_004_closing-reset-fix-done.md`
- `docs/mailbox/inventory/outbox/2026-04-17_005_open-connection-fix-done.md`

## joinery (17 fájl)

- `docs/mailbox/joinery/archive/2026-04-09_038_joinery-improvements.md`
- `docs/mailbox/joinery/archive/2026-04-09_042_joinery-dod-completion.md`
- `docs/mailbox/joinery/archive/2026-04-10_050_joinery-v2-kickoff-done.md`
- `docs/mailbox/joinery/archive/2026-04-10_050_joinery-v2-kickoff.md`
- `docs/mailbox/joinery/outbox/2026-04-15_001_dispatcher-pong.md`
- `docs/mailbox/joinery/outbox/2026-04-15_002_sprint5-test-coverage-done.md`
- `docs/mailbox/joinery/outbox/2026-04-15_003_security-review-done.md`
- `docs/mailbox/joinery/outbox/2026-04-15_004_security-fixes-done.md`
- `docs/mailbox/joinery/outbox/2026-04-15_005_cutting-contracts-blocked.md`
- `docs/mailbox/joinery/outbox/2026-04-15_006_cutting-contracts-done.md`
- `docs/mailbox/joinery/outbox/2026-04-16_007_cutting-provider-integration-done.md`
- `docs/mailbox/joinery/outbox/2026-04-16_007_saga-500-diagnosis-done.md`
- `docs/mailbox/joinery/outbox/2026-04-16_008_internal-delete-by-tenant-done.md`
- `docs/mailbox/joinery/outbox/2026-04-16_009_internal-delete-guc-fix-done.md`
- `docs/mailbox/joinery/outbox/2026-04-17_012_interceptor-skip-fix-done.md`
- `docs/mailbox/joinery/outbox/2026-04-17_013_closing-reset-fix-done.md`
- `docs/mailbox/joinery/outbox/2026-04-17_014_open-connection-fix-done.md`

## kernel (60 fájl)

- `docs/mailbox/kernel/archive/2026-04-06_007_sprintD-phase1_5-done.md`
- `docs/mailbox/kernel/archive/2026-04-06_021_sprintD-phase1_5-kernel.md`
- `docs/mailbox/kernel/archive/2026-04-07_008_sprintD-phase2-done.md`
- `docs/mailbox/kernel/archive/2026-04-07_009_sprintD-phase2-detailed-report.md`
- `docs/mailbox/kernel/archive/2026-04-07_010_phase3a-review-response.md`
- `docs/mailbox/kernel/archive/2026-04-07_011_phase3a-minorfixes-response.md`
- `docs/mailbox/kernel/archive/2026-04-07_012_phase3b-done.md`
- `docs/mailbox/kernel/archive/2026-04-07_022_sprintD-phase2-done.md`
- `docs/mailbox/kernel/archive/2026-04-07_022_sprintD-phase2-kernel.md`
- `docs/mailbox/kernel/archive/2026-04-07_023_phase2-details-request.md`
- `docs/mailbox/kernel/archive/2026-04-07_024_phase3a-spatial-bim.md`
- `docs/mailbox/kernel/archive/2026-04-07_025_phase3a-review-security.md`
- `docs/mailbox/kernel/archive/2026-04-07_026_phase3b-escrow-ga-foundation.md`
- `docs/mailbox/kernel/archive/2026-04-07_027_phase3a-minor-fixes.md`
- `docs/mailbox/kernel/archive/2026-04-07_028_phase3c-migration-0024.md`
- `docs/mailbox/kernel/archive/2026-04-07_029_phase3c-migration-0024-done.md`
- `docs/mailbox/kernel/archive/2026-04-08_013_phase3cplus-kernel.md`
- `docs/mailbox/kernel/archive/2026-04-08_030_phase3cplus-migration-0025-0026-done.md`
- `docs/mailbox/kernel/archive/2026-04-08_031_prod-readiness-kernel-done.md`
- `docs/mailbox/kernel/archive/2026-04-08_031_prod-readiness-kernel.md`
- `docs/mailbox/kernel/archive/2026-04-09_032_joinery-domain-done.md`
- `docs/mailbox/kernel/archive/2026-04-09_033_joinery-application-done.md`
- `docs/mailbox/kernel/archive/2026-04-09_034_joinery-infrastructure-done.md`
- `docs/mailbox/kernel/archive/2026-04-09_035_joinery-api-tests-done.md`
- `docs/mailbox/kernel/archive/2026-04-09_036_joinery-repository-done.md`
- `docs/mailbox/kernel/archive/2026-04-09_036_joinery-repository-impl.md`
- `docs/mailbox/kernel/archive/2026-04-09_046_kernel-keycloak-idp-done.md`
- `docs/mailbox/kernel/archive/2026-04-09_046_kernel-keycloak-idp.md`
- `docs/mailbox/kernel/archive/2026-04-10_054_stage-registry-done.md`
- `docs/mailbox/kernel/archive/2026-04-10_054_stage-registry.md`
- `docs/mailbox/kernel/archive/2026-04-10_057_stage-registry-tests-required.md`
- `docs/mailbox/kernel/archive/2026-04-11_058_flowepic-500-and-authority-fix.md`
- `docs/mailbox/kernel/archive/2026-04-11_058_flowepic-500-authority-fix-done.md`
- `docs/mailbox/kernel/archive/2026-04-12_059_toolendpoints-gettenantid-findall-done.md`
- `docs/mailbox/kernel/archive/2026-04-12_059_toolendpoints-gettenantid-fix.md`
- `docs/mailbox/kernel/archive/2026-04-12_060_migration-regen-ratelimit-done.md`
- `docs/mailbox/kernel/archive/2026-04-12_061_migration-reconcile-done.md`
- `docs/mailbox/kernel/archive/2026-04-13_062_e2e-remaining-failures-done.md`
- `docs/mailbox/kernel/outbox/2026-04-13_063_claimsresolver-fallback-done.md`
- `docs/mailbox/kernel/outbox/2026-04-13_064_tenancysession-debug-done.md`
- `docs/mailbox/kernel/outbox/2026-04-13_065_tenantsession-revert-done.md`
- `docs/mailbox/kernel/outbox/2026-04-13_066_full-diff-diagnosis-done.md`
- `docs/mailbox/kernel/outbox/2026-04-14_067_flowepic-close-fsm-fix-done.md`
- `docs/mailbox/kernel/outbox/2026-04-14_068_05close-e2e-diagnosis-blocked.md`
- `docs/mailbox/kernel/outbox/2026-04-14_069_15nodes-sync-fix-done.md`
- `docs/mailbox/kernel/outbox/2026-04-15_070_ecosystem-actor-arch-v4-done.md`
- `docs/mailbox/kernel/outbox/2026-04-15_071_migration0029-designer-fix-done.md`
- `docs/mailbox/kernel/outbox/2026-04-15_073_dispatcher-pong.md`
- `docs/mailbox/kernel/outbox/2026-04-15_074_sprint5-test-coverage-done.md`
- `docs/mailbox/kernel/outbox/2026-04-15_075_audit-event-sequence-done.md`
- `docs/mailbox/kernel/outbox/2026-04-15_076_migration0030-commit-push-done.md`
- `docs/mailbox/kernel/outbox/2026-04-15_077_audit-chain-investigation-done.md`
- `docs/mailbox/kernel/outbox/2026-04-15_078_audit-chain-prelaunch-cleanup-done.md`
- `docs/mailbox/kernel/outbox/2026-04-15_079_security-review-done.md`
- `docs/mailbox/kernel/outbox/2026-04-15_080_healthz-allow-anonymous-done.md`
- `docs/mailbox/kernel/outbox/2026-04-16_081_rls-tenant-isolation-fix-done.md`
- `docs/mailbox/kernel/outbox/2026-04-16_082_facility-get-404-mapclaims-fix-done.md`
- `docs/mailbox/kernel/outbox/2026-04-16_083_llm-tool-registry-done.md`
- `docs/mailbox/kernel/outbox/2026-04-16_084_duplicate-using-fix-done.md`
- `docs/mailbox/kernel/outbox/2026-04-16_085_internal-delete-by-tenant-done.md`

## orchestrator (54 fájl)

- `docs/mailbox/orchestrator/archive/2026-04-06_007_sprintD-phase1_5-orchestrator.md`
- `docs/mailbox/orchestrator/archive/2026-04-07_007_sprintD-phase1_5-done.md`
- `docs/mailbox/orchestrator/archive/2026-04-07_008_sprintD-phase2-done.md`
- `docs/mailbox/orchestrator/archive/2026-04-07_008_sprintD-phase2-orchestrator.md`
- `docs/mailbox/orchestrator/archive/2026-04-07_009_phase3a-spatial-routes-done.md`
- `docs/mailbox/orchestrator/archive/2026-04-07_009_phase3a-spatial-routes.md`
- `docs/mailbox/orchestrator/archive/2026-04-07_010_phase3b-bff-routes.md`
- `docs/mailbox/orchestrator/archive/2026-04-07_010_phase3b-done.md`
- `docs/mailbox/orchestrator/archive/2026-04-07_011_phase3c-brand-skin-done.md`
- `docs/mailbox/orchestrator/archive/2026-04-07_011_phase3c-brand-skin-response.md`
- `docs/mailbox/orchestrator/archive/2026-04-08_012_dist-rebuild-done.md`
- `docs/mailbox/orchestrator/archive/2026-04-08_012_dist-rebuild.md`
- `docs/mailbox/orchestrator/archive/2026-04-08_013_dist-rebuild-handshakes-done.md`
- `docs/mailbox/orchestrator/archive/2026-04-08_013_phase3cplus-orchestrator.md`
- `docs/mailbox/orchestrator/archive/2026-04-08_014_auth-response-enabledmodules-done.md`
- `docs/mailbox/orchestrator/archive/2026-04-08_015_prod-readiness-orchestrator-done.md`
- `docs/mailbox/orchestrator/archive/2026-04-08_015_prod-readiness-orchestrator.md`
- `docs/mailbox/orchestrator/archive/2026-04-09_036_doorstar-orchestrator-done.md`
- `docs/mailbox/orchestrator/archive/2026-04-09_036_doorstar-orchestrator-mediation.md`
- `docs/mailbox/orchestrator/archive/2026-04-09_043_joinery-route-tests-done.md`
- `docs/mailbox/orchestrator/archive/2026-04-09_043_joinery-route-tests.md`
- `docs/mailbox/orchestrator/archive/2026-04-09_047_orchestrator-keycloak-idp-done.md`
- `docs/mailbox/orchestrator/archive/2026-04-09_047_orchestrator-keycloak-idp.md`
- `docs/mailbox/orchestrator/archive/2026-04-10_052_joinery-v2-track-c-orc-done.md`
- `docs/mailbox/orchestrator/archive/2026-04-10_052_joinery-v2-track-c-orc.md`
- `docs/mailbox/orchestrator/archive/2026-04-10_055_stage-dispatch-blocked.md`
- `docs/mailbox/orchestrator/archive/2026-04-10_055_stage-dispatch-done.md`
- `docs/mailbox/orchestrator/archive/2026-04-10_055_stage-dispatch.md`
- `docs/mailbox/orchestrator/archive/2026-04-11_056_authme-tenantid-and-config-commit.md`
- `docs/mailbox/orchestrator/archive/2026-04-11_056_authme-tenantid-config-done.md`
- `docs/mailbox/orchestrator/archive/2026-04-11_057_vps-deploy-done.md`
- `docs/mailbox/orchestrator/archive/2026-04-11_057_vps-deploy.md`
- `docs/mailbox/orchestrator/archive/2026-04-12_058_ratelimit-middleware-order-done.md`
- `docs/mailbox/orchestrator/archive/2026-04-12_059_abstractions-proxy-done.md`
- `docs/mailbox/orchestrator/archive/2026-04-13_059_abstractions-proxy-done.md`
- `docs/mailbox/orchestrator/outbox/2026-04-14_060_proof-route-path-fix-done.md`
- `docs/mailbox/orchestrator/outbox/2026-04-14_061_proof-content-type-normalize-done.md`
- `docs/mailbox/orchestrator/outbox/2026-04-14_062_npm-audit-fix-done.md`
- `docs/mailbox/orchestrator/outbox/2026-04-15_063_ecosystem-actor-bff-routes-done.md`
- `docs/mailbox/orchestrator/outbox/2026-04-15_065_dispatcher-pong.md`
- `docs/mailbox/orchestrator/outbox/2026-04-15_067_sprint5-test-coverage-done.md`
- `docs/mailbox/orchestrator/outbox/2026-04-15_068_security-review-done.md`
- `docs/mailbox/orchestrator/outbox/2026-04-15_069_security-fixes-done.md`
- `docs/mailbox/orchestrator/outbox/2026-04-15_070_cutting-bff-routes-done.md`
- `docs/mailbox/orchestrator/outbox/2026-04-15_071_cutting-bff-post-fix-done.md`
- `docs/mailbox/orchestrator/outbox/2026-04-16_072_cutting-pathrewrite-fix-done.md`
- `docs/mailbox/orchestrator/outbox/2026-04-16_073_commit-missing-done.md`
- `docs/mailbox/orchestrator/outbox/2026-04-16_074_test-bff-endpoint-done.md`
- `docs/mailbox/orchestrator/outbox/2026-04-16_075_push-orch-074-commit-done.md`
- `docs/mailbox/orchestrator/outbox/2026-04-16_076_seed-profiles-done.md`
- `docs/mailbox/orchestrator/outbox/2026-04-17_077_doorstar-cutting-ready-seed-done.md`
- `docs/mailbox/orchestrator/outbox/2026-04-17_078_seed-facility-flowepic-fix-done.md`
- `docs/mailbox/orchestrator/outbox/2026-04-17_079_seed-add-door-item-done.md`
- `docs/mailbox/orchestrator/outbox/2026-04-17_080_commit-push-facility-idempotency-done.md`

## portal (28 fájl)

- `docs/mailbox/portal/archive/2026-04-06_014_sprintD-phase1_5-done.md`
- `docs/mailbox/portal/archive/2026-04-06_014_sprintD-phase1_5-portal.md`
- `docs/mailbox/portal/archive/2026-04-07_015_sprintD-phase2-done.md`
- `docs/mailbox/portal/archive/2026-04-07_015_sprintD-phase2-portal.md`
- `docs/mailbox/portal/archive/2026-04-07_016_phase3a-spatial-awareness-done.md`
- `docs/mailbox/portal/archive/2026-04-07_016_phase3a-spatial-awareness.md`
- `docs/mailbox/portal/archive/2026-04-07_017_phase3b-snapshot-ui-done.md`
- `docs/mailbox/portal/archive/2026-04-07_017_phase3b-snapshot-ui.md`
- `docs/mailbox/portal/archive/2026-04-07_018_sync-types-done.md`
- `docs/mailbox/portal/archive/2026-04-07_018_sync-types.md`
- `docs/mailbox/portal/archive/2026-04-07_019_phase3c-turborepo-brand-done.md`
- `docs/mailbox/portal/archive/2026-04-07_019_phase3c-turborepo-brand.md`
- `docs/mailbox/portal/archive/2026-04-08_020_phase3cplus-portal-done.md`
- `docs/mailbox/portal/archive/2026-04-08_020_phase3cplus-portal.md`
- `docs/mailbox/portal/archive/2026-04-08_021_day11-cabinetorders-live-done.md`
- `docs/mailbox/portal/archive/2026-04-08_022_prod-readiness-portal-done.md`
- `docs/mailbox/portal/archive/2026-04-08_022_prod-readiness-portal.md`
- `docs/mailbox/portal/archive/2026-04-09_048_portal-keycloak-idp-done.md`
- `docs/mailbox/portal/archive/2026-04-09_048_portal-keycloak-idp.md`
- `docs/mailbox/portal/outbox/2026-04-14_001_portal-rebuild-deploy-done.md`
- `docs/mailbox/portal/outbox/2026-04-14_002_login-route-fix-done.md`
- `docs/mailbox/portal/outbox/2026-04-15_003_dispatcher-pong.md`
- `docs/mailbox/portal/outbox/2026-04-15_004_sprint5-test-coverage-done.md`
- `docs/mailbox/portal/outbox/2026-04-15_005_security-review-done.md`
- `docs/mailbox/portal/outbox/2026-04-15_006_dist-rebuild-done.md`
- `docs/mailbox/portal/outbox/2026-04-16_007_vite-cve-blocked.md`
- `docs/mailbox/portal/outbox/2026-04-17_009_sprint3-doorstar-ui-done.md`
- `docs/mailbox/portal/outbox/2026-04-17_010_push-sprint3-done.md`

## procurement (6 fájl)

- `docs/mailbox/procurement/outbox/2026-04-16_001_internal-delete-by-tenant-done.md`
- `docs/mailbox/procurement/outbox/2026-04-16_002_internal-delete-guc-fix-done.md`
- `docs/mailbox/procurement/outbox/2026-04-17_003_interceptor-skip-fix-done.md`
- `docs/mailbox/procurement/outbox/2026-04-17_004_closing-reset-fix-done.md`
- `docs/mailbox/procurement/outbox/2026-04-17_005_open-connection-fix-done.md`
- `docs/mailbox/procurement/outbox/2026-04-17_006_supplier-create-endpoint-done.md`

---

## LIB-002 — 2026-04-20 — Új feldolgozott fájlok

### cutting (12 fájl)

- `docs/mailbox/cutting/outbox/2026-04-18_016_vagotervek-500-fix-done.md`
- `docs/mailbox/cutting/outbox/2026-04-18_017_bug004-reteszt-regresszio-done.md`
- `docs/mailbox/cutting/outbox/2026-04-18_018_bug004-post-500-deep-done.md`
- `docs/mailbox/cutting/outbox/2026-04-18_019_bug008-cutting-plans-get-500-done.md`
- `docs/mailbox/cutting/outbox/2026-04-19_020_nesting-endpoint-404-diagnosis-answer.md`
- `docs/mailbox/cutting/outbox/2026-04-19_021_cutting-planning-v1-phase1-done.md`
- `docs/mailbox/cutting/outbox/2026-04-19_022_cutting-planning-v2-strategy-done.md`
- `docs/mailbox/cutting/outbox/2026-04-20_023_cutting-028-event-bus-done.md`
- `docs/mailbox/cutting/outbox/2026-04-20_024_cutting-029-nesting-nuget-e1-e4-done.md`
- `docs/mailbox/cutting/outbox/2026-04-20_025_cutting-030-nesting-e5-done.md`
- `docs/mailbox/cutting/outbox/2026-04-20_026_cutting-031-plan-status-enum-done.md`
- `docs/mailbox/cutting/outbox/2026-04-20_027_cutting-033-migration-fix-done.md`

### kernel (12 fájl)

- `docs/mailbox/kernel/outbox/2026-04-18_089_tenant-create-500-regression-done.md`
- `docs/mailbox/kernel/outbox/2026-04-18_090_tenant-create-500-fix-done.md`
- `docs/mailbox/kernel/outbox/2026-04-18_091_retry-strategy-fix-done.md`
- `docs/mailbox/kernel/outbox/2026-04-18_093_retry-strategy-root-fix-done.md`
- `docs/mailbox/kernel/outbox/2026-04-18_094_scope-clarification-blocked.md`
- `docs/mailbox/kernel/outbox/2026-04-19_095_architecture-decision-answer.md`
- `docs/mailbox/kernel/outbox/2026-04-19_096_kernel093-not-deployed-urgent-infra.md`
- `docs/mailbox/kernel/outbox/2026-04-19_097_auth-investigation-answer.md`
- `docs/mailbox/kernel/outbox/2026-04-19_098_deployment-verification-and-enableretryonfailure-check-answer.md`
- `docs/mailbox/kernel/outbox/2026-04-19_099_urgent-bug-003b-007-still-500-investigation-answer.md`
- `docs/mailbox/kernel/outbox/2026-04-19_100_rebuild-46d64b5-fresh-binaries-critical-done.md`
- `docs/mailbox/kernel/outbox/2026-04-19_101_kernel-ready-awaiting-tester-validation.md`
- `docs/mailbox/kernel/outbox/2026-04-19_102_urgent-post-500-investigation-answer.md`

### e2e (6 fájl)

- `docs/mailbox/e2e/outbox/2026-04-18_050_soft-launch-bug-coverage-done.md`
- `docs/mailbox/e2e/outbox/2026-04-18_051_full-rerun-post-bugfix-done.md`
- `docs/mailbox/e2e/outbox/2026-04-18_052_full-rerun-kernel-unblocked-done.md`
- `docs/mailbox/e2e/outbox/2026-04-18_053_coverage-expansion-done.md`
- `docs/mailbox/e2e/outbox/2026-04-19_054_kernel093-rerun-blocked.md`
- `docs/mailbox/e2e/outbox/2026-04-19_054_kernel093-rerun-done.md`

### orchestrator (1 fájl)

- `docs/mailbox/orchestrator/outbox/2026-04-18_082_chat-422-fix-done.md`

### portal (12 fájl)

- `docs/mailbox/portal/outbox/2026-04-18_001_suppliers-bug-fix-done.md`
- `docs/mailbox/portal/outbox/2026-04-18_002_chat-ui-fix-done.md`
- `docs/mailbox/portal/outbox/2026-04-18_003_chat-messages-payload-fix-done.md`
- `docs/mailbox/portal/outbox/2026-04-18_004_bug009-orders-error-handling-done.md`
- `docs/mailbox/portal/outbox/2026-04-18_005_bug010-logout-client-id-fix-done.md`
- `docs/mailbox/portal/outbox/2026-04-18_006_bug012-016-ui-polish-done.md`
- `docs/mailbox/portal/outbox/2026-04-18_007_bug016-logout-parameter-done.md`
- `docs/mailbox/portal/outbox/2026-04-19_008_bug015-browser-back-done.md`
- `docs/mailbox/portal/outbox/2026-04-19_009_logout-parameter-revert-done.md`
- `docs/mailbox/portal/outbox/2026-04-19_010_nesting-panel-ux-done.md`
- `docs/mailbox/portal/outbox/2026-04-19_011_bug013-mobile-sidebar-done.md`
- `docs/mailbox/portal/outbox/2026-04-19_012_inventory-ui-refactor-done.md`

### joinery archive (4 fájl)

- `docs/mailbox/joinery/archive/2026-04-17_015_pdf-gyartasilap-export-done.md`
- `docs/mailbox/joinery/archive/2026-04-17_015_pdf-gyartasilap-export.md`
- `docs/mailbox/joinery/archive/2026-04-17_016_hardverlista-anyagnorma-pdf-done.md`
- `docs/mailbox/joinery/archive/2026-04-17_016_hardverlista-anyagnorma-pdf.md`

