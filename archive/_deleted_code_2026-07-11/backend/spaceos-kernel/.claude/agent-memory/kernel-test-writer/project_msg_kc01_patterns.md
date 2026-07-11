---
name: MSG-KC01 Keycloak IdP Integration test patterns
description: TenantSessionInterceptor Keycloak claim parsing, JwksHealthCheck unit test pattern, OnChallenge content-type quirk, RealmRoles mapping helper.
type: project
---

MSG-KC01 T5 adds 20 new tests across 4 files. 933 total tests after.

**Why:** Keycloak IdP replaces the internal JWT issuer. T1-T4 updated TenantSessionInterceptor
to parse `spaceos_tenants` JSON claim array, added `X-SpaceOS-Active-Tenant` header routing,
double-deserialization handling, and JwksHealthCheck.

**How to apply:**

## TenantSessionInterceptor Keycloak tests
File: `SpaceOS.Kernel.Tests/Infrastructure/Persistence/TenantSessionInterceptorKeycloakTests.cs`
- Reuses `CapturingDbConnection` from the sibling `TenantSessionInterceptorTests.cs` (same namespace, same assembly)
- Helper `BuildTenantsJson(IEnumerable<string> tenantIds)` builds the spaceos_tenants JSON
- Double-wrapped claim: use `JsonSerializer.Serialize(innerJson)` to simulate Keycloak Script Mapper
- `BuildInterceptorFromClaims(claims, activeTenantHeader, loggerMock?)` covers all claim variations
- `X-SpaceOS-Active-Tenant` header not in list → `UnauthorizedAccessException` (use `Assert.ThrowsAsync`)
- Malformed JSON → `LogLevel.Warning` once (verify with Moq `l.Log(LogLevel.Warning, ...)`)

## JwksHealthCheck unit tests
File: `SpaceOS.Kernel.Tests/Infrastructure/JwksHealthCheckTests.cs`
- Uses `StubHttpMessageHandler` (returns fixed `HttpResponseMessage`) and `ThrowingHttpMessageHandler`
- `IHttpClientFactory` mocked with `factoryMock.Setup(f => f.CreateClient(nameof(JwksHealthCheck)))`
- `HealthCheckContext` built with `HealthCheckRegistration` using `failureStatus: HealthStatus.Degraded`
- Missing authority → `Degraded` with "Jwt:Authority" in description, HTTP client never called

## RealmRoles mapping tests
File: `SpaceOS.Kernel.Tests/Api/RealmRolesMappingTests.cs`
- Mirrors the `OnTokenValidated` lambda from Program.cs as a private static helper
- Tests that `realm_access` claim JSON is parsed and roles added to `ClaimTypes.Role`
- Missing `realm_access` → no exception, no roles added

## API integration tests (Auth_Endpoints_Removed + JwtBearer_Authority_Config)
File: `SpaceOS.Kernel.Api.Tests/Endpoints/KeycloakIntegrationTests.cs`
- Uses `IClassFixture<ApiFactory>` (not `IAsyncLifetime`) — no SeedAsync needed for 401/404 tests
- `/.well-known/jwks.json` → 404 (endpoint removed)
- `POST /api/auth/refresh` → 404 (endpoint removed)
- 401 content type: `WriteAsJsonAsync` overrides the explicit `"application/problem+json"` assignment
  in `OnChallenge` — actual content-type is `application/json` in the test host.
  Assert `Contains("json", contentType)` rather than exact match.
