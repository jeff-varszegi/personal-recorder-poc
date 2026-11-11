# Proposal 001: Consolidated Project and Namespace Layout

**Status:** Proposed — for review, nothing executed
**Supersedes if ratified:** [ARCHITECTURE.md §5.2/§5.3](../ARCHITECTURE.md); amends [ADR-005](../adr/005-aspnet-core-lean-backend.md) (data access, API style), [ADR-007](../adr/007-shared-evidence-library.md) (assembly name), [ADR-014](../adr/014-open-source-posture.md) (FOSS boundary)

---

## 1. The direct answer: 19 → 8

Your instinct was one client, one back end, one shared. That is almost exactly right. Four things force splits beyond it, and everything else was ceremony:

| What forces a split | Why it is not negotiable |
|---|---|
| **Target framework** | An assembly has one TFM set. Browser code (`net10.0`) and CameraX code (`net10.0-android`) cannot be the same compilation — though *multi-targeting* lets them be the same **project**, which is how we get from three capture projects to one. |
| **Deployment artifact** | The APK, the container, the static PWA bundle and the verifier binary are four separate things that ship to four separate places. |
| **The licensing boundary** | [ADR-014](../adr/014-open-source-posture.md) makes the verifier and evidence format open source while the rest stays proprietary. FOSS code must compile without proprietary code, which makes that boundary an assembly boundary whether we like it or not. |
| **The replaceable shell** | [ADR-002](../adr/002-native-capture-core-maui-shell.md)'s central bet is that swapping MAUI for Uno is a screens rewrite. If shell and capture live in one assembly, that stops being structurally true and becomes a promise. |

Everything else — Api/Application/Domain/Infrastructure/Workers, Capture.Abstractions as its own project, four test projects — was layering expressed as project references. Namespaces express it just as well, and enforcement moves to the architecture tests (§6).

### Proposed set

| # | Project | TFM(s) | Ships as | Replaces |
|---|---|---|---|---|
| 1 | **`PersonalRecorder.Core`** | `net10.0` | library (**FOSS**) | Contracts + Evidence |
| 2 | **`PersonalRecorder.Client`** | `net10.0`, `net10.0-android` (+`-ios` P2) | library | Client.Core + Capture.Abstractions + Capture.Android + Capture.Ios |
| 3 | **`PersonalRecorder.App`** | `net10.0-android` (+`-ios` P2) | APK / IPA | Client.App |
| 4 | **`PersonalRecorder.Web`** | `net10.0` | static PWA | Web |
| 5 | **`PersonalRecorder.Service`** | `net10.0` | container | Api + Application + Domain + Infrastructure + Workers |
| 6 | **`PersonalRecorder.Verifier`** | `net10.0` | CLI (**FOSS**) | Verifier |
| 7 | **`PersonalRecorder.Harness`** | `net10.0-android` | debug APK | AndroidHarness |
| 8 | **`PersonalRecorder.Tests`** | `net10.0` | — | 4 test projects |

Project 7 exists only so Spike A (Android capture core) can proceed without waiting on Spike C (MAUI shell). **It is deletable once Spike C passes** — flagged now so it does not quietly become permanent.

Dependency graph, in full:

```
Core   ←── Client ←── App
  ↑         ↑     ↖── Harness
  │         └───────── Web
  ├── Service
  └── Verifier
```

Six edges. That is the whole architecture.

---

## 2. Naming rules

Two rules, both of them yours:

**Rule 1 — Core owns the root namespace.** `PersonalRecorder.Core.dll` contains `PersonalRecorder.Models`, `PersonalRecorder.Evidence`, `PersonalRecorder.Cryptography`. The `Core` segment is a bundling name and does not appear in any namespace inside it. This is the BCL's own pattern (`System.Private.CoreLib` holds `System.*`), and it reads well: Core is the shared vocabulary of the system, so it gets the shared vocabulary's namespace.

**Rule 2 — every other assembly namespaces under its own name.** `PersonalRecorder.Client.Capture`, `PersonalRecorder.Service.Ingest`. This keeps navigation obvious and prevents collisions — without it, the client's service-client and the service's own API surface both want to be called `PersonalRecorder.Api`.

The cost of Rule 1 is that "which assembly holds this type?" stops being answerable from the namespace alone. With eight assemblies and one exception to the rule, the map fits in a sentence: *root-level namespaces are Core; everything else says where it lives.*

**Entities go in `.Models`**, as you suggested.

---

## 3. Namespace catalog — the review surface

### `PersonalRecorder.Core` — net10.0, FOSS, no I/O, no network

