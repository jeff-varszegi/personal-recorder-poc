# Personal Recorder

> "Evidence that cannot be deleted, confiscated, or suppressed; help that cannot be blocked or delayed."

A **NimbleJinn** project: a personal recording and emergency-response system for legal observers, witnesses of government activity, domestic-violence survivors, home-alone seniors, and general personal safety.

## Status

**Phase 0 — Foundations.** Architecture ratified ([ARCHITECTURE.md](./docs/ARCHITECTURE.md), [ADRs 001–014](./docs/adr/)). The solution scaffold is on disk and green. [Proposal 001](./docs/proposals/001-consolidated-project-and-namespace-layout.md), which consolidates the scaffold to eight projects, is awaiting ratification; capture-core spikes follow.

## Stack

| Layer | Technology | ADR |
|---|---|---|
| Runtime | .NET 10 LTS everywhere; LTS-to-LTS upgrade policy | [001](./docs/adr/001-dotnet-10-lts.md) |
| Mobile | Native C# capture core (.NET for Android / .NET for iOS) under a thin .NET MAUI shell | [002](./docs/adr/002-native-capture-core-maui-shell.md), [003](./docs/adr/003-native-xaml-ui-composition.md) |
| Web | Blazor WebAssembly PWA — dashboard, emergency console, degraded fallback recorder | [004](./docs/adr/004-blazor-wasm-pwa.md) |
| Service | ASP.NET Core + PostgreSQL 17 + S3-compatible storage, single process | [005](./docs/adr/005-aspnet-core-lean-backend.md) |
| Evidence | Client-encrypted CMAF segments over resumable HTTPS; hash chain + signatures | [006](./docs/adr/006-encrypted-chunked-evidence-pipeline.md), [007](./docs/adr/007-shared-evidence-library.md) |
| Crypto | E2EE for every artifact type; recovery-key escrow on by default | [008](./docs/adr/008-e2ee-scope.md), [009](./docs/adr/009-key-model-and-recovery.md), [010](./docs/adr/010-capability-link-sharing.md) |
| Alerts | FCM + APNs + UnifiedPush + SignalR + optional SMS | [011](./docs/adr/011-redundant-alert-delivery.md) |
| Distribution | Direct APK + self-updater, own F-Droid repo, stores in parallel, PWA floor | [012](./docs/adr/012-distribution-and-side-loading.md) |

## Repository structure

```
personal-recorder/
├── PersonalRecorder.slnx
├── components/
│   ├── contracts/    DTOs, SignalR contracts, error codes
│   ├── evidence/     segment format, hash chain, crypto, verification (shared everywhere)
│   ├── client/       capture cores, Client.Core, MAUI shell, headless Android harness
│   ├── service/      Api · Application · Domain · Infrastructure · Workers
│   ├── web/          Blazor WASM PWA
│   └── verifier/     open-source evidence verifier CLI
├── test/             solution-wide architecture rules
├── tools/            docker-compose.dev.yml, build & signing scripts
├── docs/             architecture, ADRs
└── design/           naming history, UX notes
```

## Prerequisites

- **.NET 10 SDK** (10.0.100 or later — see `global.json`)
- **Android workloads**: `dotnet workload install maui-android android`
- **Docker Desktop** for the local PostgreSQL + MinIO stack
- Android Studio or the command-line Android SDK, plus an emulator or physical device

iOS is Phase 2 ([ARCHITECTURE.md §15](./docs/ARCHITECTURE.md)); the iOS capture library compiles on Windows, but packaging an iOS app requires a Mac build host.

## Getting started

```bash
# Backend dependencies (PostgreSQL 17 + MinIO, buckets auto-created)
docker compose -f tools/docker-compose.dev.yml up -d

# Build and test everything
dotnet build PersonalRecorder.slnx
dotnet test  PersonalRecorder.slnx

# Run the service (health probe at /health, OpenAPI in Development)
dotnet run --project components/service/src/PersonalRecorder.Service.Api

# Run the web PWA
dotnet run --project components/web/src/PersonalRecorder.Web

# Deploy the shell to a connected Android device or running emulator
dotnet build components/client/src/PersonalRecorder.Client.App -t:Run -f net10.0-android
```

The architecture tests in `test/PersonalRecorder.ArchitectureTests` enforce the dependency rules in [ARCHITECTURE.md §5.3](./docs/ARCHITECTURE.md) on every build — including that the capture core never depends on a UI framework, and that the verifier never depends on the service.

## Documentation

- [Architecture](./docs/ARCHITECTURE.md) — the whole system: technology rationale, evidence pipeline, cryptography, service design, distribution, threat model, phases
- [Technical Specification](./TECHNICAL_SPECIFICATION.md) — requirements baseline (`REC-*`, `STR-*`, `HB-*`, `EMR-*`, …)
- [Architecture Decision Records](./docs/adr/)

## License

Proprietary — © 2026 NimbleJinn LLC (formation in progress, New Hampshire). All rights reserved.

The **evidence format specification and the verifier CLI are open source** regardless of the rest ([ADR-014](./docs/adr/014-open-source-posture.md)) — independent verification is worthless if it depends on trusting us.
