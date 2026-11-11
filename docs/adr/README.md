# Architecture Decision Records

Each ADR captures one decision: the context that forced it, what we decided, what it costs us, and what we rejected. ADRs are immutable once accepted — a changed mind produces a new ADR that supersedes the old one, so the reasoning history stays intact.

All ADRs below are **Accepted**; they were ratified together with [ARCHITECTURE.md](../ARCHITECTURE.md).

| # | Decision | Status |
|---|---|---|
| [001](./001-dotnet-10-lts.md) | .NET 10 LTS across the stack, LTS-to-LTS policy | Accepted |
| [002](./002-native-capture-core-maui-shell.md) | Native C# capture core with a thin .NET MAUI shell (Godot assessed and rejected) | Accepted |
| [003](./003-native-xaml-ui-composition.md) | Native MAUI XAML for the MVP shell, not Blazor Hybrid | Accepted |
| [004](./004-blazor-wasm-pwa.md) | Blazor WebAssembly PWA for the web surface | Accepted |
| [005](./005-aspnet-core-lean-backend.md) | ASP.NET Core backend with minimal MVP infrastructure | Accepted |
| [006](./006-encrypted-chunked-evidence-pipeline.md) | Encrypted chunked CMAF upload, not WebRTC | Accepted |
| [007](./007-shared-evidence-library.md) | One shared evidence library and an open format specification | Accepted |
| [008](./008-e2ee-scope.md) | E2EE covers every artifact type, including notes, tags and location | Accepted |
| [009](./009-key-model-and-recovery.md) | Key hierarchy with recovery-key escrow on by default | Accepted |
| [010](./010-capability-link-sharing.md) | Capability-link sharing with the key in the URL fragment | Accepted |
| [011](./011-redundant-alert-delivery.md) | Redundant alert delivery across five channels | Accepted |
| [012](./012-distribution-and-side-loading.md) | Multi-channel distribution and side-loading strategy | Accepted |
| [013](./013-duress-and-stealth-features.md) | Duress PIN and stealth features, side-load channel only | Accepted |
| [014](./014-open-source-posture.md) | Open-source posture: verifier and evidence format FOSS | Accepted |

Programme decisions that are not architectural — iOS lands in Phase 2, and the working
name stays "Personal Recorder" (`PersonalRecorder.*`) — are recorded in
[ARCHITECTURE.md §17.1](../ARCHITECTURE.md).

## Format

```markdown
# ADR-NNN: Title

## Status
Proposed | Accepted | Superseded by ADR-XXX

## Context
The forces at play. What makes this a decision rather than an obvious call.

## Decision
What we are doing, stated so someone can act on it.

## Consequences
(+) what this buys  (−) what it costs. Both, honestly.

## Alternatives Considered
What else was on the table and why it lost.
```