The rule that keeps this assembly honest: **Core is published, so anything we would not publish does not go in Core.** That constraint also naturally keeps it thin.

| Namespace | Contents |
|---|---|
| `PersonalRecorder` | Strongly-typed IDs (`RecordingId`, `CircleId`, `DeviceId`…), `Result`/error types, error codes, cross-cutting enums |
| `PersonalRecorder.Models` | The domain as a user experiences it: `Recording`, `Snapshot`, `Note`, `Tag`, `LocationPoint`, `LocationTrack`, `Circle`, `CircleMember`, `Device`, `UserProfile`, `UserSettings`, `RetentionPolicy`, `Alert`, `ShareLink`, `HeartbeatSession` |
| `PersonalRecorder.Evidence` | The `.prseg` container (read/write), hash chain, chain manifest, export-bundle assembly, verification results. One canonical implementation, per ADR-007 — the assembly it lives in changes, the guarantee does not |
| `PersonalRecorder.Cryptography` | Key hierarchy, HKDF segment-key derivation, AEAD, sealed boxes, Ed25519 signing, recovery-code wrapping, blinded tag tokens |

**One caveat on "shared until they diverge."** You are right as a general rule, and I want to flag where it is already false: E2EE forces client and server models apart on day one. The client's `Note` has `Content`; the server's row has an opaque blob, a size, and a chain hash — it *cannot* have content. So `Models` holds the user-facing domain, and the service's row types live in `Service.Data` (§3.4) from the start. IDs, enums and genuinely shared shapes stay in Core.

I have deliberately **not** created a `Contracts` namespace. Wire DTOs start as the models themselves; a `PersonalRecorder.Contracts` namespace appears in Core the first time a wire shape genuinely diverges from a model, not before.

### `PersonalRecorder.Client` — net10.0 + platform TFMs

Multi-targeted. Platform files are excluded by convention rather than `#if`:

```xml
<Compile Remove="Platforms/**/*.cs" />
<ItemGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android'">
  <Compile Include="Platforms/Android/**/*.cs" />
</ItemGroup>
```

The `net10.0` flavour is what the Blazor PWA references — it gets view-models, the service client and the sync queue, and simply has no capture implementation.

| Namespace | TFM | Contents |
|---|---|---|
| `PersonalRecorder.Client.Capture` | all | `IRecordingEngine`, `ICaptureBuffer`, `ILocationTracker`, `ISecureStore`, `IPushChannel`; the COLD/WARM/HOT/RECORDING state machine contract |
| `PersonalRecorder.Client.Capture.Android` | android | CameraX/Camera2 + MediaCodec engine, foreground services, Keystore, fused location |
| `PersonalRecorder.Client.Capture.Ios` | ios | AVCaptureSession + AVAssetWriter, Keychain/Secure Enclave, CoreLocation |
| `PersonalRecorder.Client.Recording` | all | Session orchestration, lookback ring management, the panic pipeline and its time budget |
| `PersonalRecorder.Client.Sync` | all | The one prioritized persistent upload queue: resumable offsets, backpressure, priority lanes |
| `PersonalRecorder.Client.Storage` | all | Local encrypted store (SQLite metadata + segment ring), client-side search index |
| `PersonalRecorder.Client.Remote` | all | HTTP and SignalR clients for the service |
| `PersonalRecorder.Client.Presentation` | all | View-models and state machines shared by the MAUI shell and Blazor. No UI-framework types — this is the seam that makes the shell replaceable |

### `PersonalRecorder.Service` — net10.0

Organised by **feature, not by layer**. With one assembly, horizontal layer namespaces buy nothing: layers were only ever enforced by project references. Vertical features mean a change to heartbeat handling touches one folder.

| Namespace | Contents |
|---|---|
| `PersonalRecorder.Service.Controllers` | ASP.NET Core controllers (§4) |
| `PersonalRecorder.Service.Hubs` | SignalR hubs: presence, alerts, live-view notification |
| `PersonalRecorder.Service.Ingest` | Resumable segment upload sessions, offset tracking, header-only continuity checks, chain-head countersigning |
| `PersonalRecorder.Service.Artifacts` | Metadata queries, blinded-tag filtering, pin/retention state, export-bundle assembly |
| `PersonalRecorder.Service.Circles` | Membership, roles and permissions, invites, key-wrap distribution, revocation |
| `PersonalRecorder.Service.Alerts` | Fan-out across the five channels, per-channel delivery receipts, escalation |
| `PersonalRecorder.Service.Heartbeat` | Check-in sessions, miss detection, retention holds |
| `PersonalRecorder.Service.Sharing` | Capability links: issue, scope, expire, revoke, access log |
| `PersonalRecorder.Service.Identity` | Registration, JWT issuance, TOTP, device enrolment, public-key directory |
| `PersonalRecorder.Service.Data` | Dapper repositories, row types, connection factory, migrations (§5) |
| `PersonalRecorder.Service.Storage` | S3-compatible object-store adapter |
| `PersonalRecorder.Service.Messaging` | Outbox and the push-channel adapters (§7) |
| `PersonalRecorder.Service.Workers` | Hosted services: heartbeat monitor, alert dispatcher, retention, storage GC |

