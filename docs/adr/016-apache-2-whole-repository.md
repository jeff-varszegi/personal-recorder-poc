# ADR-016: Apache License 2.0 for the Whole Repository

## Status
Accepted. Supersedes the "proprietary for now" clause of [ADR-014](./014-open-source-posture.md) and the open-source boundary in [ADR-015](./015-consolidated-layout-and-service-stack.md) Decision 1. ADR-014's permanent commitment — the verifier and the evidence format are open — stands.

## Context
ADR-014 opened the verifier and the evidence format and kept the client, service and web proprietary until Phase 3. That split has three costs, and they are in front of us now rather than deferred: the trust question ADR-014 itself names ("is this spyware?") cannot be answered by reading the code; a repository with proprietary directories cannot be shared, reviewed or reused as a whole; and the boundary has to be policed per directory.

What the split protects is small. The repository holds a scaffold, contracts and the hash chain. The things that will carry the product's value — the operated service, the signing keys, the distribution channels, the brand — are not protected by a source license in any case, and a malicious fork is defeated by published signing fingerprints (ADR-012), not by license terms.

The copyright is held by one person. Released versions stay under the license they were released under; future versions can be licensed differently if control over forks turns out to matter, which is a decision the first public release can take with real information.

## Decision
- The whole repository is licensed under the **Apache License 2.0**: a root `LICENSE` holding the canonical text, a `NOTICE`, and `PackageLicenseExpression` set solution-wide in `Directory.Build.props`.
- Copyright holder: **Jeffrey Varszegi**, named in `NOTICE`, the README and the assembly metadata. Assignment to a company entity, when one is formed, is a later change.
- Apache-2.0 rather than MIT, for three clauses MIT lacks: no trademark rights are granted (§6), so the name and brand stay with the author; an explicit patent grant and retaliation (§3), which matters for an evidence format third parties are meant to rely on; and NOTICE preservation (§4).
- `PersonalRecorder.Core` and `PersonalRecorder.Verifier` are Apache-2.0 permanently, per ADR-014.
- The license of `App`, `Client`, `Service` and `Web` is re-examined before the first public release. **AGPL-3.0** is the pre-agreed alternative if control over forks is needed then: it keeps forks open and auditable, and the copyright holder can still license commercially.
- Any outside contribution is accepted only under a contributor license agreement, so that the relicensing option above remains real.

## Consequences
- (+) The "is this spyware?" question is answerable by anyone who can read C#. The real cost ADR-014 accepted is gone.
- (+) One license and no per-directory boundary to police; the main F-Droid repository becomes an option alongside our own (ADR-012).
- (+) Patent and trademark positions are explicit rather than implied.
- (−) Everything released under this decision is permissively licensed forever, including to competitors. Accepted: what exists today is a scaffold, and the moat is elsewhere.
- (−) Tightening later needs every contributor's agreement; the CLA requirement exists for that reason, and today the contributor count is one.
- (−) Apache-2.0 code cannot be combined into GPLv2-only works. No such dependency or consumer exists.

## Alternatives Considered
- **MIT**: accepted everywhere Apache-2.0 is; silent on trademarks and patents, which are the two things worth keeping.
- **AGPL-3.0 for the product projects, Apache-2.0 for Core and Verifier**: the strongest "keep control" position that is still open source, and the pattern of comparable safety products. Set aside for now because the code is a scaffold and permissive terms travel furthest; retained as the pre-agreed alternative at release.
- **A source-available license (Functional Source License, Business Source License)**: literal control for a term, then automatic conversion; not open source by the OSI definition, which defeats the purpose of opening the repository.
- **Keep ADR-014's split**: the costs above, for protection the split does not actually provide.
