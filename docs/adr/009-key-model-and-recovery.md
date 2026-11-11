# ADR-009: Key Hierarchy with Recovery-Key Escrow On by Default

## Status
Accepted

## Context
Keys must do four things at once: keep the server unable to read content, let circle members be granted and revoked access cheaply, survive the loss or seizure of a device, and still offer a true zero-knowledge mode for users who accept unrecoverable loss.

The hard call is the default. Pure zero-knowledge is the purist answer, and it produces a specific failure story: a user is detained, their phone is taken, they cannot remember their password, and the evidence they risked something to capture is gone forever. Evidence that dies with the device serves no one.

## Decision

```
Identity keypair (X25519)  — per user; private key wrapped by OS keystore / Secure Enclave
   ├── Device signing key (Ed25519)  — per device; signs chain heads
   ├── Recording DEK (AES-256)       — per recording; sealed-box wrapped to owner,
   │                                   authorized circle members/devices, capability links
   ├── Artifact DEK                  — per snapshot / note / location track
   ├── Tag index key                 — derives blinded tag tokens (ADR-008)
   └── Recovery key                  — printable code; wraps the identity key
```

- Primitive family: **libsodium**, across .NET, WebAssembly and tooling.
- Per-segment keys derive from the recording DEK via HKDF, so a leaked segment key exposes one segment (ADR-007).
- Circle sharing and revocation operate on **key wraps**, never by re-encrypting content.
- **Recovery-key escrow is ON by default**: a printable, exportable recovery code wraps the identity key; the server stores only a blob it cannot open. An explicit **zero-knowledge hard mode** disables escrow with unambiguous unrecoverable-loss warnings.

## Consequences
- (+) Device loss or seizure does not mean evidence loss for the default user, while the at-risk minority keeps a real zero-knowledge option.
- (+) One primitive family everywhere, with hardware-backed custody wherever the device provides it.
- (+) Revocation is cheap and instant, because it never touches stored content.
- (−) Recovery-code UX must be aggressive — onboarding plus periodic re-verification — or escrow is theater. This is a product requirement, not a nice-to-have.
- (−) The escrowed blob is an asset worth attacking; its protection and the recovery flow both need to survive the external security audit.

## Alternatives Considered
- **Zero-knowledge by default**: cryptographic purity, purchased with catastrophic-loss stories for exactly the users this product exists to serve. Available as an opt-in instead.
- **Server-held keys with policy controls**: the best UX by far, and it fails the central threat-model question about subpoena and pressure.
- **Passphrase-derived keys only, no hardware wrap**: portable across devices, materially weaker against a compromised or seized device.
