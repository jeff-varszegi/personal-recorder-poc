# Personal Recorder — System Architecture

**Status:** **Accepted** — decisions D1–D15 ratified (see §17.1); recorded as [ADRs 001–014](./adr/).
**Entity:** NimbleJinn LLC (New Hampshire, formation in progress)
**Supersedes:** the earlier development plan and ADRs preserved in `_archive/`.

> "Evidence that cannot be deleted, confiscated, or suppressed; help that cannot be blocked or delayed."

---

## 1. Purpose of this document

This is the whole-system architecture: technology choices with current evidence, component decomposition, the evidence pipeline and its cryptography, the service design, distribution strategy, threat model, verification targets, and phasing.

The requirements baseline is [`TECHNICAL_SPECIFICATION.md` §3](../TECHNICAL_SPECIFICATION.md) (requirement IDs `REC-*`, `STR-*`, `NOT-*`, `TAG-*`, `HB-*`, `LOC-*`, `EMR-*`, `COT-*`, `COM-*`, `PRV-*`, `PERF-*`, `REL-*`, `SEC-*`). Those IDs are referenced throughout and remain authoritative for *what* we build; this document specifies *how*.

---

## 2. What the system is

A personal recording and emergency-response system for legal observers, witnesses of government activity, domestic-violence survivors, home-alone seniors, and general personal safety.

Five capabilities define it. Everything else is supporting cast:

| # | Capability | Why it is hard |
|---|---|---|
| 1 | **Lookback recording** — camera pre-warmed, rolling encrypted buffer; pressing record retroactively captures the preceding 10 s–5 min (`REC-003/004`, `PERF-001/003`) | Requires hardware-encoder access and a disk-backed encrypted ring, on both platforms |
| 2 | **Evidence that survives the device** — segments stream to storage as they are produced, so confiscation mid-incident loses nothing already sent (`REC-005/006`, `REL-002`) | Must be lossless, resumable, offline-tolerant, and end-to-end encrypted at the same time |
| 3 | **Provable integrity** — a third party can verify completeness, non-modification, authorship and time without trusting our servers (`SEC-004`) | Requires a documented format, a hash chain, signatures, and an independent verifier |
| 4 | **Help that arrives** — panic and dead-man's-switch alerts to a circle of trust, over channels no single platform owner can sever (`EMR-*`, `HB-*`, `COT-*`) | Push infrastructure is itself a coercion point in our threat model |
| 5 | **Distribution that survives pressure** — installable without app-store permission (`§12`) | The Android landscape is actively closing this door |

---

## 3. Architectural principles

These are the invariants. Any proposal that violates one needs an ADR to overturn it.

1. **Evidence first.** Durability and integrity beat latency. A frame that reached the buffer and did not reach storage is a product failure, not a glitch.
2. **The capture core is the product; the UI is replaceable.** All camera, buffer, crypto, queue, and emergency logic lives in framework-free C# with thin platform implementations. The UI shell consumes interfaces and never touches a camera API.
3. **The server is untrusted with content.** Every design must answer: *what happens when the storage provider is subpoenaed, pressured, or breached?* The answer must be "they get ciphertext and metadata we have already documented."
4. **No silent failures.** Recording is a life-safety function. Every failure is visible, logged, and surfaced to the user; watchdogs detect the absence of expected work, not just the presence of errors.
5. **Design around platform truths, do not fight them.** iOS will not record camera video in the background. We ship an honest constrained mode rather than a fragile hack.
6. **Lean operations.** MVP is one ASP.NET Core process + PostgreSQL + S3-compatible storage. Every added component pays rent. A trivially self-hostable stack *is* a censorship-resistance feature.
7. **Portability under pressure.** No vendor-specific lock-in on any critical path: S3 API (not AWS SDK idioms), SMTP, pluggable push, standard container deploy.
8. **Consensus via ADR.** Ratified decisions become ADRs in `docs/adr/`.

---

## 4. Technology decisions

### 4.1 Runtime: .NET 10 LTS, and explicitly *not* .NET 11 during MVP

All projects target `net10.0` / `net10.0-android` / `net10.0-ios`. .NET 10 is the current LTS release, with a three-year support window.

.NET 11 is a **Standard Term Support** (2-year) release, and — significantly — in .NET 11 **CoreCLR replaces Mono as the default runtime for .NET MAUI apps on Android, iOS and Mac Catalyst**. That is a large, welcome, long-term improvement and a poor thing to ride during the first shipping months of a life-safety application.

**Policy: we move LTS-to-LTS.** .NET 10 through Phases 0–2; build against .NET 11 in CI during Phase 3 hardening to stay ahead of the migration; adopt the next LTS (.NET 12) as the next production hop.

### 4.2 The client framework question: Godot, MAUI, Uno, or per-platform native

Side-loading is **not** a framework property. Godot, MAUI, Uno and per-platform native all produce an ordinary APK you can host and install yourself. So side-loadability does not discriminate between them, and neither does "is it C#" — all four are C#. What discriminates is (a) maturity of the mobile runtime, (b) how directly we can reach the platform APIs the product actually depends on, and (c) whether the browser can be served from the same investment.

#### What the client must actually do

The make-or-break client requirements are platform-service integrations, not UI:

| Requirement | Platform API needed |
|---|---|
| Camera pre-warm, rolling buffer, hardware encode (`REC-003/004`, `PERF-001/003`) | Android: CameraX/Camera2 + MediaCodec. iOS: AVCaptureSession + AVAssetWriter |
| Background recording (`PERF-004`) | Android foreground service, `camera`/`microphone`/`location` types. iOS background audio mode |
| Encrypted local store with hardware key custody (`STR-008`) | Android Keystore (StrongBox where present). iOS Keychain / Secure Enclave |
| Panic < 2 s (`PERF-002`) | Warm sockets, notifications, `tel:` intent overlay |
| Background location (`LOC-*`) | FusedLocationProvider / CoreLocation with background modes |
| Push (`COT-007`, `HB-003`) | FCM + UnifiedPush (Android), APNs (iOS) |
| Maps, media playback, accessibility | Native map views, ExoPlayer/AVPlayer, platform a11y trees |

**No cross-platform framework provides the first four off the shelf.** They are platform-API work in every stack, in every language. In .NET, *.NET for Android* and *.NET for iOS* provide complete, first-party, stable C# bindings to all of them. That is the decisive advantage of staying in C#, and it is available regardless of which UI framework sits on top.