### `PersonalRecorder.Web`, `.App`, `.Verifier`, `.Harness`, `.Tests`

| Assembly | Namespaces |
|---|---|
| `PersonalRecorder.Web` | `.Pages`, `.Components`, `.Playback` (MSE + client-side decryption), `.Recording` (degraded `MediaRecorder` capture), `.Verification` (in-browser chain check) |
| `PersonalRecorder.App` | `.Views`, `.Handlers` (native camera-preview embedding), `.Platforms` |
| `PersonalRecorder.Verifier` | `PersonalRecorder.Verifier` — flat; it is one command |
| `PersonalRecorder.Harness` | `PersonalRecorder.Harness` — flat; deleted after Spike C |
| `PersonalRecorder.Tests` | `.Core`, `.Service`, `.Client`, `.Architecture` — mirroring what they test |

---

## 4. Controllers over minimal APIs

**You are right, and I am changing the recommendation.** ADR-005 chose minimal APIs largely by default.

The honest comparison for *this* service:

- **Performance is a non-argument.** Minimal APIs avoid controller activation and some reflection, which is real but measured in microseconds. Every endpoint here is I/O-bound on PostgreSQL or S3. The difference is noise.
- **Controllers ship the structure we would otherwise build.** `[ApiController]` gives automatic model validation with ProblemDetails responses, attribute routing conventions, action/authorization/exception filters, and ApiExplorer integration that OpenAPI consumes cleanly. Minimal APIs have narrowed this gap (endpoint filters, route groups, `TypedResults`), but you assemble the conventions yourself — and every project that does so assembles them slightly differently.
- **Thirteen feature areas is not a trivial project.** Cross-cutting concerns — auth policies per endpoint group, consistent error shapes, request logging with artifact IDs, rate limiting on ingest — are exactly what filters exist for.

The one place I would still consider a minimal endpoint is **segment ingest**, where we want to stream the raw request body without model binding and return precise resumable-upload status codes. That works fine in a controller too (`Request.Body` with buffering disabled), so I would start uniform and only split it out if profiling justifies it.

**Recommendation: controllers throughout.**

---

## 5. Dapper over EF Core

**Agreed, and the fit here is better than the general case.**

Your objection is to the change tracker — an identity map threaded through the data access layer, which is a liability whenever you did not want it and a source of surprise when it silently caches, lazily loads, or leaks tracked entities across a request. For this system it earns even less than usual: our access patterns are high-volume append (segments), simple filtered metadata queries, and outbox polling. There are no deep object graphs, and E2EE means the server never does rich domain work over content anyway.

What Dapper costs, and how we cover it:

| Lost with EF Core | Replacement |
|---|---|
| Migrations | **DbUp** with numbered SQL scripts — transparent, PostgreSQL-native, and it keeps the schema reviewable as SQL rather than as generated C# |
| LINQ composition | Explicit SQL. For filtered artifact queries, a small hand-rolled predicate builder — parameterised, never string-concatenated |
| Change tracking / unit of work | Explicit transactions via `IDbTransaction`. For this workload that is a feature |
| Mapping ceremony | `QueryAsync<T>` onto `record` row types is close to zero ceremony |

**One consequence you should decide on deliberately (see L7).** ASP.NET Core Identity's default store is EF Core. Keeping Identity means either taking EF Core back just for auth tables, or writing custom Dapper `IUserStore`/`IRoleStore` implementations.

There is a better option: **skip Identity.** Its user model already fits us poorly — our users have device keypairs, key wraps, escrowed recovery blobs, and per-device enrolment, none of which Identity models. We would use framework primitives directly (`PasswordHasher<T>` for hashing, a TOTP library, `System.IdentityModel.Tokens.Jwt` for issuance) over our own Dapper-backed tables. That is not rolling our own crypto; it is declining a schema that does not fit. The auth surface goes into the Phase 3 external audit scope either way.

---

