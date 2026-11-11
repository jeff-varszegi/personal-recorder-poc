# ADR-007: One Shared Evidence Library and an Open Format Specification

## Status
Accepted — amended by [ADR-015](./015-consolidated-layout-and-service-stack.md): the library is the `PersonalRecorder.Evidence` namespace of the `PersonalRecorder.Core` assembly rather than an assembly of its own. The guarantee — one implementation, every host — is unchanged.

## Context
The evidence format is written by two capture cores and the browser fallback recorder, and read by native players, the browser console, the export tooling and an independent verifier. In a multi-language stack that means four or five implementations of the same container, chain and crypto — and any divergence between them is a recording that fails verification in court while being perfectly intact.

Being all-C# makes a stronger option available: **one implementation, running everywhere**, including inside the browser via WebAssembly.

## Decision
- `PersonalRecorder.Evidence` (`net10.0`, no platform dependencies, AOT-friendly) is the single canonical implementation of: the `.prseg` segment container, per-segment key derivation and AEAD, the SHA-256 hash chain, Ed25519 signing, server countersignature validation, and export-bundle assembly and verification.
- It is consumed by `Capture.Android`, `Capture.Ios`, `Client.Core`, the Blazor web app (via WASM), and the verifier CLI. The service references it for **header-only continuity checks** and never for decryption.
- The **format specification is a published document**, versioned alongside the library, and open source (ADR-014) — a verifier nobody can read is not independent verification.
- Golden-file tests pin the format across versions; format changes require a version bump and a superseding note.

## Consequences
- (+) Format drift between writers and readers becomes structurally impossible rather than a testing burden.
- (+) The verifier is provably the same logic that wrote the file, which is exactly the claim a court needs.
- (+) Browser-side verification comes free.
- (−) `Evidence` becomes the most safety-critical library in the system; it needs the strictest review, the best test coverage, and a clean licensing boundary because it ships open source while the client does not.
- (−) Platform-neutral means no hardware-accelerated crypto shortcuts that only exist on one platform; measure before optimizing.

## Alternatives Considered
- **Per-host implementations**: conventional, and it puts the integrity guarantee at the mercy of four codebases staying in sync.
- **A C library via P/Invoke**: one implementation with genuine portability, at the cost of native build and distribution complexity in every host — including the browser — and it would forfeit the WASM reuse that makes browser verification cheap.
