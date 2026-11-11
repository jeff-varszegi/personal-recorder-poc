# ADR-008: End-to-End Encryption Covers Every Artifact Type

## Status
Accepted

## Context
It is normal to encrypt media end-to-end and leave the "metadata" — notes, tags, location tracks — server-readable, because doing so keeps search, filtering and geofencing easy.

For this product that split is indefensible. A location track showing a user outside a specific building at a specific hour, or a note reading "officer refused to give badge number", is not metadata. It is the evidence. Leaving it readable means a subpoena or a breach yields the substance of what a user documented, no matter how well the video is protected.

## Decision
Client-side encryption covers **media, snapshots, notes, tags, and location tracks**, with the consequences accepted explicitly rather than discovered later:

- **Geofencing is evaluated on-device** (`LOC-003`); the server never sees coordinates.
- **Note search runs against a client-maintained encrypted index**; the server cannot search text.
- **Thumbnails are generated and encrypted at capture time**; the server cannot produce them.
- **Tag filtering uses blinded tokens** — `HMAC(tag-index key, normalized tag)` — so the server can filter by equality without learning tag names.
- Location tracks are batched into encrypted track segments carrying the same chain properties as media, so a location timeline is evidence of the same quality as the video.

The residual server-visible surface is documented in ARCHITECTURE.md §8.5 and must be kept current: account identity, artifact existence/size/timing/ordering, circle membership graph, blinded tag tokens (equality and frequency), and alert routing.

## Consequences
- (+) Compulsion or compromise of the service yields ciphertext plus a documented metadata surface — no user's substantive record.
- (+) The privacy claim in marketing matches the system's actual behaviour, which matters for a product whose users are choosing it under duress.
- (−) Real capability loss: no server-side search, geofencing, thumbnailing, or transcoding. Several features become client work that would otherwise have been free.
- (−) Blinded tag tokens leak equality and frequency patterns. That is a deliberate, documented trade for MVP-scale filtering, not a claim of perfection.
- (−) Client-side indexes must be built, synced across devices, and kept consistent — real engineering that a server-side index would have absorbed.

## Alternatives Considered
- **Media-only E2EE**: substantially easier and it hands over the most legally sensitive material in plaintext.
- **Searchable encryption schemes** (beyond blinded equality tokens): stronger query support with materially more complexity and its own leakage profile; revisit if search becomes a differentiator.
- **Server-side processing under an enclave**: shifts trust to a hardware vendor and an attestation chain; not credible against our threat model, and not self-hostable.
