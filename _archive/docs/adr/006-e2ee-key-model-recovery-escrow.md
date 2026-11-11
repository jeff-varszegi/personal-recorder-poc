# ADR-006: E2EE Key Model with Default Recovery-Key Escrow

## Status
Accepted (2026-07-31)

## Context
All content is E2EE (STR-008, SEC-001); the server is untrusted with plaintext. Keys must support circle sharing (wrap, don't re-encrypt), survive device loss (evidence that dies with a confiscated phone serves no one), and offer a true zero-knowledge mode for users who accept unrecoverable-loss risk. Spec §9.2 sketched zero-knowledge as the option; the recoverability default needed a decision.

## Decision
- Content crypto via **libsodium** (mature bindings across .NET, and libsodium.js for the Blazor client).
- Per-user **X25519 identity keypair**; private key material wrapped by OS keystore/Secure Enclave where hardware supports it.
- Per-recording symmetric **DEK**, sealed-box-wrapped to the owner and to each authorized circle member/device. Circle sharing and revocation operate on key wraps, never on content.
- Per-segment integrity: SHA-256 hash chain + periodic **Ed25519** signatures (feeds ADR-005 and the verifier CLI).
- **Recovery-key escrow ON by default**: a printable/exportable recovery code wraps the identity key; the server stores only a blob it cannot open. Explicit **zero-knowledge hard mode** opt-out disables escrow with clear unrecoverable-loss warnings.
- Group-messaging E2EE starts as sealed-box fan-out; evaluate MLS (RFC 9420) when messaging volume justifies it.

## Consequences
- (+) Device loss ≠ evidence loss for default users; the at-risk minority can still choose pure zero-knowledge.
- (+) One primitive family (libsodium) across every component; hardware-backed custody where available.
- (−) Recovery-code UX must be aggressive (onboarding + periodic nags) or escrow is theater.
- (−) Honest limitation to document: server still sees metadata (traffic timing, circle graph, artifact existence) — E2EE bounds content exposure, not traffic analysis.

## Alternatives Considered
- **Zero-knowledge by default**: purity at the cost of catastrophic-loss stories for exactly the users this app serves.
- **Server-held keys with policy controls**: simplest UX; unacceptable under the subpoena/pressure threat model.
- **Passphrase-derived keys only** (no hardware wrap): portable but weaker against device compromise.