#### Godot — honest assessment

I recommend **against** Godot for the phone client, and the reason is maturity, not taste. The current stable release is Godot 4.6, and its documentation still says:

- **Android + C#:** *"Projects written in C# can be exported to Android as of Godot 4.2, but support is experimental and some limitations apply."* Several releases after 4.2, still experimental.
- **iOS + C#:** experimental; export only from macOS with Xcode; official simulator template is x64-only.
- **Web + C#:** **not supported.** A .NET web prototype has been demoed and a community fork exists; it is not in a stable release.

Three consequences, in severity order:

1. **Shipping a panic button for detained users on a runtime export the engine itself labels experimental is not a defensible risk.** This is the whole argument. If the app fails to record during a false arrest, there is no retry.
2. **Godot would make us write the hard 20% twice.** Platform features reach Godot through its own Java/Kotlin and Objective-C plugin system, which is separate from — and would duplicate — the .NET capture core. We would either maintain a second bridge or tunnel .NET through Godot's C# embedding in a configuration essentially nobody else runs in production.
3. **We would pay engine costs for benefits we never use.** Continuous render-loop battery draw (in an app with a whole battery-management section), GPU texture-bridging to show a camera preview instead of embedding the native preview view, no native map control, and accessibility only recently added and unproven on mobile — for user populations (seniors, DV survivors) where accessibility is core. Against that: no scene graph, no physics, no shaders, no game loop. This is not an engine-shaped application.

Where Godot *would* be defensible: a desktop companion with a fully custom UI. That need is covered more cheaply by the Blazor PWA, so nothing in this system falls to Godot. If you want to keep a hand in Godot, the architecture in §5 lets you build an experimental shell against `IRecordingEngine` later without risking the product — see §4.3.

#### Shell comparison (current)

| Criterion | **.NET MAUI** | **Uno Platform** | **Godot + C#** | **Per-platform native C# UI** |
|---|---|---|---|---|
| Mobile C# status | First-party, stable; CoreCLR in .NET 11 | Stable (rides the .NET SDKs) | **Experimental exports (Android + iOS)** | First-party, stable |
| Embed native camera preview | Handler → native view | Native view embedding | GPU texture bridging | Trivial |
| Reach platform APIs | Direct | Direct | Via engine plugin layer | Direct |
| Accessibility | Native a11y trees | Good | New, unproven on mobile | Best |
| Maps / media playback | Native via handlers | Native embedding | DIY | Trivial |
| Battery model | Event-driven | Event-driven | Render loop | Event-driven |
| Browser reuse | Blazor Hybrid (shared Razor) | Same XAML → WASM | **None for C#** | None |
| Vendor risk | Microsoft first-party | Smaller vendor, active, now collaborating with the MAUI team | Engine community ≠ app-dev community | None; ~2× UI cost |
| **Verdict** | **Recommended** | Pre-agreed fallback | Not recommended | Final fallback |

**Recommendation: a deliberately thin .NET MAUI shell over a native C# capture core**, with Uno as the pre-agreed fallback and per-platform native C# UI as the final fallback. Phase 0 Spike C gates the choice with hard exit criteria (§15).

### 4.3 Why the shell choice is structurally de-risked

This is the most important structural property of the design, and it is what makes §4.2 a low-stakes decision rather than a bet:

```
The shell talks to:   IRecordingEngine · ICaptureBuffer · ILocationTracker
                      IPanicPipeline · ISecureStore · IPushChannel · IUploadQueue

The shell never talks to:  CameraX · MediaCodec · AVAssetWriter · Keystore ·
                           Secure Enclave · FusedLocation · FCM · APNs
```

Consequently a shell swap — MAUI → Uno, or MAUI → per-platform native, or an experimental Godot front end — is a **weeks-scale rewrite of screens**, not a months-scale rewrite of the product. Architecture tests enforce this boundary from the first commit (§5.3).

### 4.4 Web: Blazor WebAssembly PWA

The "any browser, zero install" surface. Four jobs:

1. **Dashboard** — artifact browser, tag filtering, retention/pin review, exports.
2. **Emergency console** — a circle member opens a link from an alert and watches the live feed and location. Must work for a non-technical person on a borrowed laptop.
3. **Playback with E2EE** — encrypted segments fetched over HTTPS and decrypted client-side, fed to Media Source Extensions (Managed Media Source on iOS Safari 17.1+).
4. **Degraded fallback recorder** — `getUserMedia` + `MediaRecorder`, chunked encrypted upload. Foreground-only, no lookback prewarm parity, storage-eviction caveats, clearly labelled. Its purpose is that **no platform owner can fully remove the core function.**

The decisive payoff of one language: **`PersonalRecorder.Evidence` — the same C# that writes and verifies the evidence format on the phone — runs unmodified in the browser via WASM and in the verifier CLI.** One canonical implementation, three hosts. Small JS interop shims (MediaRecorder, MSE) are expected and are glue, not logic.

### 4.5 Backend: ASP.NET Core, minimal infrastructure

| Concern | MVP | Scale path (deferred until measured) |
|---|---|---|
| API | ASP.NET Core controllers + OpenAPI (ADR-015) | — |
| Real-time | SignalR (heartbeat, presence, alerts, live-view notify) | Redis backplane when multi-node |
| Data | Dapper + Npgsql (PostgreSQL 17); DbUp SQL migrations (ADR-015) | Read replicas; partition location/segment tables |
| Object store | S3 API — MinIO for dev/self-host, any S3-compatible provider in prod | Lifecycle rules for tiering (`STR-004`) |
| Background work | .NET hosted services + PostgreSQL outbox; `LISTEN/NOTIFY` for pub/sub | MassTransit + RabbitMQ when justified |
| Cache | None | Redis when measured |
| AuthN | JWT + TOTP 2FA over bespoke Dapper-backed tables; no ASP.NET Core Identity (ADR-015) | Passkeys (Phase 2) |

Rationale for the trim: each component is an operational liability for a small team running a service people's safety depends on, and `docker compose up` producing a complete working system is a real censorship-resistance property, not a developer convenience. .NET Aspire is a reasonable dev-time inner-loop convenience but is **not adopted** for MVP — Compose stays canonical because it is also the self-host artifact.

---

## 5. System structure

### 5.1 Context

