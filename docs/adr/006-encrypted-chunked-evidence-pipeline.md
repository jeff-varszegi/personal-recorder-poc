# ADR-006: Encrypted Chunked CMAF Upload, Not WebRTC

## Status
Accepted

## Context
The instinctive architecture for "stream video from a phone" is WebRTC into an SFU. But the primary job here is **evidence preservation**, not communication. Every buffered frame must eventually land, intact and provable, across hostile networks and offline gaps (`PERF-003`, `REC-006/007`, `REL-002`).

WebRTC is lossy by design — correctly so, for calls. It is also hostile to end-to-end encryption (insertable streams gymnastics, with the SFU still observing packet timing and sizes), and it requires SFU, TURN and UDP egress that many networks block. Server-side transcoding, the usual justification for an SFU, is impossible under E2EE anyway.

Sub-second latency matters for conferencing (`COM-003`, Phase 4). It does not matter for evidence, nor for "is my person okay" live viewing, where a few seconds is fine.

## Decision
- Capture cores hardware-encode into **~2-second fMP4 (CMAF) segments**. Each segment is individually AES-256-GCM encrypted client-side, appended to a SHA-256 hash chain, periodically Ed25519-signed, written to the encrypted local ring, and uploaded over HTTPS through a **resumable, prioritized, backpressure-aware queue** (tus-style offsets; panic segments > live segments > snapshots and notes > backfill).
- Segments are **independently decryptable**, so a partial upload is still usable evidence — the property that makes streaming-during-an-incident worth doing at all.
- The server is an encrypted-blob store plus metadata. It verifies chain continuity from plaintext headers, and countersigns received chain heads with a timestamp.
- **Circle live view**: members fetch the same encrypted segments through a rolling playlist and decrypt client-side — MSE in the browser, ExoPlayer/AVPlayer custom data sources natively. Latency ~3–6 s, zero added server compute.
- **Conferencing (Phase 4)**: deploy self-hosted **LiveKit** rather than building an SFU.

## Consequences
- (+) Lossless, resumable, offline-tolerant delivery; E2EE is trivial rather than a fight.
- (+) Infrastructure is plain HTTPS plus object storage — CDN-able, self-hostable, and unblocked on networks that drop UDP.
- (+) Tamper evidence (`SEC-004`) falls naturally out of the per-segment chain instead of being bolted on.
- (+) Offline recording and later sync are the same code path as live upload, not a special case — which is why they will actually work.
- (−) Live view runs ~3–6 s behind rather than sub-second. Acceptable for monitoring; revisited only for conferencing.
- (−) No server-side thumbnails or transcodes; thumbnails are generated and encrypted at capture time.

## Alternatives Considered
- **WebRTC → SFU with server-side recording**: sub-second latency, but lossy, E2EE-hostile, infrastructure-heavy, and it fails on UDP-blocked networks.
- **RTMP/SRT ingest**: the streaming industry's standard answer, but plaintext at ingest — which breaks the central guarantee — and it adds a media-server tier.
- **Whole-file upload after recording**: simplest of all, and it loses precisely the property that matters most: evidence surviving a device seized mid-incident.
