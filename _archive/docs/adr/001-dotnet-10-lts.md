# ADR-001: Target .NET 10 LTS Across the Stack

## Status
Accepted (2026-07-31)

## Context
The project is all-C#/.NET across backend, mobile client, and web (see ADR-002..004). A runtime version policy is needed. The v0.1 specification referenced .NET 8; .NET 10 shipped November 2025 as the current LTS (supported into November 2028), and MAUI 10 was an explicitly quality-focused release.

## Decision
All projects target .NET 10 LTS: `net10.0` (libraries, service, web), `net10.0-android` and `net10.0-ios` (capture cores, MAUI shell). Single-version policy: the entire solution moves LTS-to-LTS together; STS releases (.NET 11) are skipped unless a specific need arises.

## Consequences
- (+) Longest support runway available today; one toolchain version across every component.
- (+) MAUI 10 quality improvements land in the layer we are most cautious about.
- (−) We forgo STS features for up to two years at a time; acceptable for a safety-critical product that values stability.

## Alternatives Considered
- **.NET 8 LTS** (spec v0.1): support ends November 2026 — would force an upgrade mid-MVP.
- **Track STS releases**: more churn in exchange for features we have no identified need for.
