# ADR-014: Open-Source Posture — Verifier and Evidence Format Are FOSS

## Status
Accepted

## Context
The audience for this product includes people pointing a recording app at their own government. "Trust us" does not scale to that audience: *is this spyware?* and *can this recording be trusted?* are questions they are right to ask and should not have to take our word on.

Full open-sourcing of the client is the strongest possible answer, and it was considered. It was deferred over copyleft lock-in concerns and the practical realities of a pre-revenue company. That leaves a real question: what is the minimum that must be open for the trust claims to be true rather than marketed?

## Decision
- **Open source from day 1**: the `PersonalRecorder.Verifier` CLI and the **evidence format specification** (container, chain, signature scheme, export bundle). Independent verification that depends on our code being trusted is not independent verification.
- **Proprietary for now**: client, service and web. Revisit around Phase 3, when the security audit lands and the entity's position is clearer. A permissive license (MIT/Apache) with permissively-licensed dependencies remains available then — the deferral is not a commitment to stay closed.
- The trust burden that open-sourcing the client would have carried is instead carried by: the open verifier, **published signing-key fingerprints**, **reproducible builds** (Phase 3), the **external security audit**, and the documented threat model with its honest metadata-leakage table.
- Consequence accepted: the **main F-Droid repository is off the table** while the client is proprietary. Our own F-Droid repo is unaffected and can host proprietary builds (ADR-012).

## Consequences
- (+) The evidentiary claim — completeness, integrity, authorship, time — is checkable by anyone, forever, with no dependence on us or our servers continuing to exist.
- (+) Licensing flexibility is preserved for a decision made with more information.
- (−) "Is this spyware?" cannot be answered by third-party audit of the client today. Reproducible builds and the external audit are partial substitutes, and they are weaker ones. This is the real cost.
- (−) Contributors cannot help with the client, which worsens the solo bus-factor risk.
- (−) The `Evidence` library ships open while the client that uses it does not; the licensing boundary must be clean from the start, not retrofitted.

## Alternatives Considered
- **Full FOSS client from Phase 1**: the strongest trust position and the best answer to the audience's central question; deferred over copyleft lock-in concerns, to be revisited at Phase 3.
- **Everything proprietary including the verifier**: coherent commercially, and it would make the evidentiary claim unverifiable — which is the one claim the product cannot afford to weaken.
- **Core-OSS server**: makes self-hosting real for organizations with compliance needs; worth revisiting alongside the Phase 3 decision.
