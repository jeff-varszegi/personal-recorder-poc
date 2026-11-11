# ADR-002: Native C# Capture Core with a Thin .NET MAUI Shell

## Status
Accepted

## Context
The make-or-break client requirements are platform-service integrations, not UI: camera pre-warm with a rolling encrypted lookback buffer and hardware encoding (`REC-003/004`, `PERF-001/003`), background recording via foreground services (`PERF-004`), keystore-backed local encryption (`STR-008`), a sub-two-second panic pipeline (`PERF-002`), and background location (`LOC-*`).

**No cross-platform framework provides any of those off the shelf** — they are platform-API work in every stack and every language. What a framework choice actually determines is how directly we can reach CameraX, MediaCodec, AVCaptureSession, AVAssetWriter, Keystore and the Secure Enclave, and how much runtime maturity sits between our code and the hardware.

Godot was specifically proposed and evaluated, since the engine supports C# and produces ordinary side-loadable APKs.

## Decision
1. **Capture core**: framework-independent, platform-native C# libraries — `Client.Capture.Android` (`net10.0-android`: CameraX/Camera2, MediaCodec → encrypted CMAF ring, foreground services, Keystore, fused location) and `Client.Capture.Ios` (`net10.0-ios`: AVCaptureSession, AVAssetWriter, Keychain/Secure Enclave, CoreLocation), both behind shared interfaces in `Capture.Abstractions`. All view-models, state machines, sync and crypto live in framework-free `Client.Core`.
2. **UI shell**: **.NET MAUI**, kept deliberately thin. The shell consumes `IRecordingEngine`, `IPanicPipeline`, `ICaptureBuffer` and never touches a camera API. Gate: Phase 0 Spike C (camera-preview embedding, cold start, APK size, screen-reader pass), with **Uno Platform as the pre-agreed fallback** and per-platform native C# UI as the final fallback.
3. Architecture tests enforce the boundary from the first commit: capture projects may not reference a UI framework, and framework-free projects may not acquire a platform target.

## Consequences
- (+) The riskiest code is written once, against first-party stable .NET bindings to the platform APIs.
- (+) A shell swap becomes a weeks-scale rewrite of screens rather than a months-scale rewrite of the product — the framework decision is structurally de-risked rather than merely well-argued.
- (+) One language across capture, service, web, contracts and verifier.
- (−) MAUI carries a mixed quality reputation from its early releases; mitigated by the thin-shell rule, the spike gate, and the fallback ladder.
- (−) Two capture-core implementations to maintain — inherent to the requirements, not to this decision.

## Alternatives Considered
- **Godot + C#** (specifically proposed): rejected for the phone client. As of the current stable Godot documentation, C# export to **Android and iOS remains experimental** (unchanged in status since 4.2) and **C# web export is unsupported** — a demoed prototype and a community fork, not a shipping feature. Beyond maturity: platform features route through Godot's own Java/Kotlin plugin system, which would duplicate the .NET capture bridge; and the engine imposes real costs (render-loop battery draw, GPU texture-bridging to display a camera preview, no native map control, accessibility only recently added and unproven on mobile — for user populations where accessibility is core) in exchange for engine benefits this application never uses. There is no scene graph, no physics, no game loop here. Side-loadability, the property that motivated the proposal, is a packaging concern and is equally available from every candidate.
- **Uno Platform**: strong runner-up, and the only mature C#-to-WebAssembly XAML story. Retained as the pre-agreed fallback; the browser-reuse advantage is smaller here than it looks, because the web surface's job (dashboard, console) differs sharply from the phone's (capture, panic).
- **React Native / Flutter**: same platform-integration burden, plus a language the team has no depth in.
- **Per-platform native C# UI, no framework**: maximum control, roughly double the UI effort; retained as the final fallback.
