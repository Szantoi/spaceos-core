---
name: MSG-KC01 Keycloak IdP Integration Status
description: MSG-KC01 KC-T1..T4 Keycloak OIDC authority-based JWT, TenantSessionInterceptor spaceos_tenants claim, JwksHealthCheck, old auth code removal — 913 tests passing (2026-04-09)
type: project
---

MSG-KC01 CLOSED_DONE — all 4 tasks complete, 913 tests passing.

**Why:** Replace local ES256 JWT issuance with Keycloak OIDC authority-based JWKS validation.

**How to apply:** T5 (tests for TenantSessionInterceptor spaceos_tenants claim logic) is a separate task.

Tasks:
- KC-T1: Program.cs replaced ISigningKeyProvider+ConfigureJwtBearerOptions with AddJwtBearer(Authority/Audience). appsettings.json/Development.json updated.
- KC-T2: TenantSessionInterceptor updated to parse spaceos_tenants JSON claim (with X-SpaceOS-Active-Tenant header support). TenantClaimDto added to Application/DTOs/.
- KC-T3: JwksHealthCheck added to Infrastructure/Health/. Registered as "jwks"/"ready" tag. /health/ready endpoint added to Program.cs.
- KC-T4: Deleted ConfigureJwtBearerOptions.cs, ISigningKeyProvider.cs, LocalEcKeyProvider.cs, JwtAccessTokenIssuer.cs, IJwtAccessTokenIssuer.cs, TokenPairDto.cs, RefreshTokenCommand/Handler/Validator, RefreshTokenCommandHandlerTests.cs, RefreshTokenCommandValidatorTests.cs. AuthEndpoints.cs: removed refresh + JWKS endpoints, kept logout. ApiFactory.cs/OpenApiTests.cs: removed ISigningKeyProvider/IJwtAccessTokenIssuer stubs. AuthEndpointTests.cs: removed refresh/JWKS tests.
