---
name: JwksHealthCheck public visibility exception
description: JwksHealthCheck must remain public because Program.cs references it as a generic type argument; InternalsVisibleTo does not cover Kernel.Api.
type: project
---

`SpaceOS.Infrastructure/Health/JwksHealthCheck.cs` is `public sealed` rather than `internal sealed`.

Infrastructure CLAUDE.md requires `internal sealed` for infrastructure implementations. However, `Program.cs` in `SpaceOS.Kernel.Api` uses `AddCheck<JwksHealthCheck>` and `nameof(JwksHealthCheck)` — a direct generic type reference that cannot be resolved if the class is internal (the Api project is not in `InternalsVisibleTo`).

**Approved by:** Not yet approved — flagged as UNFIXABLE in REVIEW_REPORT_KC01 (2026-04-09).

**Scope:** `SpaceOS.Infrastructure/Health/JwksHealthCheck.cs` only.

Resolution options pending developer decision:
1. Register via factory lambda `services.AddCheck("jwks", sp => new JwksHealthCheck(...), tags: ["ready"])` to avoid the generic type reference — allows making the class internal.
2. Add `SpaceOS.Kernel.Api` to `InternalsVisibleTo` in Infrastructure csproj — but this expands the visibility boundary architecturally.
