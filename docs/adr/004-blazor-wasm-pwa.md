# ADR-004: Blazor WebAssembly PWA for the Web Surface

## Status
Accepted

## Context
The system needs a browser surface for four distinct reasons: a dashboard for reviewing and exporting artifacts; an emergency console a circle member can open from an alert on any device with zero install; playback of end-to-end-encrypted media; and — most importantly — a recording capability that no platform owner can remove.

That last one is a threat-model requirement, not a convenience. If both app stores delist us and Android's developer-verification regime is turned against us, the web must still record.

## Decision
Blazor WebAssembly, delivered as an installable PWA, providing:

- **Dashboard** — artifact browser, tag filtering, retention and pin review, exports.
- **Emergency console** — live segment playback via Media Source Extensions with client-side decryption, plus live location (Managed Media Source covers iPhone Safari 17.1+).
- **Degraded fallback recorder** — `getUserMedia` + `MediaRecorder` with chunked encrypted upload. Foreground-only, no lookback prewarm parity, storage-eviction caveats, labelled plainly as degraded.
- **Verification view** — chain verification in the browser using the same `Evidence` library that wrote the recording.

Small JavaScript interop shims for `MediaRecorder` and MSE are expected; they are glue, never application logic.

## Consequences
- (+) The same C# evidence and crypto code runs on the phone, in the browser, and in the verifier CLI — one implementation, three hosts, no format drift.
- (+) An un-deplatformable floor under the entire product.
- (+) Blazor's C# reuse means the web surface costs a fraction of what a separate front-end stack would.
- (−) WASM payload size and cold start are worse than a hand-tuned JS app; acceptable for a dashboard and console, and mitigated by PWA caching.
- (−) The fallback recorder is genuinely inferior to the native one. Communicating that honestly, without scaring users away from it in the moment they need it, is a UX problem we own.

## Alternatives Considered
- **JavaScript/TypeScript SPA**: better payload characteristics, at the cost of a second language, a second crypto implementation, and a permanent format-drift risk on the one thing that must never drift.
- **Server-rendered Blazor**: cheaper first paint, but requires a live server connection — unacceptable for a surface whose purpose includes surviving loss of our infrastructure — and cannot do client-side decryption offline.
- **No web surface**: removes the only distribution channel that cannot be blocked.
