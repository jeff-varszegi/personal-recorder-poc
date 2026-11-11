# ADR-005: Encrypted Chunked-Upload Evidence Pipeline (not WebRTC)

## Status
Accepted (2026-07-31) — revises spec §4.1's WebRTC-SFU-first streaming architecture

## Context
Spec v0.1 assumed WebRTC/HLS ingest through an SFU with server-side transcoding. But the primary job is **evidence preservation**: every buffered frame must eventually land, intact and provable, across hostile networks and offline gaps (PERF-003, REC-006/007, REL-002). WebRTC is lossy by design, hard to E2EE (insertable streams), needs SFU/TURN/UDP infrastructure, and server-side transcoding is impossible under E2EE anyway. Sub-second latency matters only for conferencing (COM-003, Phase 4) — not for evidence or "is my person okay" live viewing.

## Decision
- Capture cores hardware-encode into **~2-second fMP4 (CMAF) segments**. Each segment is AES-256-GCM encrypted client-side with a per-recording key, appended to a SHA-256 hash chain, periodically signed (Ed25519), stored in the encrypted local ring/queue, and uploaded over HTTPS via a **resumable, prioritized, backpressure-aware queue** (tus-style offsets; priority: panic segments > live segments > snapshots/notes > backfill).
- The server is a dumb encrypted-blob store plus metadata; it countersigns and timestamps received chain heads (proof of existence without content access).
- **Circle live view**: members fetch the same encrypted segments via a rolling playlist (LL-HLS-style) and decrypt client-side — MSE in the browser, ExoPlayer/AVPlayer custom data sources natively. Expected latency ~3–6 s; zero added server compute.
- **Conferencing (Phase 4)**: deploy self-hosted **LiveKit** (WebRTC SFU; JWT token minting from .NET) rather than building an SFU.

## Consequences
- (+) Lossless, offline-tolerant, resumable delivery; E2EE is trivial; infra is plain HTTPS + object storage (CDN-able, self-hostable); tamper-evidence (SEC-004) falls naturally out of the per-segment hash chain.
- (+) Offline recording + later sync (REC-006/007) is the same code path, not a special case.
- (−) Live view latency ~3–6 s instead of sub-second — acceptable for the monitoring use case; revisit only for conferencing.
- (−) Client-side decryption on viewers means no server-side thumbnails/transcodes; thumbnails are generated and encrypted client-side at capture time.

## Alternatives Considered
- **WebRTC → SFU with server recording** (spec v0.1): sub-second latency, but lossy, E2EE-hostile, heavy infrastructure, UDP-blocked networks fail.
- **RTMP/SRT ingest**: streaming-industry standard, but plaintext-at-ingest (breaks E2EE) and adds a media-server tier.
- **Whole-file upload after recording**: simplest, but loses the "evidence survives device confiscation mid-incident" property that segments give.
