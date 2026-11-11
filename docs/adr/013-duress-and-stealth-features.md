# ADR-013: Duress PIN and Stealth Features, Side-Load Channel Only

## Status
Accepted

## Context
Two of the product's core scenarios end with someone else holding the user's unlocked phone: a detention in which an officer demands the device, and a domestic-violence situation in which an abuser checks it.

Encryption at rest does nothing in either case, because the device is unlocked. What is needed is a way for the app to present an innocuous state to a coercing observer while the evidence that has already reached storage stays safe — and, critically, while the user is not caught doing it.

This is also the feature most likely to cause an app-store rejection and the most open to misuse framing, so its availability is a channel decision as much as a technical one.

## Decision
- **Duress PIN**: an alternate unlock code that opens the app into a **decoy state** — an empty or innocuous artifact list, no recording history, no circle. It never deletes cloud-side evidence.
- Entering the duress PIN optionally emits a **silent circle alert**, indistinguishable on-device from a normal unlock.
- **Silent panic** (`EMR-007`) and **stealth recording UI** (`PRV-003/004`) sit in the same family: no visible indication, quick-exit gestures, screen-off operation.
- **Availability is channel-gated**: compiled into `SIDELOAD` builds only; compiled out of `PLAY` and `APPSTORE` builds (ADR-012).
- **Default off**, with onboarding that explains both the protection and its limits.
- Local deletion is always *presentation-level only*. The app must never destroy evidence in response to an unlock code, because a coerced user pressing the wrong thing must not be able to erase the record — and because destroying evidence on command has legal implications for the user we will not build in silently.

## Consequences
- (+) Directly addresses the two scenarios where the product is otherwise defeated by simple physical coercion.
- (+) Keeping deletion out of the mechanism means duress mode can never be turned into an evidence-destruction tool by an adversary who learns the PIN.
- (−) A decoy state that looks wrong under scrutiny is worse than none. It must be convincing — plausible install date, plausible emptiness — and that is real design work.
- (−) Store builds differ in behaviour from side-loaded builds, which complicates support ("my duress PIN option is missing").
- (−) Some jurisdictions may treat a decoy state as obstruction. Onboarding must be honest that this is a safety tool with legal risk, and we ship no legal advice.

## Alternatives Considered
- **No duress features**: simplest, store-clean, and it abandons users at the exact moment they are most exposed.
- **Duress wipe** (destroy local data on the duress PIN): satisfying and dangerous — it converts a coerced unlock into permanent evidence loss and hands an adversary a destruction command.
- **Ship duress features in all channels**: uniform behaviour, at meaningful risk of store rejection for the whole app; the side-load channels exist precisely so this trade does not have to be made globally.