```
┌───────────────────────────────────────────────────────────────────────────┐
│ CLIENTS                                                                   │
│  Android app (MAUI shell + native capture core)   ← primary               │
│  iOS app (Phase 2, constrained: no background video)                      │
│  Blazor WASM PWA (dashboard · emergency console · degraded recorder)      │
│  Verifier CLI (offline, open source, no server contact)                   │
│  Future: dashcam ingest, home-security integration                        │
└──────────────┬───────────────────────────┬────────────────────────────────┘
               │ encrypted segments        │ REST + SignalR
               │ (resumable HTTPS)         │ (metadata, control, alerts)
┌──────────────▼───────────────────────────▼────────────────────────────────┐
│ SERVICE (single ASP.NET Core process at MVP)                              │
│  Ingest · Artifacts · Circles · Alerts · Heartbeat · Retention · Sharing  │
│  Workers: heartbeat monitor · alert dispatcher · retention · storage GC   │
└──────────────┬───────────────────────────┬────────────────────────────────┘
               │                           │
┌──────────────▼──────────────┐ ┌──────────▼────────────────────────────────┐
│ PostgreSQL 17               │ │ S3-compatible object storage              │
│ metadata · outbox · pub/sub │ │ encrypted segments & artifacts (opaque)   │
└─────────────────────────────┘ └───────────────────────────────────────────┘
               │
     ┌─────────▼──────────────────────────────────────┐
     │ Alert fan-out: FCM · APNs · UnifiedPush ·      │
     │ SignalR · optional SMS                          │
     └────────────────────────────────────────────────┘
```

### 5.2 Solution layout

One solution, eight projects, `components/` convention (ADR-015). `dotnet build` and `dotnet test` green:

```
personal-recorder/
├── PersonalRecorder.slnx                        # .NET 10 XML solution format
├── PersonalRecorder.WorkloadFree.slnf           # the subset that builds without a mobile workload (CI core job)
├── global.json · Directory.Build.props · Directory.Packages.props · .editorconfig
├── components/
│   ├── core/PersonalRecorder.Core/              # net10.0 — open source. Namespaces: PersonalRecorder, .Models, .Evidence, .Cryptography
│   ├── client/
│   │   ├── PersonalRecorder.Client/             # net10.0 + net10.0-android (+ios in Phase 2): Capture, Recording, Sync, Storage, Remote, Presentation
│   │   ├── PersonalRecorder.App/                # MAUI shell — net10.0-android (+ios in Phase 2)
│   │   └── PersonalRecorder.Harness/            # headless .NET for Android capture harness; deleted once Spike C passes
│   ├── service/PersonalRecorder.Service/        # net10.0 — one process, feature namespaces (§9.1)
│   ├── web/PersonalRecorder.Web/                # Blazor WASM PWA
│   └── verifier/PersonalRecorder.Verifier/      # CLI — open source
├── test/PersonalRecorder.Tests/                 # .Core · .Client · .Service · .Architecture
├── tools/docker-compose.dev.yml                 # PostgreSQL 17 + MinIO, buckets auto-created
├── .github/workflows/build.yml                  # core job (workload-free filter) + separate Android job
├── docs/                                        # this file, ADRs, proposals
└── design/                                      # naming history, UX notes
```

Four things force an assembly boundary, and nothing else does: target framework, deployment artifact, the open-source boundary (ADR-014), and the replaceable shell (ADR-002). Layering inside an assembly is expressed by namespace and enforced by §5.3.

**Naming.** `Core` owns the root namespace: it contains `PersonalRecorder`, `PersonalRecorder.Models`, `PersonalRecorder.Evidence` and `PersonalRecorder.Cryptography`, and the `Core` segment appears in no namespace. Every other assembly namespaces under its own name (`PersonalRecorder.Client.Capture`, `PersonalRecorder.Service.Ingest`).

**Client** is multi-targeted. The `net10.0` flavour is what the Blazor PWA references — view-models, service clients, the sync queue — and has no capture implementation. Platform code lives under `Platforms/<os>/` and is compiled in by folder convention, never by `#if`. Namespaces: `.Capture` (the contracts and the COLD/WARM/HOT/RECORDING state machine, with platform implementations in `.Capture.Android` and `.Capture.Ios`), `.Recording` (session orchestration, lookback, the panic pipeline), `.Sync` (the one prioritized upload queue), `.Storage` (local encrypted store and search index), `.Remote` (HTTP and SignalR clients), `.Presentation` (view-models shared by the shell and the browser; no UI-framework types — this is the seam that makes the shell replaceable).

**Service** is organised by feature, not by layer: `.Ingest`, `.Artifacts`, `.Circles`, `.Alerts`, `.Heartbeat`, `.Sharing`, `.Identity` (§9.1), meeting at `.Data` (Dapper repositories, row types, migrations), `.Storage` (S3) and `.Messaging` (outbox and push adapters), with `.Controllers`, `.Hubs` and `.Workers` as the entry points.

**The headless Android harness is deliberate.** It exercises the capture core with no UI-framework variables — fast iteration on the hard parts — and doubles as the soak-test and CI device-test vehicle. It exists so Spike A can proceed without waiting on Spike C, and is deleted when Spike C passes.

### 5.3 Dependency rules (enforced by `test/PersonalRecorder.Tests`)

Project level — six edges, read from the `.csproj` files:

```
Core     → (BCL only; no packages)
Client   → Core
App      → Client
Harness  → Client
Web      → Core, Client
Service  → Core
Verifier → Core                    (no service reference, no network stack)
```

Namespace level — read from the compiled IL, because project references cannot see inside an assembly:

```
Core                             → never Client, Service, Web or App; no System.Net, no ASP.NET
Client.Capture.*                 → never Microsoft.Maui.*, never Client.Presentation
Client.Presentation              → never any UI framework
Service.<feature>                → never another feature, never Controllers, Hubs or Workers
Service.Data / Storage / Messaging → never a feature, never an entry point
Verifier                         → never Service, never System.Net
```

Violations fail the build. This is what keeps §4.3 true over time, and it catches what project references never could — a view-model leaking into the capture core inside one assembly.

---

## 6. Capture core

### 6.1 State machine

| State | Android | iOS | Power | Entry |
|---|---|---|---|---|
| **COLD** | Camera closed | Session stopped | ~0 | Default / user disable / timeout |
| **WARM** | CameraX bound, preview ~10 fps, no encoder | Session running, no writer | Low | App foregrounded |
| **HOT** | Encoder running → encrypted ring buffer (10 s–5 min) | Writer running → ring | Medium | User arms ("Ready"), or geofence/context rule |
| **RECORDING** | Ring sealed and queued; continuous segment upload | Same, foreground only | Full | Panic or manual record |

