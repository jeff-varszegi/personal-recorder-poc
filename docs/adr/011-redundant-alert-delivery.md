# ADR-011: Redundant Alert Delivery Across Five Channels

## Status
Accepted

## Context
Panic alerts and missed-check-in escalations (`EMR-004`, `COT-007`, `HB-003`) are the moments this product exists for. Everything else can degrade; this cannot.

Two facts shape the design. First, Google and Apple push infrastructure is both a single point of failure and, in our threat model, a pressure point — a platform owner under political pressure can sever alert delivery without touching the app itself. Second, the users most likely to need this product disproportionately run de-Googled devices (GrapheneOS, CalyxOS) where FCM does not exist at all.

## Decision
Alert fan-out attempts every available channel per recipient — first success wins per device, all channels for panic:

1. **FCM** (standard Android) and **APNs** (iOS — no alternative exists; the dependency is documented rather than glossed over).
2. **UnifiedPush** (self-hostable, e.g. ntfy) as a first-class channel for de-Googled devices, not an afterthought.
3. **SignalR** direct delivery whenever a recipient's app holds a live connection — the lowest-latency path and the only one with no third-party dependency.
4. **SMS bridge** (pluggable provider) as an optional, paid last resort for panic alerts.

Delivery is tracked **per channel per recipient**. Undelivered panic alerts escalate visibly to the sender; a silently undelivered alert is the worst failure this system can have (`REL-002`).

Alert payloads are sealed to recipient keys; the server routes ciphertext and learns only who was alerted and when.

## Consequences
- (+) No single platform owner can silently sever the alert path.
- (+) Self-hosted deployments get a complete alert story with zero Google or Apple dependence on Android.
- (+) De-Googled users are first-class rather than tolerated.
- (−) More moving parts per alert, and per-channel delivery telemetry is required from day one rather than added later.
- (−) iOS remains APNs-bound for background wake. Stated honestly in the threat model; no engineering fixes it.
- (−) The SMS channel costs money and carries no payload security, so it conveys "open the app" and nothing more.

## Alternatives Considered
- **FCM/APNs only**: simplest, and it concedes the exact chokepoint the threat model says will be used against us.
- **A persistent self-managed socket on Android**: battery cost and Doze-mode fragility, when UnifiedPush already solves this with a shared distributor.
- **SMS-first**: expensive, spoofable, no payload security. Fine as a last resort, wrong as a primary.
