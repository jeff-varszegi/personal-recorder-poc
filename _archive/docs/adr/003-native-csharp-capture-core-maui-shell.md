# ADR-003: Native C# Capture Core with .NET MAUI UI Shell

## Status
Accepted (2026-07-31) — supersedes the informal 2026-01-16 "React Native" decision (never authored as an ADR)

## Context
The make-or-break client requirements are platform-service integrations, not UI: camera pre-warm + rolling lookback buffer with hardware encoding, background recording via foreground services, an encrypted local segment store with keystore-backed keys, a sub-2-second panic pipeline, and background location. **No cross-platform framework provides these off the shelf** — they are platform-API work in every stack. The original React Native choice was premised on full-stack JavaScript experience the team does not have; actual team depth is .NET. Godot (C#) was proposed and evaluated as the client.

## Decision
1. **Capture core**: framework-independent, platform-native C# libraries — `PersonalRecorder.Client.Capture.Android` (`net10.0-android`: CameraX/Camera2, MediaCodec → encrypted CMAF ring buffer, foreground services, Keystore, fused location) and `PersonalRecorder.Client.Capture.Ios` (`net10.0-ios`: AVCaptureSession, AVAssetWriter, Keychain/Secure Enclave, CoreLocation) — behind shared interfaces in `Capture.Abstractions`. All view-models, state machines, sync, and crypto live in framework-free `Client.Core`.
2. **UI shell**: **.NET MAUI**, kept deliberately thin. Gate: Phase 0 Spike C (camera-preview embedding, cold start, APK size, screen-reader pass) with **Uno Platform as the pre-agreed fallback** and per-platform native C# UI as the final fallback.

## Consequences
- (+) The riskiest code is written once against first-party .NET platform bindings; the shell is swappable in weeks, not months — the framework choice is de-risked structurally.
- (+) One language across the entire system; leverages the team's strongest competency.
- (−) MAUI carries a mixed quality reputation; mitigated by the thin-shell rule, the spike gate, and the fallback ladder.
- (−) Two capture-core implementations to maintain (inherent to the requirements, not to this decision).

## Alternatives Considered
- **Godot + C#** (proposed): rejected for the phone client. (a) C#/.NET exports are stable on desktop but **experimental on Android and iOS, and unsupported on web** — disqualifying for a life-safety app and killing browser reuse; (b) platform features route through Godot's own Java/Kotlin plugin system, duplicating the .NET capture bridge; (c) real costs (render-loop battery, GPU texture-bridging for camera preview, no native maps, accessibility only recently added in 4.5 and unproven on mobile — critical for senior/DV-survivor users) with zero engine benefits used (no scene graph, physics, or game loop in this app).
- **React Native** (original): wrong human-factors premise; JS bridge layer adds nothing over C# bindings for this team.
- **Flutter**: same platform-integration burden plus a new language (Dart).
- **Uno Platform**: strong runner-up (same XAML to WebAssembly); retained as the pre-agreed fallback.
- **Per-platform native C# UI (no framework)**: maximum control, ~2× UI effort; retained as final fallback.