`PERF-001` (< 500 ms) is measured **WARM → RECORDING** and enforced as an automated on-device test, not an aspiration. COLD → RECORDING is measured separately and reported honestly (camera cold start dominates; target < 1.5 s).

### 6.2 The ring buffer

- Video and audio hardware-encoded into **~2-second fMP4 (CMAF) segments**.
- Each segment is **individually encrypted and independently decryptable** — a partial upload is still usable evidence, which is the entire point of streaming during an incident.
- The ring is **disk-backed**, not memory-backed: a 5-minute 1080p lookback is far too large for RAM, and disk survives process death. Files are encrypted at rest with a key held in the OS keystore.
- The encoder path never blocks on network or disk-flush latency. Upload is fully decoupled through the queue (§6.4). A backed-up network degrades upload freshness, never capture.
- **Watchdog** (`REL-002`): a monitor asserts that segments appear at the expected cadence and that bytes reach disk. Absence of expected work raises a visible error — silent failure is the failure mode that matters here.

### 6.3 Panic path budget (`PERF-002` < 2 s)

Achieved by pre-doing everything that can be pre-done while HOT:

| t | Action |
|---|---|
| 0 ms | Button press → haptic + unmistakable UI state change |
| < 50 ms | Ring sealed; buffered segments enqueued at top priority; state → RECORDING |
| < 100 ms | Alert composed and pushed over the **already-warm** SignalR connection; push API called in parallel |
| < 300 ms | First buffered segment begins uploading on a **pre-established TLS connection** to a **pre-created upload session** (recording ID and DEK were generated on entry to HOT — panic never waits for a round trip) |
| < 2 s | First segment acknowledged; alert delivery receipts begin arriving; 911 quick-dial overlay presented (`EMR-002`) |

The overlay never auto-dials (§11.3). Recording continues behind and during the call (`EMR-003`).

### 6.4 Upload queue

One persistent, encrypted, prioritized queue (SQLite-backed) carries **everything** — segments, snapshots, notes, tags, location tracks:

```
priority 0  panic-triggered segments (including sealed lookback)
priority 1  live segments of an active recording
priority 2  snapshots, notes, tag/metadata mutations
priority 3  backfill of earlier local-only recordings
```

Resumable (tus-style offsets), backpressure-aware, survives process death, resumes on connectivity. Offline recording and later sync (`REC-006/007`) are **the same code path**, not a special case — which is why they will actually work.

### 6.5 Platform truths

| Capability | Android | iOS |
|---|---|---|
| Video with app backgrounded / screen off | ✅ Foreground service (`camera` type). Must be started **while in the foreground**, then persists; onboarding requests battery-optimization exemption | ❌ Not possible at the OS level. UX answer: "podium mode" — screen dimmed to black, app foreground, recording continues — or audio-only |
| Audio in background | ✅ FGS (`microphone` type) | ✅ Background audio mode |
| Rolling buffer while HOT | ✅ | ✅ foreground only |
| Stealth / silent panic (`PRV-003/004`) | ✅ in sideloaded build; **feature-flagged OFF in the Play build** | Keep minimal; store-reviewed |
| Emergency dial | `tel:` intent overlay, one tap | `tel:` URL, one tap |

This table is why iOS is Phase 2: Android delivers the full vision; iOS delivers a constrained, honest version that needs its own UX pass.

---

## 7. Evidence format and integrity

The `PersonalRecorder.Evidence` namespace of `PersonalRecorder.Core` is the single canonical implementation — used by both capture cores, the browser (WASM), the verifier CLI, and the service (chain-continuity checks only, never decryption). One format, one implementation, no drift.

### 7.1 Segment container (`.prseg`)

```
┌── header (plaintext, authenticated as AEAD associated data) ──────────────┐
│ magic "PRSG" · format version · recording ULID · segment index (u32)      │
│ media track id · capture start/end (monotonic + wall clock)               │
│ prev_chain_hash (32 B) · flags (keyframe | panic | from-lookback)         │
├── body ───────────────────────────────────────────────────────────────────┤
│ AES-256-GCM ciphertext of the CMAF segment                                │
├── trailer ────────────────────────────────────────────────────────────────┤
│ 16-byte GCM tag                                                           │
└───────────────────────────────────────────────────────────────────────────┘
```

- **Per-segment key:** `K_seg = HKDF-SHA256(DEK, info = "prseg-v1" ‖ recordingId ‖ segIndex)`. A leaked segment key exposes one segment, never the recording.
- **Nonce:** 4-byte context ‖ 8-byte big-endian segment index. Reuse is structurally impossible.
- The header is authenticated but not encrypted, so the server can verify chain continuity and ordering without any ability to decrypt.

### 7.2 Chain, signatures, countersignature

```
chain_hash[n] = SHA-256( chain_hash[n-1] ‖ SHA-256(header ‖ ciphertext ‖ tag) )
```

- Client signs `(recordingId, n, chain_hash[n], wallclock)` with its **Ed25519 device key** every ~15 segments (≈30 s) and at finalize.
- The **server countersigns** each received chain head with its own Ed25519 key plus server receive time, and returns the countersignature to the client. This yields *proof of existence by time* with no access to content.
- The verifier checks: continuity (no missing or reordered indices), integrity (hashes match), authorship (device signature), and independent time bounds (server countersignature). Device clocks can be wrong or manipulated; the countersignature is what makes the timeline defensible.

### 7.3 Verification surfaces

| Surface | What it does |
|---|---|
| **Verifier CLI** (`PersonalRecorder.Verifier`, NativeAOT, **open source**) | Given an export bundle, verifies completeness, integrity, signer, and timestamps **with no contact with our servers**. This is what makes the evidentiary story real to a court, a lawyer, or a journalist. |
| **In-browser verification** | Same `Evidence` library via WASM, so a recipient can verify a shared artifact in the emergency console with no install. |
| **Service-side continuity check** | Header-only; detects gaps at ingest so we can tell the user *now* rather than at trial. |

### 7.4 Export bundle

A self-contained, signed directory (or zip): media segments, manifest with the full chain, signatures, server countersignatures, our public keys, the format specification, and verification instructions. Handed to counsel it is verifiable without us, forever, even if the service no longer exists.

---

## 8. Cryptography and keys

### 8.1 Hierarchy

