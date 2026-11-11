# ADR-015: Consolidated Project Layout, Controllers, Dapper, and Bespoke Authentication

## Status
Accepted. Ratified from [Proposal 001](../proposals/001-consolidated-project-and-namespace-layout.md), which holds the fuller reasoning. Amends [ADR-005](./005-aspnet-core-lean-backend.md) (API style, data access, authentication), [ADR-007](./007-shared-evidence-library.md) (the evidence library is a namespace of `PersonalRecorder.Core`) and [ADR-014](./014-open-source-posture.md) (the open-source boundary is `Core` plus `Verifier`); renames the projects named in [ADR-002](./002-native-capture-core-maui-shell.md) and [ADR-003](./003-native-xaml-ui-composition.md) to namespaces without changing what they decide.

## Context
The first scaffold had nineteen projects: Api/Application/Domain/Infrastructure/Workers as separate assemblies, a separate capture-abstractions project, one capture project per platform, four test projects. That is layering expressed as project references. Only four things genuinely force an assembly boundary here:

| What forces a split | Why |
|---|---|
| Target framework | Browser code (`net10.0`) and CameraX code (`net10.0-android`) cannot share a compilation — though multi-targeting lets them share a *project* |
| Deployment artifact | The APK, the container, the static PWA bundle and the verifier binary ship to four different places |
| The licensing boundary | ADR-014 opens the verifier and the evidence format; open code must compile without proprietary code |
| The replaceable shell | ADR-002's bet is that swapping MAUI for Uno is a screens rewrite; shell and capture in one assembly would make that a promise rather than a structure |

Everything else is expressed as well by namespaces, with enforcement moved into the architecture tests.

Three of ADR-005's defaults also deserved a second look once the service's real shape was clear. Minimal APIs were chosen by default; the service is I/O-bound on PostgreSQL and S3, so their performance edge is noise, while controllers ship the conventions (validation with ProblemDetails, attribute routing, filters, ApiExplorer) that thirteen feature areas would otherwise assemble by hand. EF Core's change tracker — an identity map threaded through data access — buys nothing for an append-heavy workload with no deep object graphs, where E2EE means the server never does rich domain work over content. And ASP.NET Core Identity's user model does not fit users who have device keypairs, key wraps, escrowed recovery blobs and per-device enrolment.

## Decision
1. **Eight projects**, with the dependency graph in full:

   | Project | TFM(s) | Ships as |
   |---|---|---|
   | `PersonalRecorder.Core` | `net10.0` | library, **open source** |
   | `PersonalRecorder.Client` | `net10.0`, `net10.0-android` (+`-ios` in Phase 2) | library |
   | `PersonalRecorder.App` | `net10.0-android` (+`-ios` in Phase 2) | APK / IPA |
   | `PersonalRecorder.Web` | `net10.0` | static PWA |
   | `PersonalRecorder.Service` | `net10.0` | container |
   | `PersonalRecorder.Verifier` | `net10.0` | CLI, **open source** |
   | `PersonalRecorder.Harness` | `net10.0-android` | debug APK |
   | `PersonalRecorder.Tests` | `net10.0` | — |

   ```
   Core   ←── Client ←── App
     ↑         ↑     ↖── Harness
     │         └───────── Web
     ├── Service
     └── Verifier
   ```

2. **`Core` owns the root namespace.** `PersonalRecorder.Core.dll` contains `PersonalRecorder`, `PersonalRecorder.Models`, `PersonalRecorder.Evidence` and `PersonalRecorder.Cryptography`; the `Core` segment appears in no namespace. Every other assembly namespaces under its own name. Entities go in `.Models`. Shared shapes stay shared until they diverge — and the E2EE split is already a divergence: the client's note has content, the server's row has an opaque blob, so service row types live in `Service.Data` from the start.
3. **`Client` is one multi-targeted project.** Platform code lives under `Platforms/<os>/` and is compiled in by folder convention, never by `#if`. The `net10.0` flavour, with no capture implementation, is what the browser references.
4. **`Service` is organised by feature namespace**, not by layer: `Ingest`, `Artifacts`, `Circles`, `Alerts`, `Heartbeat`, `Sharing`, `Identity`, meeting at `Data`, `Storage` and `Messaging`, with `Controllers`, `Hubs` and `Workers` as the entry points.
5. **Controllers throughout.** Segment ingest streams the raw request body from a controller as well; it is split into a minimal endpoint only if profiling ever justifies it.
6. **Dapper over Npgsql**, with **DbUp** applying numbered SQL scripts, so the schema is reviewed as SQL. Explicit transactions; a small parameterised predicate builder for filtered artifact queries.
7. **No ASP.NET Core Identity.** Bespoke, Dapper-backed user and device tables, using framework primitives directly: `PasswordHasher<T>`, a TOTP library, `System.IdentityModel.Tokens.Jwt`. This is declining a schema that does not fit, not rolling our own cryptography; the authentication surface is in the Phase 3 external audit scope.
8. **Architecture tests move to namespace and type level**, reading the compiled IL: Core never depends on Client, Service, Web or App, and takes no package beyond the BCL (a libsodium binding being the one planned exception); `Client.Capture.*` never on `Microsoft.Maui.*` or `Client.Presentation`; `Client.Presentation` never on any UI framework; service features never on each other or on the entry points; `Verifier` never on the service or a network stack. The project-level allow-list (six edges) stays as a first line.
9. **The Harness stays until Spike C passes**, then is deleted. Written down now so it does not quietly become permanent.

## Consequences
- (+) Six edges are the whole architecture; a change to heartbeat handling touches one folder.
- (+) Namespace-level enforcement catches what project references never could, such as a view-model leaking into the capture core within one assembly.
- (+) Controllers give consistent error shapes, per-group authorization policies and request logging through filters rather than per-project convention.
- (+) Dapper's cost — explicit SQL, explicit transactions — is a feature for an append-heavy, E2EE workload.
- (−) "Which assembly holds this type?" is no longer answerable from the namespace alone for Core. The map fits in a sentence: root-level namespaces are Core; everything else says where it lives.
- (−) Identity's ready-made flows (lockout, confirmation, external logins) are given up; each is built only if needed.
- (−) Migrations are hand-written SQL and schema drift is detected by review and test, not by a model diff.

## Alternatives Considered
- **Keep the nineteen-project layout**: layers enforced by project references, at the cost of ceremony in every change and a boundary (capture vs presentation) that project references could not express anyway.
- **`Client` + `Client.Android` + `Client.Ios`, no multi-targeting**: ten projects and no `Platforms/` globbing; rejected as a slightly larger surface for the same guarantee.
- **Minimal APIs**: chosen by ADR-005 by default; retained for nothing, since the one endpoint that wants raw body streaming works in a controller too.
- **EF Core**: migrations and LINQ composition for free, with a change tracker this workload does not want and a server that never does domain work over content.
- **EF Core for Identity's tables only**: keeps Identity at the price of a second data stack for a user model that still does not fit.