## 6. What consolidation costs, and how we keep it

Fewer projects means fewer project-reference edges, and project references were how ADR-002's structural guarantee was enforced. Six edges cannot express "capture must not depend on presentation."

So the architecture tests move from **project level to namespace and type level**, using reflection over the built assemblies:

- `PersonalRecorder.Models` may not reference `PersonalRecorder.Client.*` or `PersonalRecorder.Service.*`
- `PersonalRecorder.Client.Capture.*` may not reference `Microsoft.Maui.*` or `PersonalRecorder.Client.Presentation`
- `PersonalRecorder.Client.Presentation` may not reference any UI framework
- `Core` may not reference anything outside the BCL and libsodium — no HTTP, no SignalR, no ASP.NET
- `Verifier` may not reference the service or any network stack
- Service feature namespaces may not reference each other directly; they meet at `Data` and at the controllers

This is strictly more valuable than what we have now: it catches violations that project references never could, such as a view-model leaking into the capture core within a single assembly. It is also more work — roughly a day, and it is the main cost of this proposal.

---

## 7. Outbox and push, explained

You asked what these are.

**The outbox** solves a problem that matters most for the one thing this product exists for. When a panic alert fires, two things must happen: record it in the database, and call FCM/APNs/UnifiedPush. Those cannot be made atomic — if we write the row and crash before the push call, the alert is silently lost. For a panic alert that is the worst possible failure.

The outbox pattern makes it atomic by making the *intent* part of the same transaction:

```
BEGIN
  INSERT INTO alerts        (...)   -- the fact
  INSERT INTO outbox        (...)   -- the intent: "deliver this alert"
COMMIT
                ↓
   dispatcher worker claims the row, performs delivery, marks it done
   (crash → the row is still there → work resumes; at-least-once)
```

At-least-once is exactly the right guarantee here: a duplicate alert is a minor annoyance, a lost one is a failure of the product's purpose. PostgreSQL `LISTEN/NOTIFY` wakes the dispatcher immediately, with polling as the fallback — no message broker required at MVP (ADR-005).

**Push** is the concrete adapters behind `IPushChannel` for the five channels in [ADR-011](../adr/011-redundant-alert-delivery.md) — FCM, APNs, UnifiedPush, SignalR direct, and the optional SMS bridge — plus the per-channel delivery-receipt recording that lets an undelivered panic alert escalate visibly instead of failing silently. They sit next to the outbox in `Service.Messaging` because they are the outbox's consumers.

---

## 8. Decisions requested

| # | Decision | Recommendation |
|---|---|---|
| **L1** | Consolidate 19 projects → 8 as in §1 | **Yes.** Your instinct, plus the four splits that deployment and licensing force |
| **L2** | Core owns the root namespace; every other assembly namespaces under its own name | **Yes** (§2) — your bundling-assembly technique, scoped to the one assembly that is purely a bundle |
| **L3** | Entities in `.Models`, shared in Core, split only on divergence | **Yes**, with the E2EE caveat: server row types start separate because they genuinely cannot hold plaintext |
| **L4** | One multi-targeted `Client` project rather than separate abstraction/Android/iOS projects | **Yes.** Alternative if you prefer no multi-targeting: `Client` + `Client.Android` + `Client.Ios` = 10 projects, no `Platforms/` globbing |
| **L5** | Controllers instead of minimal APIs | **Yes** — you are right; amends ADR-005 |
| **L6** | Dapper + DbUp instead of EF Core | **Yes** — good fit for an append-heavy, E2EE workload; amends ADR-005 |
| **L7** | Drop ASP.NET Core Identity; bespoke auth tables over Dapper using framework primitives | **Yes**, and this one deserves your explicit sign-off because it touches security. The alternative is EF Core for auth tables only |
| **L8** | Service organised by feature namespace, not by layer | **Yes** (§3.4) |
| **L9** | Keep `Harness` for Phase 0, delete after Spike C passes | **Yes**, with the deletion written down now |

## 9. If ratified

1. Restructure the solution — the projects are empty, so this is a file move plus csproj edits, not a migration.
2. Rewrite `DependencyRuleTests` at namespace/type level (§6) — the real work, roughly a day.
3. Amend ADR-005 (API style, data access, Identity), ADR-007 (assembly name), ADR-014 (FOSS boundary is now Core + Verifier, which is broader and cleaner than Evidence + Verifier).
4. Update ARCHITECTURE.md §5.2/§5.3 and the README structure section.
5. Then the model design conversation you have been waiting for — `PersonalRecorder.Models` first, together.