```
Identity keypair (X25519)  — per user; private key wrapped by OS keystore / Secure Enclave
   ├── Device signing key (Ed25519)  — per device; signs chain heads
   ├── Recording DEK (AES-256)       — per recording; sealed-box wrapped to:
   │        owner identity key · each authorized circle member/device · capability links
   ├── Artifact DEK                  — per snapshot / note / location track
   ├── Tag index key                 — derives blinded tag tokens (§8.4)
   └── Recovery key                  — printable code; wraps the identity key (escrow, §8.3)
```

Primitive family: **libsodium** across .NET, WASM, and tooling. Circle sharing and revocation operate on **key wraps**, never on re-encrypting content.

### 8.2 Everything is E2EE — not just video

Media, snapshots, **notes, tags, and location tracks** are all encrypted client-side. This is a deliberate extension of the archived key model, and it has real consequences we accept:

- Server-side geofencing is impossible → **geofencing is evaluated on-device** (`LOC-003`).
- Server-side search over note text is impossible → **the client maintains an encrypted search index**.
- Server-side thumbnailing/transcoding is impossible → **thumbnails are generated and encrypted at capture time**.

Location tracks are batched into encrypted track segments carrying the same chain properties as media, so a location timeline is evidence of the same quality as the video.

### 8.3 Recovery

**Recovery-key escrow is ON by default**, with an explicit **zero-knowledge hard mode** opt-out. A printable/exportable recovery code wraps the identity key; the server stores only a blob it cannot open.

Rationale: evidence that dies with a confiscated phone and a forgotten password serves no one — but the choice stays the user's, and the hard-mode option is real rather than theoretical. Corollary: the recovery-code UX must be aggressive (onboarding, periodic re-verification), or escrow is theater.

### 8.4 Sharing with people who are not users

The "lawyer, journalist, or grandmother on a borrowed Chromebook" case, solved without weakening E2EE:

```
https://app.example/v/{artifactId}#k={key material}&t={access token}
```

The **URL fragment is never transmitted to the server** — the key travels only in the recipient's browser. The token authorizes the ciphertext fetch and is independently expiring, revocable, and access-logged. Sharing a link never gives the service the ability to read the content.

### 8.5 What the server can still see — stated honestly

E2EE bounds content exposure. It does not bound metadata, and we document this rather than oversell:

| Visible to the server | Not visible |
|---|---|
| Account identity, devices, login times | Any media, note, or location content |
| Existence, size, timing and ordering of artifacts | What any artifact depicts or says |
| Circle membership graph | Circle message content |
| Blinded tag tokens (`HMAC(tag key, tag)`) — equality and frequency only | Tag names |
| That a location track exists and when | Where the user was |
| Alert routing (who was alerted, when) | Alert payload content |

Blinded tag tokens are a deliberate trade: they buy server-side tag filtering at MVP scale and leak equality and frequency patterns. Documented, not hidden.

---

## 9. Service

### 9.1 Modules

Each module is a feature namespace of `PersonalRecorder.Service` (ADR-015); features meet at `Data`, `Storage` and `Messaging`, and at the controllers, hubs and workers — never directly.

| Module | Responsibility |
|---|---|
| **Ingest** | Resumable segment upload sessions, offset tracking, header-only continuity checks, chain-head countersignature |
| **Artifacts** | Metadata CRUD, blinded-tag filtering, pin/retention state, export bundle assembly |
| **Circles** | Membership, roles and permissions (`COT-003`), invites, key-wrap distribution, revocation |
| **Alerts** | Panic and missed-check-in fan-out across all channels, per-channel delivery receipts, escalation |
| **Heartbeat** | Check-in sessions, interval config, miss detection, retention holds |
| **Sharing** | Capability links: issue, scope, expire, revoke, access log |
| **Identity** | Registration, JWT, TOTP, device enrolment, public-key directory |

### 9.2 Core tables (PostgreSQL 17)

`users` · `devices` (public keys, push tokens per channel) · `circles` · `circle_members` (role, permissions) · `recordings` (owner, times, state, retention, chain head, countersignature) · `segments` (index, size, storage key, hash, received_at) · `artifacts` (snapshots/notes, encrypted blobs) · `tag_tokens` · `key_wraps` (artifact ↔ recipient) · `location_tracks` · `heartbeat_sessions` · `alerts` + `alert_deliveries` (per channel) · `share_links` · `outbox` · `audit_log`.

Partition `segments` and `location_tracks` by time when volume justifies it.

### 9.3 Workers

- **Heartbeat monitor** — detects misses, applies the escalation ladder, places retention holds.
- **Alert dispatcher** — drains the outbox, fans out across channels, records per-channel receipts, escalates undelivered panic alerts *visibly*.
- **Retention** — rolling-window expiry, pin honouring, tiering (`STR-004`), 30-day soft delete (`STR-006`), cryptographic erasure (`STR-009`: destroy the key wraps, then reclaim ciphertext).
- **Storage GC** — reconciles object storage against metadata; orphan detection both directions.

### 9.4 Real-time

SignalR carries presence, heartbeat check-ins, alert delivery, live-view notifications, and in-circle messaging (Phase 2). It is also the lowest-latency alert channel whenever a recipient's app is connected — and the only one with no third-party dependency at all.

### 9.5 Scale path (documented, not built)

Redis backplane when multi-node → read replicas → time-partitioned segment/location tables → dedicated ingest nodes (ingest is stateless and CDN-frontable) → message broker if outbox throughput demands it.

---

## 10. Web application

Blazor WASM PWA, installable, offline-capable for browsing cached artifacts.

- **Dashboard** — artifact browser, tag filtering, playback, pin/retention review, exports.
- **Emergency console** — live segment playback via MSE with client-side decryption; live location; one-tap call to the member or emergency services.
- **Degraded recorder** — the unblockable floor (§4.4).
- **Verification view** — chain verification in-browser via the shared `Evidence` library.
- **Capability-link viewer** — the no-account recipient experience (§8.4).

---

## 11. Emergency response

### 11.1 Panic

Press → recording guaranteed active → circle alerted across every channel → 911 quick-dial overlay → recording continues throughout. Silent mode (`EMR-007`) suppresses all local UI indication; sideload builds only.

### 11.2 Heartbeat / dead-man's switch

The feature that exists because a detained user cannot press anything:

```
check-in due ─► missed ─► (1) local nag + escalating device prompts
                       ─► (2) circle alerted (HB-003) + RETENTION HOLD placed (HB-004)
                       ─► (3) emergency escalation per user config (HB-005)
```

