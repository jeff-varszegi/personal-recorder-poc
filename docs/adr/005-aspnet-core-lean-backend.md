# ADR-005: ASP.NET Core Backend with Minimal MVP Infrastructure

## Status
Accepted

## Context
The service's job is narrower than it first appears. Because content is end-to-end encrypted (ADR-008), the server cannot transcode, thumbnail, index text, or evaluate geofences. What it actually does is: accept encrypted blobs reliably, hold metadata, route alerts, watch heartbeats, enforce retention, and countersign chain heads.

That is a small workload with a large reliability requirement — and it is run by a very small team, for users whose safety depends on it. Every infrastructure component added is a component that can page someone at 3 a.m.

There is also a strategic constraint: a system that self-hosts trivially is a system that cannot be shut down by pressuring one provider.

## Decision
ASP.NET Core (Minimal APIs) on .NET 10, with the entire MVP footprint being **one process + PostgreSQL 17 + S3-compatible object storage**:

| Concern | MVP | Deferred until measured need |
|---|---|---|
| API | Minimal APIs + OpenAPI | — |
| Real-time | SignalR (heartbeat, presence, alerts, live-view notify) | Redis backplane when multi-node |
| Data | EF Core + Npgsql | Read replicas; time-partitioned segment/location tables |
| Object store | S3 API — MinIO for dev and self-host, any S3-compatible provider in production | Lifecycle rules for tiering |
| Background work | Hosted services + PostgreSQL outbox; `LISTEN/NOTIFY` for pub/sub | MassTransit + RabbitMQ |
| Cache | None | Redis |
| AuthN | ASP.NET Core Identity + JWT, TOTP 2FA | Passkeys (Phase 2) |

Hard requirement: **vendor neutrality** — the S3 *API* rather than provider-specific SDK idioms, SMTP for mail, pluggable push (ADR-011) — so the whole backend can move providers or on-premises under pressure. `.NET Aspire` is not adopted; `docker compose` stays canonical because it doubles as the self-hosting artifact.

## Consequences
- (+) `docker compose up` yields a complete, working, self-hostable system.
- (+) Fewer moving parts means fewer failure modes and a smaller attack surface for a service that is itself a target.
- (+) A documented scale path exists without paying for it now.
- (−) PostgreSQL does triple duty (data, queue, pub/sub) at MVP scale. This must be monitored, with the split points identified in advance rather than discovered under load.
- (−) Workers run in the API process at MVP; a runaway worker can affect request latency until they are split out.

## Alternatives Considered
- **The full component stack from day 1** (Redis, RabbitMQ, MassTransit): operationally heavier than MVP scale justifies, and it would make the self-host story a deployment project rather than one command.
- **Serverless / managed-cloud-native**: excellent operational economics, directly opposed to the self-hosting and vendor-neutrality requirements that exist because of our threat model.
- **Node.js or Go**: no advantage here, and both discard the single-language property that lets the evidence implementation be shared with the clients and the verifier.
