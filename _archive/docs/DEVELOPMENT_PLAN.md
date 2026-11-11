# Personal Recorder — Development Plan (All-C#/.NET Stack)

**Version:** 0.2 (Ratified)
**Date:** July 31, 2026
**Status:** Accepted — decisions D1–D9 ratified 2026-07-31 (see §10.1); supersedes TECHNICAL_SPECIFICATION.md §4.2 (client stack) and §10 (roadmap)
**Author:** Drafted by Claude (Fable 5) for review and consensus; all decisions below are *proposed*, to be ratified as ADRs

---

## 1. Summary

This plan replaces the React Native client direction with an **all-C#/.NET stack**, per the revised project direction. The headline recommendations:

| Layer | Recommendation | Confidence |
|-------|---------------|------------|
| Backend | ASP.NET Core on **.NET 10 LTS** (confirms prior .NET decision; bumps from .NET 8) | High |
| Mobile capture core | **Platform-native C#** via .NET for Android / .NET for iOS — framework-independent | High |
| Mobile UI shell | **.NET MAUI** (thin shell over the capture core) — **not Godot** (§3.3) | Medium-high |
| Web ("cross-browser") | **Blazor WebAssembly PWA** — dashboard, live viewing, and fallback recorder | High |
| Evidence streaming | **Encrypted chunked upload (CMAF segments), not WebRTC** — WebRTC deferred to conferencing | High |
| Distribution | Multi-channel from day 1: direct APK + in-app updater, own F-Droid repo, stores in parallel (§6) | High |

Two 2026 landscape facts materially shape this plan:

1. **Google's developer verification program** (announced Aug 2025) will require sideloaded apps on certified Android devices to be signed by a Google-verified developer — enforcement begins in Brazil/Indonesia/Singapore/Thailand in **September 2026** and expands **globally in 2027**. "Just distribute an APK" is no longer a complete censorship-resistance strategy; §6 addresses this directly.
2. **iOS side-loading remains unavailable in the US.** EU (DMA) and now Japan permit alternative marketplaces and web distribution, but Apple notarization still gates every install. The US strategy must be: compliant App Store listing + TestFlight + PWA fallback.

The single most important structural decision in this plan: **the recording engine ("capture core") is built as framework-independent, platform-native C# libraries.** The UI shell — whatever we choose — is a thin, replaceable layer. This de-risks the UI framework choice almost completely: if MAUI disappoints, we swap shells, not the product.

---

## 2. Guiding Principles

1. **Evidence first.** Durability and integrity beat latency. No silent failures (REL-002). A dropped frame that was in the buffer is a product failure, not a glitch.
2. **The capture core is the product; the UI is replaceable.** All camera, buffer, encryption, sync, and emergency logic lives in framework-free C# libraries with platform-specific implementations behind interfaces.
3. **E2EE by default; the server is untrusted with content.** Every architectural choice must survive the question: "what happens when the storage provider is subpoenaed or pressured?"
4. **Distribution resilience is a feature and is engineered like one** — update channels, signing-key custody, multiple install paths, channel-specific feature flags. It gets roadmap time, not an afterthought.
5. **Respect platform truths.** iOS will not record camera video in the background, period (§4.4). We design UX around OS constraints instead of fighting them and shipping something fragile.
6. **Lean operations.** MVP deploys as a single node (PostgreSQL + MinIO + one ASP.NET Core process). Every added infrastructure component must pay rent. This also keeps the self-hosting option (spec Q2) real.
7. **Consensus via ADR.** This plan proposes; ADRs dispose. Nothing below is final until ratified. The existing ADR index (docs/adr/README.md) lists ADR-001 React Native / ADR-002 Fastify / ADR-003 pnpm as "Accepted" — those files were never written and the decisions are superseded; the index has been reset accordingly (D8, ratified 2026-07-31).

---

## 3. Technology Decisions (Proposed ADRs 001–007, restarting the index)

### 3.1 ADR-001 (proposed): .NET 10 LTS across the stack

.NET 10 shipped November 2025 as the current LTS (support into 2028). All projects target `net10.0` / `net10.0-android` / `net10.0-ios`. MAUI 10 was an explicitly quality-focused release, which is what we want from a UI framework we treat as a thin shell. Single-version policy: the whole solution moves LTS-to-LTS together.

### 3.2 ADR-002 (proposed): Backend — ASP.NET Core, minimal infrastructure

Confirms the earlier .NET-backend pivot (which the stale ADR index never captured) with a leaner MVP footprint than spec §4.3:

| Concern | MVP choice | Scale path (deferred until needed) |
|---------|-----------|-----------------------------------|
| API | ASP.NET Core Minimal APIs + OpenAPI | — |
| Real-time | SignalR (heartbeat, presence, alerts, live-view signaling) | Redis backplane when multi-node |
| Data | EF Core + Npgsql (PostgreSQL 17) | Read replicas; partitioning for location tracks |
| Media store | S3 API via MinIO (dev + self-host) or any S3-compatible provider (prod) | Storage tiering (STR-004) via lifecycle rules |
| Background work | .NET hosted services + PostgreSQL-backed outbox/queue | MassTransit + RabbitMQ when justified |
| AuthN | ASP.NET Core Identity + JWT; TOTP 2FA; passkeys later | — |
| Cache | None (PostgreSQL is fast) | Redis when measured need appears |

Rationale for trimming Redis/RabbitMQ/MassTransit from MVP: every component is an operational liability for a small team running a service people's safety depends on. PostgreSQL alone can carry queues, pub/sub (LISTEN/NOTIFY), and cache duty at MVP scale, and a single `docker compose up` self-host story is itself a censorship-resistance feature.

Vendor neutrality is a hard requirement: S3 *API* (not AWS-specific SDK usage patterns), SMTP for mail, and push via multiple channels (§3.7) — so the whole backend can move providers or on-prem under pressure.

### 3.3 ADR-003 (proposed): Client — native C# capture core + .NET MAUI shell (the Godot question)

#### What the client layer actually has to do

The make-or-break client requirements are not UI requirements. They are platform-service integration:

| Requirement | What it needs from the platform |
|---|---|
| Camera pre-warm + rolling buffer (REC-003/004, PERF-001) | CameraX/Camera2 + MediaCodec (Android); AVCaptureSession + AVAssetWriter (iOS). Hardware H.264/H.265 encoding into an in-memory/encrypted ring of segments |
| Background recording (PERF-004) | Android foreground service with `camera`/`microphone`/`location` service types; iOS background *audio* mode (video impossible — §4.4) |
| Panic pipeline < 2s (PERF-002) | Instant transition from warm buffer to upload; notifications; `tel:` intent overlay |
| Encrypted local store (STR-008) | Hardware keystore / Secure Enclave key custody; libsodium for content crypto |
| Location (LOC-*) | Fused location provider / CoreLocation, background modes |
| Push/alerts (COT-007, HB-003) | FCM + UnifiedPush (Android), APNs (iOS) |
| Maps, video playback, a11y | Native map views, ExoPlayer/AVPlayer, platform accessibility trees |

**No cross-platform framework — not Godot, not MAUI, not React Native — provides any of the first four off the shelf.** They must be written against platform APIs regardless. In the .NET world, .NET for Android and .NET for iOS provide complete, first-party C# bindings to those APIs (CameraX, MediaCodec, foreground services, Keystore, AVFoundation, AVAssetWriter, Keychain). So the plan is:

> **Build the capture core as platform-native C# class libraries (`net10.0-android`, `net10.0-ios`) behind shared interfaces. Choose the UI shell separately, on UI merits alone.**

This is also the answer to "which framework?" being a lower-stakes decision than it appears: the shell consumes `IRecordingEngine`, `IPanicPipeline`, `ILocationTracker` — it never touches a camera API.

#### Godot assessment (honest)

I recommend **against** Godot for the phone client. Reasons, in order of severity:

1. **Export maturity for C# is disqualifying for a life-safety app.** As of Godot 4.x today, C#/.NET exports are: desktop **stable**; Android **experimental** (since 4.2, arm64/x64 only); iOS **experimental** (macOS-only export, Xcode required); **web: not supported at all**. Shipping a panic button for detained users on an *experimental* runtime export is a risk we cannot justify, and "no C# web export" kills the cross-browser reuse story outright.
2. **Godot adds a second integration layer to the hardest 20%.** Platform features in Godot require Godot-specific Android (Java/Kotlin) and iOS plugins, bridged through the engine's plugin system — which is separate from, and would duplicate, our .NET capture core. We would literally write the platform bridge twice, or tunnel the .NET core through Godot's C# embedding in ways nobody else does (no community to lean on when it breaks).
3. **The costs are real and the benefits go unused.** Costs: continuous render loop battery draw (low-processor mode mitigates but doesn't eliminate — a concern for an app whose spec has a battery section); camera preview requires GPU texture-bridging frames into the engine rather than embedding a native preview view; no native map component; accessibility (AccessKit) only landed in Godot 4.5 and is unproven on mobile — and accessibility is core for the seniors/DV-survivor user populations. Benefits we'd actually use: none — this app has no scene graph, physics, shaders, or game loop. Godot is a fine engine; this just isn't an engine-shaped app.

Where Godot *would* be defensible: a desktop-only companion app with a fully custom UI. But desktop is covered more cheaply by the Blazor PWA (§3.4), so nothing in the system falls to Godot.

#### UI shell comparison

| Criterion | **.NET MAUI** | **Uno Platform** | **Avalonia** | **Godot + C#** | **Per-platform native C# UI** |
|---|---|---|---|---|---|
| C# on Android/iOS | First-party, stable | Stable (rides .NET SDKs) | Stable-ish mobile | **Experimental exports** | First-party, stable |
| UI paradigm | Native controls | Skia or native | Skia-drawn | Engine-drawn | Native controls |
| Embed native camera preview | Handler → native view (straightforward) | Native view embedding supported | Texture bridging (awkward) | Texture bridging (awkward) | Trivial |
| Accessibility | Native a11y trees; .NET 10 improved | Good | Weaker on mobile | New (4.5), unproven mobile | Best |
| Maps / media playback | Native views via handlers | Native embedding | DIY | DIY | Trivial |
| Battery model | Native (event-driven UI) | Native-ish | Render-on-demand | Render loop (mitigated) | Native |
| Browser reuse | Via Blazor Hybrid (shared Razor components) | **Same XAML → WebAssembly** | WASM (less mature) | **None for C#** | None |
| Ecosystem/longevity risk | Microsoft first-party; mixed community sentiment, .NET 10 quality push | Smaller vendor, active | OSS, desktop-centric | Engine community ≠ app-dev community | Zero framework risk, 2× UI cost |
| Fit verdict | **Recommended default** | Strong alternative | Not for mobile here | **Not recommended** | Escape hatch |

**Recommendation: .NET MAUI shell**, with two explicit hedges baked in:

- The capture core and all view-models live in framework-free projects, so a pivot to Uno or per-platform native UI is a shell rewrite only (weeks, not months).
- Phase 0 includes a timeboxed shell spike with hard exit criteria (§8, Spike C). If MAUI fails the spike (camera preview embedding, cold start, a11y audit, APK size), Uno is the pre-agreed fallback; per-platform native C# UI is the final fallback.

A note on the original ADR-001 (React Native): its stated rationale was "TypeScript aligns with full-stack JavaScript experience." That premise was the assistant's assumption, not your actual profile — your stated depth is .NET, and you have an explicitly low opinion of React's API direction. The human-factors argument that picked React Native actually picks C# when applied to the real team.

### 3.4 ADR-004 (proposed): Web — Blazor WebAssembly PWA

The "appropriate cross-browser technology" for an all-C# stack is Blazor WASM, delivered as an installable PWA:

- **Dashboard**: artifact browser with tag filtering, circle management, retention/pin review, exports (spec Phase 1 web deliverables).
- **Emergency console**: circle members open a link from an alert and watch a member's live feed + location — this must work on any browser with zero install (the "grandmother on a Chromebook" test), i.e., Chrome/Edge/Firefox/Safari, desktop and mobile.
- **Playback with E2EE**: encrypted CMAF segments fetched over HTTPS, decrypted client-side (WebCrypto/libsodium.js), fed to Media Source Extensions. Safari support notes: MSE on iPhone requires iOS 17.1+ (Managed Media Source); acceptable floor for 2026.
- **Fallback recorder** (the unblockable channel): `getUserMedia` + MediaRecorder capture with chunked encrypted upload. Foreground-only, no lookback buffer prewarm parity, storage-eviction caveats — clearly labeled "degraded mode," but it means **no platform owner can fully de-platform the core function**. MediaRecorder is supported in Safari 14.1+, Chrome, Firefox, Edge.
- Small amounts of JS interop (MediaRecorder/MSE glue) are expected and fine; they are shims, not application logic.

### 3.5 ADR-005 (proposed): Evidence pipeline — encrypted chunked upload, not WebRTC

This **revises spec §4.1's WebRTC-SFU-first architecture** and is the most significant technical proposal in this plan.

**Design:** The capture core hardware-encodes into ~2-second fMP4 (CMAF) segments. Each segment is AES-256-GCM encrypted client-side with a per-recording key, appended to a SHA-256 hash chain, periodically signed (Ed25519), written to the encrypted local ring buffer, and uploaded over HTTPS via a resumable, backpressure-aware queue (tus-style offsets). The server is a dumb encrypted-blob store plus metadata.

| | Chunked encrypted upload | WebRTC to SFU |
|---|---|---|
| Delivery guarantee | Lossless, resumable, offline-tolerant — every buffered frame eventually lands (PERF-003, REC-006/007 fall out for free) | Lossy by design (fine for calls, wrong for evidence) |
| E2EE | Trivial — server never needs plaintext | Needs insertable-streams gymnastics; SFU sees packet timing/sizes |
| Infra | Plain HTTPS + object storage; CDN-able; self-host trivial | SFU cluster, TURN servers, UDP egress (blocked on many networks) |
| Tamper evidence | Natural per-segment hash chain (SEC-004) | Bolt-on |
| Latency to circle viewers | ~3–6 s (segment pull, LL-HLS-style playlist) | Sub-second |

The one thing WebRTC wins is sub-second latency, which matters for **conferencing** (COM-003) — a Phase 4 feature — and not for evidence preservation or "is my person okay" live viewing, where 3–6 seconds is fine. When conferencing arrives, we deploy **LiveKit** (self-hostable SFU; token minting is plain JWT from .NET) rather than building an SFU.

Live viewing for the circle: members fetch the same encrypted segments via a rolling playlist and decrypt client-side — zero additional server compute, E2EE preserved end-to-end, works in the Blazor console via MSE and in native clients via ExoPlayer/AVPlayer custom data sources.

### 3.6 ADR-006 (proposed): E2EE key model

- Content crypto via **libsodium** (mature .NET bindings; same primitives across mobile/web/server tooling).
- Per-user X25519 identity keypair; private key material protected by OS keystore/Secure Enclave-backed wrapping where hardware supports it.
- Per-recording symmetric DEK; wrapped (sealed box) to the owner and to each authorized circle member/device. Circle sharing = wrapping keys, not re-encrypting content.
- **Recovery key escrow ON by default** (printable/exportable recovery code; server stores only a wrapped blob it cannot open) with an explicit **zero-knowledge hard mode** opt-out for users who accept unrecoverable-loss risk (spec §9.2 flips: escrowed-recovery is default, ZK is the option). Evidence that dies with a confiscated phone and a forgotten password serves no one — but the choice is the user's. **Needs your call: §10 D5.**
- Group messaging E2EE (Phase 2 text, Phase 4 conferencing): start with sealed-box fan-out; evaluate MLS (RFC 9420) when group messaging volume justifies it.

### 3.7 ADR-007 (proposed): Alert delivery redundancy

Panic alerts (EMR-004, COT-007) must not have a single deliverability chokepoint — Google/Apple push infrastructure is itself a pressure point in our threat model:

1. FCM (standard Android) and APNs (iOS — no alternative exists; note the dependency honestly).
2. **UnifiedPush** (e.g., self-hostable ntfy) for de-Googled devices — first-class, not an afterthought, because our most at-risk users disproportionately run GrapheneOS/CalyxOS.
3. SignalR direct delivery whenever any circle member's app is alive.
4. Optional SMS bridge (Twilio or pluggable) as paid last-resort fan-out.

---

## 4. Client Architecture

### 4.1 Layering

```
┌────────────────────────────────────────────────────────────┐
│  UI SHELL (.NET MAUI)                     ← replaceable    │
│  Screens, navigation, theming, big-red-button UX           │
├────────────────────────────────────────────────────────────┤
│  Client.Core (net10.0 — framework-free)                    │
│  View-models · state machines · sync queue · crypto        │
│  API/SignalR clients · panic pipeline orchestration        │
├────────────────────────────────────────────────────────────┤
│  Capture.Abstractions (net10.0)                            │
│  IRecordingEngine · ICaptureBuffer · ILocationTracker      │
│  IPanicPipeline · ISecureStore · IPushChannel              │
├──────────────────────────┬─────────────────────────────────┤
│  Capture.Android         │  Capture.Ios                    │
│  (net10.0-android)       │  (net10.0-ios)                  │
│  CameraX/Camera2         │  AVCaptureSession               │
│  MediaCodec → CMAF ring  │  AVAssetWriter → CMAF ring      │
│  Foreground services     │  Background audio mode          │
│  Keystore · FusedLocation│  Keychain/Enclave · CoreLocation│
└──────────────────────────┴─────────────────────────────────┘
```

A tiny headless **Android test-harness app** (plain .NET for Android, no MAUI) exercises the capture core directly — fast iteration on the hard parts without UI-framework variables, and it doubles as the soak-test vehicle (§8 Spike A).

### 4.2 Camera state machine (spec §5.2.1, mapped to APIs)

| State | Android | iOS | Power |
|---|---|---|---|
| COLD | Camera closed | Session not running | ~0 |
| WARM | CameraX bound, preview 10fps, no encoder | Session running, no writer | Low |
| HOT | Encoder running → encrypted ring buffer (10s–5min) | Writer running → ring buffer | Medium |
| RECORDING | Ring flushed to upload queue + continuous segment upload | Same (foreground only) | Full |

PERF-001 (<500 ms to recording) is measured WARM→RECORDING and enforced as an automated device test, not a hope.

### 4.3 Sync & offline

All artifacts (segments, snapshots, notes, tags, location tracks) flow through one persistent, encrypted, prioritized upload queue (SQLite-backed): panic-triggered segments > live segments > snapshots/notes > backfill. Queue survives process death; resumes on connectivity (REC-006/007). Every artifact carries its hash-chain position so the server can attest completeness without reading content.

### 4.4 Platform truths (design around, don't fight)

| Capability | Android | iOS |
|---|---|---|
| Record video, app backgrounded/screen off | ✅ Foreground service (`camera` type; Android 14+ while-in-use rules; battery-optimization exemption prompt in onboarding) | ❌ OS-level impossible. UX: "podium mode" — screen dimmed to black, app foreground, recording continues; or audio-only |
| Record audio in background | ✅ Foreground service (`microphone` type) | ✅ Background audio mode |
| Rolling buffer while "HOT" | ✅ | ✅ foreground only |
| Silent/stealth UI (PRV-003/004) | ✅ in sideloaded build; **feature-flagged OFF in Play build** (store policy risk) | Store-reviewed; keep minimal |
| Auto-answer/auto-dial | `tel:` intent overlay (one tap) — as spec §7.2 | Same via `tel:` URL |

This table is why iOS ships in Phase 2 rather than Phase 1: Android delivers the full vision; iOS delivers a constrained-but-honest version that needs its own UX pass.

---

## 5. Solution & Repository Structure

Top-down, one solution, respecting the established `components/` convention:

```
personal-recorder/
├── PersonalRecorder.sln
├── components/
│   ├── contracts/
│   │   └── src/PersonalRecorder.Contracts/        # DTOs, enums, SignalR contracts, error codes
│   ├── service/
│   │   ├── src/
│   │   │   ├── PersonalRecorder.Service.Api/            # Host: REST + SignalR + OpenAPI
│   │   │   ├── PersonalRecorder.Service.Domain/         # Entities, invariants, domain events
│   │   │   ├── PersonalRecorder.Service.Application/    # Use cases, ports
│   │   │   ├── PersonalRecorder.Service.Infrastructure/ # EF Core, S3/MinIO, outbox, push
│   │   │   └── PersonalRecorder.Service.Workers/        # Retention, heartbeat monitor, alert dispatch
│   │   └── test/                                        # Unit · Integration (Testcontainers) · Architecture
│   ├── client/
│   │   ├── src/
│   │   │   ├── PersonalRecorder.Client.Core/
│   │   │   ├── PersonalRecorder.Client.Capture.Abstractions/
│   │   │   ├── PersonalRecorder.Client.Capture.Android/
│   │   │   ├── PersonalRecorder.Client.Capture.Ios/
│   │   │   ├── PersonalRecorder.Client.App/             # MAUI shell
│   │   │   └── PersonalRecorder.Client.AndroidHarness/  # Headless capture-core test app
│   │   └── test/
│   ├── web/
│   │   └── src/PersonalRecorder.Web.Dashboard/          # Blazor WASM PWA
│   └── verifier/
│       └── src/PersonalRecorder.Verifier/               # Open-source CLI: hash-chain/signature verification (§7)
├── tools/                                               # docker-compose.dev.yml, build/signing scripts
├── design/  ·  docs/ (adr/)  ·  TECHNICAL_SPECIFICATION.md
```

Notes:
- **Contracts become C#** (shared by service, clients, web). The README's "TypeScript types generated from .NET" contract flow becomes unnecessary — Blazor consumes the C# contracts directly. NSwag TS generation returns only if a TS surface ever appears (e.g., the JS shims stay too small to need it).
- Namespace root `PersonalRecorder.*` pending the naming decision (spec Q6). Locking the working name **before** Phase 1 code volume is strongly advised (§10 D3); a rename after Phase 0 is cheap, after Phase 2 it is not.
- Architecture tests (NetArchTest or similar) enforce the dependency rules from day 1: `Capture.* → Abstractions only`, `App → Core + Abstractions`, `Domain → nothing`.

---

## 6. Distribution & Side-Loading Strategy

The stated existential requirement: survivable distribution under political pressure on app stores. The 2026 landscape, verified this week:

### 6.1 Landscape

- **Android — Google developer verification**: registration opened to all developers March 2026; certified-device enforcement begins **September 2026 (BR/ID/SG/TH)**, **global 2027**. Sideloaded apps on Google-certified devices will require a Google-verified developer signature. This is identity verification, not app review — but it creates a revocation capability we must plan around. F-Droid has publicly warned this threatens its model.
- **iOS**: US — no side-loading; App Store/TestFlight only. EU — DMA alternative marketplaces (e.g., AltStore PAL, Epic) and web distribution, all still Apple-notarized. Japan — alternative stores began appearing Feb 2026 under the new competition law. Expect, but do not depend on, US movement.
- **Desktop/web**: unrestricted; the PWA cannot be de-platformed.

### 6.2 Channel matrix

| Channel | Priority | Notes |
|---|---|---|
| **Direct APK + in-app self-updater** | P0, day 1 | The Signal model. An APK without an update channel becomes a stale, dangerous artifact — the updater ships in the *first* public build. Update manifest + signed APKs on our domain **and** mirrored (GitHub Releases). |
| **Own F-Droid repository** | P0 | Our repo, our signing, no main-repo policy gate; users add one URL. Main F-Droid repo later if/when client is FOSS (D4). |
| **Obtainium support** | P1 | Just needs stable release URLs + versioning discipline. |
| **Google Play** | P1, parallel track | Compliant listing per spec §8.2 ("personal safety"); **stealth features compiled out** via channel feature-flags. Play build also satisfies developer-verification reach for mainstream users. |
| **AOSP-ROM outreach (GrapheneOS/CalyxOS users)** | P1 | Unaffected by Google verification; disproportionately our audience. Works via F-Droid repo/Obtainium. |
| **iOS App Store + TestFlight** | P1 (Phase 2) | Safety positioning; expect review friction on background/stealth behaviors — keep iOS build store-clean. |
| **EU/Japan alternative distribution** | P2 | When volume/entity eligibility allows. |
| **Blazor PWA** | P0 | The unblockable floor: recording (degraded), viewing, emergency console, quick-dial. |

### 6.3 Actions this quarter (time-sensitive)

1. **Register the publishing entity and complete Google developer verification now** (open since March 2026) — before the Sept 2026 enforcement wave. Entity decided 2026-07-31: **NimbleJinn LLC (New Hampshire, formation in progress)**. Note: Google Play Console organization accounts require a D-U-N-S number — request it as soon as the LLC exists (free, but lead time can run weeks).
2. **Signing-key ceremony**: generate Android signing keys offline; publish SHA-256 cert fingerprints on the website and in docs so users can verify APKs (`apksigner verify --print-certs`); document custody (who, where, hardware token).
3. Stand up the update-manifest endpoint + mirror scheme early (it's ~a static JSON + APKs; trivial, but it must exist before the first user installs).
4. **Reproducible builds** as a stated goal (F-Droid main repo prerequisite; major trust signal for this audience) — target Phase 3.

### 6.4 Channel-specific feature flags

One codebase, per-channel compile flags: `SIDELOAD` (full features incl. stealth/silent panic), `PLAY` / `APPSTORE` (stealth off, store-safe), `FDROID` (FOSS-only dependencies when that path activates). The capture core is identical everywhere; only surface features flag.

### 6.5 Open-source posture (recommendation for spec Q1)

For this audience — people pointing a recording app at their own government — **"trust me" does not scale; the client must be open source** (auditable: "is this spyware?" must be answerable by third parties). Recommendation: client + verifier FOSS from Phase 1 (license TBD — AGPL vs Apache trade-off worth its own ADR discussion); server core-OSS per the spec's existing recommendation (also makes self-hosting real). **Decided 2026-07-31 (D4): deferred — proprietary initially**, to avoid copyleft lock-in concerns. Revisit around Phase 3; note that open-sourcing would not require copyleft — a permissive license (MIT/Apache) with permissively-licensed dependencies remains available then. Consequences now: the main-F-Droid-repo channel is shelved (our *own* F-Droid repo is unaffected — it can host proprietary APKs); the trust burden shifts to the open verifier CLI, reproducible builds, and published signing fingerprints. The verifier stays FOSS regardless.

---

## 7. Security & Integrity Additions

- **Tamper-evident chain** (SEC-004, spec §9.3), made concrete: per-segment SHA-256 chained hashes; chain head signed with the user's Ed25519 key every N segments and at recording end; server countersigns+timestamps received chain heads (proof-of-existence-by-time without content access). Consider RFC 3161 TSA or transparency-log anchoring in Phase 4 for third-party timestamp proof.
- **Open verifier CLI** (`PersonalRecorder.Verifier`): given an exported recording + chain manifest, verifies completeness, integrity, timestamps, and signer — *independently of us*. This is what makes the evidentiary story real to a court or journalist: verification requires no trust in our servers. FOSS regardless of D4.
- **Threat-model addendum** (new doc, Phase 0): adds *platform coercion* (store delisting, push revocation, developer-verification revocation, cloud-provider termination, domain seizure) with per-threat mitigations mapping to §6; and *server compromise/legal compulsion* (E2EE limits blast radius to metadata — be honest in docs that traffic metadata, circle graphs, and location-track *existence* are visible to the server unless/until we add padding/oblivious techniques; do not oversell).
- Standard hygiene: TLS 1.3 + certificate pinning (sideloaded builds can pin hard; store builds pin with backup pins), dependency audit in CI, secrets never in repo, external security audit gate before Phase 3 exit (spec already commits to this).

---

## 8. Phases

Estimates assume ~1 senior engineer full-time equivalent with heavy AI pairing; scale calendar time accordingly. Each phase has explicit exit criteria; the spikes exist to kill the riskiest unknowns first.

### Phase 0 — Foundations & de-risking (3–4 weeks)

- Ratify ADRs 001–007 (§3) + naming (D3) + entity/verification kickoff (§6.3).
- Scaffold solution per §5 (top-down: `.sln` and project skeletons first, together — per your stated preference).
- CI/CD: GitHub Actions — build, test, Android APK artifact + signing, Blazor publish; `tools/docker-compose.dev.yml` (PostgreSQL + MinIO).
- **Spike A (existential): Android capture core.** CameraX WARM/HOT states; MediaCodec → 2s encrypted CMAF segments into ring buffer; foreground service; 4-hour soak on 2 physical devices (one flagship, one budget) measuring battery/thermal/frame integrity. **Exit: WARM→RECORDING < 500 ms; zero buffer frame loss; soak survives screen-off; battery burn measured and written down.**
- **Spike B: walking skeleton.** Harness app records → encrypts → chunk-uploads to MinIO via API → Blazor page lists, decrypts, plays via MSE. **Exit: E2EE round trip works in Chrome + Safari.**
- **Spike C (timeboxed 1 week): MAUI shell slice.** Camera preview embedding over capture core, panic screen, cold-start time, APK size, screen-reader pass. **Exit: pass → MAUI confirmed; fail → Uno spike week, then decide.**

### Phase 1 — MVP: Android + Web (10–12 weeks)

Maps spec Phase 1 to the new stack: auth (Identity + JWT + TOTP); video/audio/snapshot capture with lookback buffer; notes + tags (system + custom + smart sort); panic button + 911 quick-dial overlay + single-circle alerts (SignalR + FCM + UnifiedPush); location tracking + metadata attachment; retention windows, pinning, auto-disable safety net; Blazor dashboard (artifact browser, tag filtering, playback, exports); **distribution v1: direct APK + self-updater + own F-Droid repo + published key fingerprints**. Beta with a small trusted cohort ("circle of first users") starts week ~8.

### Phase 2 — iOS + Circles (8–10 weeks)

iOS capture core (AVFoundation; podium-mode UX; audio-only background); TestFlight channel; multiple circles + permission levels; live-view (encrypted segment streaming) in Blazor console + native viewers; heartbeat/check-in with escalation (HB-001..005) — server-side monitor in Workers; basic in-circle messaging.

### Phase 3 — Hardening (6–8 weeks)

Offline/sync robustness under hostile networks (airplane-mode storms, captive portals); storage tiering + archival + monthly review flow; push redundancy completion; verifier CLI 1.0 + tamper-chain countersigning; observability (OpenTelemetry, alerting on REL-002 silent-failure detectors); load/chaos tests; **external security audit + fixes**; reproducible builds; App Store submission attempt with compliant build.

### Phase 4+ — Advanced (ongoing)

Conferencing via LiveKit (E2EE where feasible); geofencing; zero-knowledge hard mode; voice activation (on-device); transcription (**on-device only** — server-side transcription would break E2EE; e.g., Whisper-class models via ONNX Runtime); org accounts + white-label; desktop capture app if demand (Uno/Avalonia then); dashcam/home-security SDK per spec Phase 5.

---

## 9. Risk Register

| Risk | L×I | Mitigation |
|---|---|---|
| Google developer-verification revocation as pressure vector | M×H | Verify early under a considered entity (D9/Q3); own F-Droid repo + AOSP-ROM path (unaffected devices); PWA floor; documented `adb` path for advanced users; monitor F-Droid/regulatory pushback |
| Apple App Store rejection/removal | H×M | Store-clean build, safety positioning; TestFlight; EU/Japan channels; PWA floor; iOS is *a* channel, not *the* channel |
| iOS background-video limitation disappoints users | H×M | Honest UX (podium mode, audio-only background); set expectations in marketing; Android-first messaging for the primary observer use case |
| Battery burn makes HOT mode impractical | M×H | Spike A measures before we promise; adaptive quality ladder (spec §5.2.2); hardware encoder settings tuned per device class |
| MAUI quality issues | M×M | Thin shell over framework-free core; pre-agreed Uno fallback; per-platform native UI as final fallback |
| Solo bus-factor | H×H | Everything as code + ADRs; boring infrastructure; FOSS client invites contributors; document the signing/key ceremonies especially |
| E2EE key loss = evidence loss | M×H | Recovery-key escrow default ON (D5); aggressive UX for recovery-code saving; multi-device key sync in Phase 2 |
| Legal exposure (consent laws, entity jurisdiction) | M×H | Jurisdiction-based consent defaults (spec Q4); entity + counsel before public beta (Q3/Q5); we ship no legal advice, link Know-Your-Rights resources |
| Camera fragmentation on Android | H×M | CameraX (not raw Camera2) as baseline; device test matrix (flagship/budget/Samsung/Pixel); harness app in CI device farm later |
| Scope creep (this spec is *large*) | H×M | Phase gates + ADR discipline; MVP cut is Phase 1 list, nothing more |

---

## 10. Decisions Needed From You

| # | Decision | Options | My recommendation |
|---|---|---|---|
| D1 | Mobile UI shell | MAUI / Uno / Godot / per-platform native | **MAUI** with Spike C gate + Uno fallback (§3.3) |
| D2 | Evidence pipeline | Chunked E2EE upload / WebRTC-first | **Chunked** (§3.5) |
| D3 | Working name & namespace root | Lock "Personal Recorder" → `PersonalRecorder.*` / pick final name now (design/NAMING.md list) | Lock a name **before Phase 1**; namespace-rename after Phase 0 is cheap, later is not |
| D4 | Open-source posture | Client FOSS now / later / never; license | **Client + verifier FOSS from Phase 1**; server core-OSS (§6.5) |
| D5 | Recovery-key escrow default | ON with ZK opt-out / OFF (pure ZK) | **ON** (§3.6) — evidence recoverability beats purity, user keeps the choice |
| D6 | iOS timing | Phase 2 (plan) / pull into Phase 1 | **Phase 2** — Android+PWA covers all users at MVP without doubling Phase 1 |
| D7 | Hosting | EU-based VPS (e.g., Hetzner) / US cloud / hybrid | Worth a jurisdiction discussion alongside Q3; architecture is provider-neutral either way |
| D8 | Repo housekeeping | Reset stale ADR index; update README (RN → C# stack) and spec §4.2/§8/§10 after ADR ratification | Yes, as Phase 0 first PR — **not done yet; awaiting your OK** |
| D9 | Entity + Google developer verification | Start now / defer | **Start now** — Sept 2026 enforcement is close, and identity choice (Q3) gates it |

### 10.1 Ratified outcomes (2026-07-31)

| # | Outcome |
|---|---------|
| D1 | **MAUI shell** confirmed, with the Spike C gate and Uno fallback |
| D2 | **Chunked E2EE upload** confirmed |
| D3 | Working name locked: **PersonalRecorder** (`PersonalRecorder.*` namespaces, `PersonalRecorder.sln`) |
| D4 | Open-sourcing **deferred** (proprietary initially; copyleft lock-in concern) — see §6.5; verifier CLI stays FOSS |
| D5 | Recovery-key escrow **ON by default**, zero-knowledge opt-out |
| D6 | iOS in **Phase 2**. Confidence basis: .NET for iOS is first-party and stable (decade-plus Xamarin.iOS lineage in production), and the capture core uses the same AVFoundation APIs every iOS camera app uses — the residual iOS risk is App Store *policy*, not technology; mitigated by the store-clean build, TestFlight, and the PWA floor |
| D7 | Hosting jurisdiction **deferred** (legal question; architecture stays provider-neutral either way) |
| D8 | Repo housekeeping approved and executed: ADRs 001–007 authored, ADR index reset, README and spec §4/§8/§10/§11 updated |
| D9 | Entity: (New Hampshire, formation in progress)**; Google developer verification to follow immediately after formation |

---

## 11. Sources (landscape verification, July 2026)

- Godot C# platform state: [godotengine.org — Current state of C# platform support in Godot 4.2](https://godotengine.org/article/platform-state-in-csharp-for-godot-4-2/) · [Godot Forum — C# web export status](https://forum.godotengine.org/t/is-there-an-update-on-exporting-c-projects-to-web/128821) · [Godot Forum — iOS C#/mono maturity](https://forum.godotengine.org/t/is-the-ios-support-good-enough-when-using-the-mono-version-for-a-full-release/128950)
- Google developer verification: [F-Droid Forum — Google will require developer verification to install Android apps](https://forum.f-droid.org/t/google-will-require-developer-verification-to-install-android-apps-including-sideloading/33123) · [Hackaday — Developer verification even for sideloading](https://hackaday.com/2025/08/26/google-will-require-developer-verification-even-for-sideloading/) · [Cybernews — Verify identity or lose sideloading](https://cybernews.com/tech/google-android-developer-verify-identity-lose-sideloading/)
- iOS distribution: [TechCrunch — Alternative app stores in the EU and elsewhere (Feb 2026)](https://techcrunch.com/2026/02/22/move-over-apple-meet-the-alternative-app-stores-available-in-the-eu-and-elsewhere/) · [doesioshavesideloadingyet.com](https://doesioshavesideloadingyet.com/)
- .NET/MAUI: [Microsoft — .NET MAUI support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/maui) · [Microsoft Learn — What's new in .NET MAUI for .NET 10](https://learn.microsoft.com/en-us/dotnet/maui/whats-new/dotnet-10?view=net-maui-10.0)

---

*Ratified 2026-07-31; ADRs 001–007 authored. Next: Phase 0 — solution scaffold built together top-down, then Spikes A–C.*