The retention hold is critical and easy to overlook: a missed check-in must **prevent** the rolling window from expiring, or detention itself destroys the evidence.

### 11.3 911

No programmatic dialling. Panic presents a persistent overlay with 911 pre-loaded — one tap completes the call — plus configured emergency contacts and a silent-alert-only option. This avoids the carrier-partnership, E911-location and liability barriers entirely while keeping the user in control. RapidSOS integration is a Phase 4 consideration if scale justifies the partnership.

---

## 12. Distribution and side-loading

### 12.1 The distribution landscape — time-critical

- **Google developer verification.** Certified Android devices (Android 8+) carry an Android Developer Verifier system service, and Google is phasing in a requirement that installed apps come from verified developers. The rollout is staged: first the high-friction "advanced flow" for installing unverified apps — enable developer mode, restart, **wait 24 hours**, re-authenticate — then **hard enforcement**, beginning in Brazil, Indonesia, Singapore and Thailand, where certified devices block normal installs of apps from unregistered developers — from Play *and* from OEM stores — and expanding worldwide after that. Registration is open.
- **iOS.** No alternative distribution in the **US**. EU (DMA) and **Japan** (MSCA; iOS 26.2) have Apple-authorized marketplaces and web distribution — all still Apple-notarized. Plan for US movement; do not depend on it.
- **Web.** Unrestricted. The PWA cannot be de-platformed.

**Implication:** "just ship an APK" is no longer a complete censorship-resistance strategy, and the window to get ahead of it is closing. Verification is identity registration, not app review — but it creates a revocation capability, which is exactly the kind of pressure point §3.3 tells us to plan around.

### 12.2 Channel matrix

| Channel | Priority | Notes |
|---|---|---|
| **Direct APK + in-app self-updater** | P0, day 1 | The Signal model. An APK without an update channel becomes a stale, dangerous artifact — the updater ships in the *first* public build. Signed manifest on our domain, mirrored (GitHub Releases) |
| **Own F-Droid repository** | P0 | Our repo, our signing, no main-repo policy gate; users add one URL. Hosts proprietary builds fine |
| **Obtainium** | P1 | Needs only stable release URLs and versioning discipline |
| **Google Play** | P1, parallel | "Personal safety" positioning; stealth features compiled out via channel flags; also satisfies verification reach for mainstream users |
| **AOSP ROMs (GrapheneOS/CalyxOS)** | P1 | **Unaffected by Google verification**, and disproportionately our audience |
| **iOS App Store + TestFlight** | P1 (Phase 2) | Store-clean build; expect review friction on background/stealth behaviours |
| **EU / Japan alternative distribution** | P2 | When entity eligibility and volume allow |
| **Blazor PWA** | P0 | The floor. Cannot be blocked |
| **`adb install`** | documented | Advanced-user escape hatch; remains available under the new rules |

### 12.3 Actions on the critical path (start now — see D11)

1. **Complete NimbleJinn LLC formation, then Google developer verification immediately.** Play Console organization accounts require a **D-U-N-S number**; it is free but lead time runs weeks. This is the longest-lead item in the entire project and it is gated by a legal formation, not by engineering.
2. **Signing-key ceremony** — generate Android signing keys offline, publish SHA-256 certificate fingerprints on the site and in docs (`apksigner verify --print-certs`), document custody (who, where, hardware token). Do this *before* the first external install; key custody cannot be retrofitted.
3. **Stand up the update-manifest endpoint and mirror** — a static signed JSON plus APKs. Trivial, but must exist before the first user installs.
4. **Reproducible builds** — stated goal, targeted in Phase 3; a major trust signal for this audience.

### 12.4 Channel feature flags

One codebase: `SIDELOAD` (full features incl. stealth/silent panic), `PLAY`/`APPSTORE` (stealth off, store-safe), `FDROID`. **The capture core is identical everywhere**; only surface features flag.

---

## 13. Threat model (summary — full document lands in Phase 0)

| Adversary / event | Mitigation | Residual risk |
|---|---|---|
| On-scene seizure of the device mid-incident | Segments already uploaded are already safe; lookback sealed and prioritized at panic; local store encrypted with keystore-bound keys; optional duress PIN (D12) | Everything not yet uploaded when the network was unavailable |
| Legal compulsion / subpoena of the service | E2EE: we can produce only ciphertext plus the metadata in §8.5 | Metadata is real and is documented, not denied |
| Platform coercion (store delisting, push revocation, developer-verification revocation) | Multi-channel distribution (§12), multi-channel alerts (§11), PWA floor, self-host path | APNs has no alternative on iOS; stated honestly |
| Cloud provider termination or domain seizure | Provider-neutral (S3 API, Compose deploy), mirrors, self-hosting | Migration is disruptive even when possible |
| Account takeover | TOTP now, passkeys Phase 2; new-device enrolment requires existing-device or recovery-key approval | Recovery code compromise |
| Server compromise | Server holds no content keys; countersigning key is the only sensitive server key | Metadata exposure; forged countersignature timestamps going forward |
| Endpoint compromise (malware on the phone) | Keystore/Enclave custody, no plaintext at rest outside the process | E2EE cannot survive a compromised endpoint — true of every system |
| Network interference / captive portals | Resumable queue, offline-first, retry with backoff, TLS pinning (hard pin in sideload builds, backup pins in store builds) | Total sustained blocking delays upload |

---

## 14. Non-functional targets and how each is verified

| ID | Target | Verification |
|---|---|---|
| `PERF-001` | WARM → RECORDING < 500 ms | Automated on-device instrumentation test in CI; device matrix |
| `PERF-002` | Panic → stream start < 2 s | Instrumented end-to-end test with a network shaper |
| `PERF-003` | Zero buffered-frame loss | Property test: `SIGKILL` at random points; verifier must validate the partial chain |
| `PERF-004` | Continuous background operation | 4-hour soak on ≥2 physical devices (flagship + budget); battery and thermal recorded |
| `REL-002` | No silent failures (99.99 %) | Watchdog assertions on encoder cadence and disk writes; fault injection |
| `REL-003` | Full offline operation | Chaos suite: airplane mode, captive portal, 2G, mid-upload connectivity loss |
| `SEC-001/004` | E2EE, tamper evidence | Golden-file format tests across versions; independent verifier as the oracle; external audit before Phase 3 exit |
| `REL-001` | 99.9 % service uptime | OpenTelemetry, synthetic probes on panic and heartbeat paths |

