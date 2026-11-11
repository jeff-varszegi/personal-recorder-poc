# Architecture Decision Records

This directory contains Architecture Decision Records (ADRs) for the Personal Recorder project.

## What is an ADR?

An ADR is a document that captures an important architectural decision made along with its context and consequences.

## Template

Use the following template for new ADRs:

```markdown
# ADR-NNN: Title

## Status
Proposed | Accepted | Deprecated | Superseded

## Context
What is the issue that we're seeing that motivates this decision?

## Decision
What is the decision that we're making?

## Consequences
What are the positive and negative consequences of this decision?

## Alternatives Considered
What other options were evaluated?
```

## Index

| ADR | Title | Status | Date |
|-----|-------|--------|------|
| [001](./001-dotnet-10-lts.md) | Target .NET 10 LTS Across the Stack | Accepted | 2026-07-31 |
| [002](./002-aspnet-core-backend-lean-infrastructure.md) | ASP.NET Core Backend with Lean MVP Infrastructure | Accepted | 2026-07-31 |
| [003](./003-native-csharp-capture-core-maui-shell.md) | Native C# Capture Core with .NET MAUI UI Shell | Accepted | 2026-07-31 |
| [004](./004-blazor-wasm-pwa-web.md) | Blazor WebAssembly PWA for Web Surfaces | Accepted | 2026-07-31 |
| [005](./005-encrypted-chunked-evidence-pipeline.md) | Encrypted Chunked-Upload Evidence Pipeline | Accepted | 2026-07-31 |
| [006](./006-e2ee-key-model-recovery-escrow.md) | E2EE Key Model with Default Recovery-Key Escrow | Accepted | 2026-07-31 |
| [007](./007-redundant-alert-delivery.md) | Redundant Alert Delivery Channels | Accepted | 2026-07-31 |

### Historical note

The v0.1 index (2026-01-16) listed three "Accepted" decisions — *React Native for Mobile*, *Fastify for Backend API*, and *pnpm Workspaces for Monorepo* — whose ADR files were never authored. All three were superseded on 2026-07-31 by the all-C#/.NET direction: React Native → ADR-003, Fastify → ADR-002, pnpm workspace → single .NET solution (see [DEVELOPMENT_PLAN.md §5](../DEVELOPMENT_PLAN.md)).

---

*Decision provenance for ADRs 001–007: proposed in [docs/DEVELOPMENT_PLAN.md](../DEVELOPMENT_PLAN.md) §3, ratified with decisions D1–D9 on 2026-07-31.*
