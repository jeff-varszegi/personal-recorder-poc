# ADR-001: .NET 10 LTS Across the Stack

## Status
Accepted

## Context
Every component — capture cores, shared logic, service, web, verifier — is C#. The version policy therefore applies to the whole system at once, and it interacts with a specific fact: **.NET 11 is a Standard Term Support (two-year) release, and in .NET 11 CoreCLR replaces Mono as the default runtime for .NET MAUI on Android, iOS and Mac Catalyst.**

That runtime swap is a genuine long-term improvement. It is also exactly the kind of change one does not want landing underneath a life-safety application during its first shipping months.

## Decision
- All projects target `net10.0` / `net10.0-android` / `net10.0-ios`. .NET 10 is the current LTS release, with a three-year support window.
- **The solution moves LTS-to-LTS.** STS releases are treated as previews: built against in CI to stay ahead of migrations, never shipped.
- .NET 11 compatibility is validated in CI during Phase 3 hardening. The next production hop is the following LTS (.NET 12).
- `global.json` pins the SDK band; a single `TargetFramework` policy is enforced by an architecture test, so moving off .NET 10 requires a superseding ADR rather than a project-file edit.

## Consequences
- (+) One runtime version across the system; no cross-component version skew to reason about.
- (+) The MAUI CoreCLR migration is something we adopt deliberately, with the MVP already shipped and measurable.
- (−) We forgo .NET 11 features and performance work for roughly a year.
- (−) Staying on an older LTS means eventually absorbing two years of change at once; the CI-against-STS rule exists to keep that from being a surprise.

## Alternatives Considered
- **Track the latest release always**: newest features and fixes, at the cost of riding a runtime swap through MVP on the exact component (MAUI on mobile) that carries our highest platform risk.
- **Mixed versions** (service on latest, clients on LTS): plausible for a service-only shop, but the shared `Contracts` and `Evidence` libraries run in every host — version skew there would be a recurring tax.