---

## 15. Delivery plan

Estimates assume ~1 senior engineer full-time with heavy AI pairing.

### Phase 0 — Foundations and de-risking (3–4 weeks)

Two tracks run in parallel; the second is calendar-critical and mostly not engineering:

**Engineering**
- Ratify §17 decisions → ADRs.
- Scaffold the solution per §5.2 (`.sln` and project skeletons together, top-down), CI/CD (build, test, signed APK artifact, Blazor publish), `tools/docker-compose.dev.yml`.
- **Spike A (existential) — Android capture core.** CameraX WARM/HOT, MediaCodec → encrypted CMAF ring, foreground service, 4-hour soak on two physical devices. *Exit: WARM→RECORDING < 500 ms; zero buffer loss; survives screen-off; battery burn measured and written down.*
- **Spike B — walking skeleton.** Harness records → encrypts → chunk-uploads → Blazor page lists, decrypts, plays. *Exit: E2EE round trip in Chrome and Safari.*
- **Spike C (timeboxed 1 week) — MAUI shell slice.** Camera preview embedding, panic screen, cold start, APK size, screen-reader pass. *Exit: pass → MAUI confirmed; fail → one Uno week, then decide.*

**Distribution / entity (critical path)**
- LLC formation → D-U-N-S → Google developer verification → signing-key ceremony → update-manifest endpoint. Start immediately; §12.3.

### Phase 1 — MVP: Android + Web (10–12 weeks)

Auth (JWT + TOTP over bespoke tables); video/audio/snapshot capture with lookback; notes and tags (system, custom, smart sort); panic + 911 overlay + single-circle alerts (SignalR + FCM + UnifiedPush); location tracking and metadata; retention windows, pinning, auto-disable safety net; Blazor dashboard (browse, filter, playback, export); **distribution v1** (direct APK + self-updater + own F-Droid repo + published fingerprints). Trusted-cohort beta from ~week 8.

### Phase 2 — iOS + Circles (8–10 weeks)

iOS capture core (AVFoundation, podium mode, audio-only background); TestFlight; multiple circles with permission levels; live view in the console and native clients; heartbeat/check-in with escalation; in-circle messaging; passkeys.

### Phase 3 — Hardening (6–8 weeks)

Hostile-network robustness; storage tiering and archival review flow; push redundancy completion; **verifier CLI 1.0** and chain countersigning; OpenTelemetry and silent-failure detectors; load and chaos tests; **external security audit and fixes**; reproducible builds; .NET 11 compatibility validated in CI; App Store submission.

### Phase 4+ — Advanced

Conferencing via self-hosted **LiveKit** (`COM-003`); geofencing; zero-knowledge hard mode polish; voice activation and on-device transcription (**on-device only** — server-side would break E2EE); organization accounts; dashcam and home-security integrations.

---

## 16. Risk register

| Risk | L×I | Mitigation |
|---|---|---|
| Google developer-verification revocation used as a pressure vector | M×H | Verify early under a considered entity; own F-Droid repo; AOSP-ROM path (unaffected); PWA floor; documented `adb` path |
| **Verification not completed before enforcement** | **M×H** | §12.3 item 1 starts now; it is gated on LLC formation and D-U-N-S lead time, not engineering |
| Apple rejection or removal | H×M | Store-clean build, safety positioning, TestFlight, EU/JP channels, PWA floor; iOS is *a* channel, not *the* channel |
| iOS background-video limitation disappoints users | H×M | Honest UX (podium mode, audio-only), expectations set in marketing, Android-first messaging |
| Battery burn makes HOT impractical | M×H | Spike A measures before we promise; adaptive quality ladder; per-device-class encoder tuning |
| MAUI quality issues | M×M | Thin shell over framework-free core; Spike C gate; pre-agreed Uno fallback |
| .NET 11 CoreCLR-on-mobile migration surprises | M×M | Stay on .NET 10 LTS through Phase 2; validate .NET 11 in CI during Phase 3 |
| Solo bus-factor | H×H | Everything as code + ADRs; boring infrastructure; document key ceremonies above all |
| E2EE key loss = evidence loss | M×H | Escrow default ON; aggressive recovery-code UX; multi-device key sync in Phase 2 |
| Legal exposure (two-party consent, jurisdiction) | M×H | Jurisdiction-aware consent defaults; counsel before public beta; we ship no legal advice, we link Know-Your-Rights resources |
| Android camera fragmentation | H×M | CameraX baseline; device matrix (flagship / budget / Samsung / Pixel); harness in CI device farm |
| Scope creep — this specification is *large* | H×M | Phase gates and ADR discipline; the MVP cut is the Phase 1 list, nothing more |

---

## 17. Decisions

**All fifteen are ratified** (§17.1).

| # | Topic | Decision |
|---|---|---|
| **D1** | Runtime | **.NET 10 LTS** across the stack; LTS-to-LTS policy; .NET 11 validated in CI but not shipped during MVP (§4.1) |
| **D2** | Client architecture and shell | **Native C# capture core + thin .NET MAUI shell.** Godot not recommended for the phone client on current evidence (§4.2); Uno is the pre-agreed fallback; Spike C gates it |
| **D3** | Mobile UI composition | **All-native MAUI XAML for MVP.** Blazor Hybrid (sharing Razor components with the web dashboard) is attractive for content screens but adds a WebView dependency and a second UI paradigm on the panic path — evaluate for the artifact browser in Phase 2 |
| **D4** | Evidence pipeline | **Encrypted chunked CMAF upload over resumable HTTPS**, not WebRTC. LiveKit only when conferencing arrives (§4.5, §6) |
| **D5** | Shared evidence library | **Yes — `PersonalRecorder.Evidence`** as the single canonical format/crypto implementation used by capture cores, browser (WASM), and verifier CLI (§7) |
| **D6** | E2EE scope | **Everything** — media, snapshots, notes, tags, location tracks. Accept the consequences: client-side geofencing, client-side search index, capture-time thumbnails, blinded tag tokens (§8.2, §8.5) |
| **D7** | Recovery | **Escrow ON by default**, zero-knowledge hard mode as an explicit opt-out (§8.3) |
| **D8** | Sharing with non-users | **Capability links with the key in the URL fragment** — server never receives key material; token expiring, revocable, logged (§8.4) |
| **D9** | Backend footprint | **ASP.NET Core + PostgreSQL + S3-compatible storage, single process.** No Redis, no broker, no Aspire at MVP; Compose is canonical because it is also the self-host artifact (§4.5) |
| **D10** | Alert delivery | **FCM + APNs + UnifiedPush + SignalR + optional SMS**, per-channel delivery receipts, visible escalation on failure (§11) |
| **D11** | Distribution — **time-critical** | **Start entity formation, D-U-N-S, Google developer verification and the signing-key ceremony now.** Advanced-flow friction is global; hard enforcement is phased in by country (§12.1, §12.3) |
| **D12** | Duress features | **Build duress PIN + decoy state, sideload-channel only, default off, never destructive.** High value for the DV and detention scenarios |
| **D13** | Open-source posture | **Verifier CLI *and* the evidence format specification are FOSS from day 1** (that is what buys trust); client and service proprietary for now, revisit ~Phase 3 |
| **D14** | iOS timing | **Phase 2.** Android + PWA covers every user at MVP without doubling Phase 1 |
| **D15** | Name | Keep **"Personal Recorder"** / `PersonalRecorder.*`. Rebranding later needs no codebase change; `design/NAMING.md` holds the candidate list |

