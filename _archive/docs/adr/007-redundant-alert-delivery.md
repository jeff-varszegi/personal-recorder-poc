# ADR-007: Redundant Alert Delivery Channels

## Status
Accepted (2026-07-31)

## Context
Panic and missed-check-in alerts (EMR-004, COT-007, HB-003) are the moments the product exists for. Google/Apple push infrastructure is a single point of failure *and* a pressure point in our threat model (platform coercion). Our most at-risk users disproportionately run de-Googled devices (GrapheneOS/CalyxOS) with no FCM at all.

## Decision
Alert fan-out attempts every available channel per recipient, first-success-wins per device, all-channels for panic:

1. **FCM** (standard Android) and **APNs** (iOS — no alternative exists; dependency documented honestly)
2. **UnifiedPush** (self-hostable, e.g., ntfy) as a first-class channel for de-Googled devices — not an afterthought
3. **SignalR** direct delivery whenever a recipient's app holds a live connection
4. Optional **SMS bridge** (pluggable provider, e.g., Twilio) as paid last-resort fan-out for panic alerts

Delivery is tracked per channel; undelivered panic alerts escalate visibly (no silent failures, REL-002).

## Consequences
- (+) No single platform owner can silently sever the alert path; de-Googled users are first-class.
- (+) Self-hosted deployments get a complete alert story (UnifiedPush + SignalR + SMS) with zero Google/Apple dependence on Android.
- (−) More moving parts per alert; requires per-channel delivery telemetry from day 1.
- (−) iOS remains APNs-bound for background wake — an honest, documented platform limit.

## Alternatives Considered
- **FCM/APNs only**: simplest; unacceptable single chokepoint under the threat model.
- **Persistent self-managed socket on Android**: battery cost and Doze-mode fragility; UnifiedPush already solves this with a shared distributor.
- **SMS-first**: costly, spoofable, no payload security; fine as last resort only.
