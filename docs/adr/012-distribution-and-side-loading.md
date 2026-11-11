# ADR-012: Multi-Channel Distribution and Side-Loading Strategy

## Status
Accepted

## Context
The product's existential requirement is that it remain installable under political pressure on app stores. The platform landscape has moved against that:

- **Android developer verification.** Certified Android devices (Android 8+) carry an Android Developer Verifier system service, and Google is phasing in a requirement that installed apps come from verified developers. The rollout is staged: first a high-friction "advanced flow" for installing apps from unverified developers — enable developer mode, restart, wait 24 hours, re-authenticate — then **hard enforcement**, beginning in Brazil, Indonesia, Singapore and Thailand, where certified devices block normal installs of apps from unregistered developers, from Play *and* from OEM stores, and expanding worldwide after that. Registration is open.
- **iOS.** No alternative distribution in the US. The EU (DMA) and Japan (MSCA) have Apple-authorized marketplaces and web distribution, all still Apple-notarized.
- **Web.** Unrestricted.

"Just ship an APK" is therefore no longer a complete strategy. Verification is identity registration rather than app review — but it creates a revocation capability, which is precisely the kind of leverage this project must plan around.

## Decision
Distribution is engineered as a feature, across parallel channels:

| Channel | Priority | Notes |
|---|---|---|
| Direct APK + in-app self-updater | P0, day 1 | An APK without an update channel becomes a stale, dangerous artifact; the updater ships in the *first* public build. Signed manifest on our domain, mirrored |
| Own F-Droid repository | P0 | Our repo, our signing, no main-repo policy gate; hosts proprietary builds fine |
| Obtainium | P1 | Needs only stable release URLs and versioning discipline |
| Google Play | P1, parallel | "Personal safety" positioning; stealth features compiled out (ADR-013) |
| AOSP ROMs (GrapheneOS/CalyxOS) | P1 | Unaffected by Google verification, and disproportionately our audience |
| iOS App Store + TestFlight | P1 (Phase 2) | Store-clean build; expect review friction on background behaviours |
| EU / Japan alternative distribution | P2 | When entity eligibility and volume allow |
| Blazor PWA | P0 | The floor that cannot be blocked |
| `adb install` | documented | Advanced-user escape hatch, still available under the new rules |

**Time-critical actions, starting immediately:**
1. Complete LLC formation, then Google developer verification. Play Console organization accounts require a **D-U-N-S number** — free, but with lead time measured in weeks. This is the longest-lead item in the project and it is gated by legal formation, not engineering.
2. **Signing-key ceremony** — keys generated offline, SHA-256 certificate fingerprints published, custody documented. Key custody cannot be retrofitted after the first external install.
3. Update-manifest endpoint and mirror, live before the first user installs.
4. Reproducible builds as a stated goal, targeted in Phase 3.

**Channel feature flags**: `SIDELOAD` (full features), `PLAY`/`APPSTORE` (store-safe), `FDROID`. The capture core is identical in every channel; only surface features flag.

## Consequences
- (+) No single gatekeeper can remove the product; users retain at least one install path in every plausible scenario.
- (+) Publishing signing fingerprints and shipping an updater from build one establishes verifiable provenance before there is any user base to protect.
- (−) Multiple channels multiply release engineering, and each has its own compliance surface.
- (−) Registering for developer verification means accepting an identity relationship with Google that is itself a revocation vector. We take it because the reach matters, and we mitigate with the channels that do not depend on it.
- (−) Some users will land on the degraded PWA. That must be an honest, well-built experience rather than a token fallback.

## Alternatives Considered
- **Play-only distribution**: maximal reach, and it concedes the entire premise of the product.
- **Side-load only, no stores**: ideologically clean; forfeits the mainstream users who need this most and who will never enable developer mode and wait 24 hours.
- **Waiting to see how enforcement lands**: the enforcement schedule is published and the lead times are long. Waiting is a decision to be unverified when it starts.