### 17.1 Ratification record

D1–D15 ratified as recommended. Two carried explicit comment from Jeffrey: **D12** (duress features) was affirmed as a good catch and is now in scope; **D15** kept the working name, on the reasoning that rebranding later needs no codebase change — so the name must not be allowed to impede progress.

| # | Outcome | Recorded as |
|---|---|---|
| D1 | .NET 10 LTS; LTS-to-LTS policy, STS validated in CI only | [ADR-001](./adr/001-dotnet-10-lts.md) |
| D2 | Native C# capture core + thin MAUI shell; Godot rejected for the phone client; Uno pre-agreed fallback | [ADR-002](./adr/002-native-capture-core-maui-shell.md) |
| D3 | Native MAUI XAML for MVP; Blazor Hybrid re-evaluated in Phase 2 for content screens only | [ADR-003](./adr/003-native-xaml-ui-composition.md) |
| D4 | Encrypted chunked CMAF upload; LiveKit deferred to conferencing | [ADR-006](./adr/006-encrypted-chunked-evidence-pipeline.md) |
| D5 | `PersonalRecorder.Evidence` as the single canonical implementation | [ADR-007](./adr/007-shared-evidence-library.md) |
| D6 | E2EE covers media, snapshots, notes, tags and location tracks | [ADR-008](./adr/008-e2ee-scope.md) |
| D7 | Recovery-key escrow ON by default; zero-knowledge hard mode opt-out | [ADR-009](./adr/009-key-model-and-recovery.md) |
| D8 | Capability links; key in URL fragment, never sent to the server | [ADR-010](./adr/010-capability-link-sharing.md) |
| D9 | ASP.NET Core + PostgreSQL + S3, single process; no Redis, broker, or Aspire at MVP | [ADR-005](./adr/005-aspnet-core-lean-backend.md) |
| D10 | Five-channel alert delivery with per-channel receipts | [ADR-011](./adr/011-redundant-alert-delivery.md) |
| D11 | Multi-channel distribution; entity, D-U-N-S, verification and signing ceremony start now | [ADR-012](./adr/012-distribution-and-side-loading.md) |
| D12 | **Agreed.** Duress PIN + decoy state, side-load channel only, default off, never destructive | [ADR-013](./adr/013-duress-and-stealth-features.md) |
| D13 | Verifier CLI and evidence format specification FOSS; client and service proprietary, revisit ~Phase 3 | [ADR-014](./adr/014-open-source-posture.md) |
| D14 | iOS in Phase 2 | This document, §15 |
| D15 | **Name kept**: "Personal Recorder", `PersonalRecorder.*`. Rebranding later requires no codebase change | This document |

Web-surface technology (Blazor WASM PWA, §4.4) was ratified implicitly as part of the whole and is recorded as [ADR-004](./adr/004-blazor-wasm-pwa.md). The layout and service-stack decisions L1–L9 of [Proposal 001](./proposals/001-consolidated-project-and-namespace-layout.md) are recorded as [ADR-015](./adr/015-consolidated-layout-and-service-stack.md).

---

## 18. Sources

- Godot C# platform status: [Exporting for Android — Godot docs (stable)](https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_android.html) · [Exporting for iOS — Godot docs](https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_ios.html) · [Current state of C# platform support in Godot](https://godotengine.org/article/platform-state-in-csharp-for-godot-4-2/) · [Godot 4.6 release archive](https://godotengine.org/download/archive/4.6-stable/) · [C# web export status discussion](https://github.com/godotengine/godot-proposals/discussions/13076)
- .NET / MAUI: [What's new in .NET MAUI for .NET 11 — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/maui/whats-new/dotnet-11?view=net-maui-10.0) · [Microsoft is moving .NET MAUI to CoreCLR in .NET 11 — Thurrott](https://www.thurrott.com/dev/336040/microsoft-is-moving-net-maui-to-coreclr-runtime-in-net-11) · [.NET 11 Preview 6 roundup — Visual Studio Magazine](https://visualstudiomagazine.com/articles/2026/07/15/net-11-preview-6-roundup-aspnet-core-maui-c-ef-core-and-sdk-updates.aspx)
- Uno Platform: [Uno Platform and Microsoft .NET team collaboration](https://platform.uno/blog/announcing-unoplatform-microsoft-dotnet-collaboration/) · [MAUI vs Avalonia vs Uno in 2026](https://startdebugging.net/2026/05/maui-vs-avalonia-vs-uno-in-2026/)
- Android distribution: [Google sets Sept. 30 deadline for developer verification — The Hacker News](https://thehackernews.com/2026/06/google-sets-sept-30-deadline-for.html) · [Android sideloading changes timeline — Android Authority](https://www.androidauthority.com/android-sideloading-changes-timeline-3679204/) · [Understanding Android developer verification — Google support](https://support.google.com/android-developer-console/answer/16561738?hl=en)
- Android platform constraints: [Foreground service types — Android Developers](https://developer.android.com/develop/background-work/services/fgs/service-types) · [Restrictions on starting a foreground service from the background](https://developer.android.com/develop/background-work/services/fgs/restrictions-bg-start)
- iOS distribution: [About alternative app distribution — Apple](https://support.apple.com/en-us/118110) · [Changes to iOS in Japan — Apple Developer](https://developer.apple.com/support/app-distribution-in-japan/)

