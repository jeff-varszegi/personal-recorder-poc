# Personal Recorder

> "Evidence that cannot be deleted, confiscated, or suppressed; help that cannot be blocked or delayed."

A project by **Jeffrey Varszegi**: a personal recording and emergency-response system for legal observers, witnesses of government activity, domestic-violence survivors, home-alone seniors, and general personal safety.

## Status

**Phase 0 — Foundations.** Architecture ratified ([ARCHITECTURE.md](./docs/ARCHITECTURE.md), [ADRs 001–014](./docs/adr/)). The eight-project solution ([ADR-015](./docs/adr/015-consolidated-layout-and-service-stack.md)) is on disk and green, with the first code in place: identifiers and the evidence hash chain in Core, the capture, panic and upload contracts in Client, and project- and namespace-level architecture tests. Next: the domain model (`PersonalRecorder.Models`), then the capture-core spikes.

## Stack

| Layer | Technology | ADR |
|---|---|---|
| Runtime | .NET 10 LTS everywhere; LTS-to-LTS upgrade policy | [001](./docs/adr/001-dotnet-10-lts.md) |
| Mobile | Native C# capture core (.NET for Android / .NET for iOS) under a thin .NET MAUI shell | [002](./docs/adr/002-native-capture-core-maui-shell.md), [003](./docs/adr/003-native-xaml-ui-composition.md) |
| Web | Blazor WebAssembly PWA — dashboard, emergency console, degraded fallback recorder | [004](./docs/adr/004-blazor-wasm-pwa.md) |
| Service | ASP.NET Core controllers + Dapper over PostgreSQL 17 + S3-compatible storage, single process | [005](./docs/adr/005-aspnet-core-lean-backend.md), [015](./docs/adr/015-consolidated-layout-and-service-stack.md) |
| Evidence | Client-encrypted CMAF segments over resumable HTTPS; hash chain + signatures | [006](./docs/adr/006-encrypted-chunked-evidence-pipeline.md), [007](./docs/adr/007-shared-evidence-library.md) |
| Crypto | E2EE for every artifact type; recovery-key escrow on by default | [008](./docs/adr/008-e2ee-scope.md), [009](./docs/adr/009-key-model-and-recovery.md), [010](./docs/adr/010-capability-link-sharing.md) |
| Alerts | FCM + APNs + UnifiedPush + SignalR + optional SMS | [011](./docs/adr/011-redundant-alert-delivery.md) |
| Distribution | Direct APK + self-updater, own F-Droid repo, stores in parallel, PWA floor | [012](./docs/adr/012-distribution-and-side-loading.md) |

## Repository structure

```
personal-recorder/
├── PersonalRecorder.slnx
├── PersonalRecorder.WorkloadFree.slnf   everything that builds without a mobile workload (the CI core job)
├── components/
│   ├── core/       PersonalRecorder.Core       models, evidence format, cryptography
│   ├── client/     PersonalRecorder.Client     capture, recording, sync, storage, presentation (net10.0 + android)
│   │               PersonalRecorder.App        the MAUI shell
│   │               PersonalRecorder.Harness    headless Android capture harness, until Spike C passes
│   ├── service/    PersonalRecorder.Service    the back end: one process, feature namespaces
│   ├── web/        PersonalRecorder.Web        Blazor WASM PWA
│   └── verifier/   PersonalRecorder.Verifier   offline evidence verifier CLI
├── test/           PersonalRecorder.Tests      .Core · .Client · .Service · .Architecture
├── tools/          docker-compose.dev.yml, build & signing scripts
├── docs/           architecture, ADRs, proposals
└── design/         naming history, UX notes
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

# Apply schema migrations, then run the service (health probe at /health, OpenAPI in Development)
dotnet run --project components/service/PersonalRecorder.Service -- migrate
dotnet run --project components/service/PersonalRecorder.Service

# Run the web PWA
dotnet run --project components/web/PersonalRecorder.Web

# Deploy the shell to a connected Android device or running emulator
dotnet build components/client/PersonalRecorder.App -t:Run -f net10.0-android
```

Without the Android workload, build and test the portable subset instead:

```bash
dotnet build PersonalRecorder.WorkloadFree.slnf -p:IncludeMobileTargets=false
dotnet test  PersonalRecorder.WorkloadFree.slnf -p:IncludeMobileTargets=false
```

The architecture tests in `test/PersonalRecorder.Tests` enforce the dependency rules in [ARCHITECTURE.md §5.3](./docs/ARCHITECTURE.md) on every build, at project level (who may reference whom) and at namespace level inside each assembly: capture never reaches presentation or a UI framework, service features never reach each other, and the verifier never reaches the service or the network.

## Documentation

- [Architecture](./docs/ARCHITECTURE.md) — the whole system: technology rationale, evidence pipeline, cryptography, service design, distribution, threat model, phases
- [Technical Specification](./TECHNICAL_SPECIFICATION.md) — requirements baseline (`REC-*`, `STR-*`, `HB-*`, `EMR-*`, …)
- [Architecture Decision Records](./docs/adr/)

## License

[Apache License 2.0](./LICENSE) — Copyright © 2026 Jeffrey Varszegi. See also [NOTICE](./NOTICE).

The whole repository is currently open source but may be adjusted later ([ADR-016](./docs/adr/016-apache-2-whole-repository.md)). `PersonalRecorder.Core` (the evidence format and its implementation) and the verifier CLI stay open permanently, whatever is decided for the product projects before the first public release ([ADR-014](./docs/adr/014-open-source-posture.md)) — independent verification is worthless if it depends on trusting us.
