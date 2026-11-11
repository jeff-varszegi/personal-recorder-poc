# Personal Recorder

> "Evidence that cannot be deleted, confiscated, or suppressed; help that cannot be blocked or delayed."

A project for a personal recording and emergency-response system for legal observers, witnesses, and individuals in vulnerable situations.

## Status

**Phase 0 (Foundations)** — stack ratified 2026-07-31 (all-C#/.NET; see [ADRs](./docs/adr/)). Solution scaffold is the next step; the structure below is the agreed target, not yet on disk.

## Stack

| Layer | Technology | ADR |
|-------|-----------|-----|
| Backend | ASP.NET Core on .NET 10 LTS · SignalR · EF Core + PostgreSQL · S3-compatible storage (MinIO) | [001](./docs/adr/001-dotnet-10-lts.md), [002](./docs/adr/002-aspnet-core-backend-lean-infrastructure.md) |
| Mobile | Native C# capture core (.NET for Android / .NET for iOS) + thin .NET MAUI shell | [003](./docs/adr/003-native-csharp-capture-core-maui-shell.md) |
| Web | Blazor WebAssembly PWA (dashboard, emergency console, fallback recorder) | [004](./docs/adr/004-blazor-wasm-pwa-web.md) |
| Evidence pipeline | Client-encrypted CMAF segments over resumable HTTPS; E2EE with default recovery escrow | [005](./docs/adr/005-encrypted-chunked-evidence-pipeline.md), [006](./docs/adr/006-e2ee-key-model-recovery-escrow.md) |
| Alerts | FCM + APNs + UnifiedPush + SignalR + optional SMS | [007](./docs/adr/007-redundant-alert-delivery.md) |

## Project Structure (target, per [Development Plan §5](./docs/DEVELOPMENT_PLAN.md))

```
personal-recorder/
├── PersonalRecorder.sln
├── components/
│   ├── contracts/       # PersonalRecorder.Contracts — DTOs, SignalR contracts, error codes
│   ├── service/         # ASP.NET Core backend (Api / Domain / Application / Infrastructure / Workers)
│   ├── client/          # Capture cores (Android/iOS), Client.Core, MAUI shell, Android harness
│   ├── web/             # Blazor WASM PWA dashboard
│   └── verifier/        # Open verifier CLI (recording integrity/chain verification)
├── tools/               # docker-compose.dev.yml, build & signing scripts
├── design/              # Design documents and naming history
├── docs/                # Development plan, ADRs
└── TECHNICAL_SPECIFICATION.md
```

## Development Prerequisites

- .NET 10 SDK (+ `dotnet workload install maui-android`; `maui-ios` needs macOS + Xcode, Phase 2)
- Docker Desktop (PostgreSQL + MinIO via `tools/docker-compose.dev.yml`, lands with the scaffold)
- Android Studio or command-line Android SDK + an emulator or physical device

Build/run commands will be documented here once the Phase 0 scaffold exists.

## Documentation

- [Development Plan](./docs/DEVELOPMENT_PLAN.md) — stack rationale, distribution strategy, phases, risks
- [Technical Specification](./TECHNICAL_SPECIFICATION.md) — requirements and system design
- [Architecture Decision Records](./docs/adr/)

## License

Proprietary. All rights reserved.
Open-source posture will be revisited (~Phase 3); the verifier CLI will be open source regardless. See [Development Plan §6.5](./docs/DEVELOPMENT_PLAN.md).
