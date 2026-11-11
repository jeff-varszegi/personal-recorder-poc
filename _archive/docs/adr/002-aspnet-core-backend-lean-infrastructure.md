# ADR-002: ASP.NET Core Backend with Lean MVP Infrastructure

## Status
Accepted (2026-07-31) — supersedes the informal 2026-01-16 "Fastify" decision (never authored as an ADR) and trims the spec §4.3 stack

## Context
The backend was already redirected from Node.js/Fastify to .NET during initial planning (deep team competency, SignalR for real-time, true multi-threading). The remaining question was infrastructure footprint: spec §4.3 listed Redis, MassTransit + RabbitMQ, and cloud-vendor SDKs. Every component is an operational liability for a small team running a service people's safety depends on, and a trivially self-hostable deployment is itself a censorship-resistance feature (spec Q2).

## Decision
ASP.NET Core (Minimal APIs) on .NET 10, with the MVP reduced to **PostgreSQL + S3-compatible object storage (MinIO for dev/self-host) + one ASP.NET Core process**:

- Real-time: SignalR (heartbeat, presence, alerts, live-view signaling)
- Data: EF Core + Npgsql (PostgreSQL 17)
- Background work: .NET hosted services + PostgreSQL-backed outbox/queue
- AuthN: ASP.NET Core Identity + JWT, TOTP 2FA
- Contracts: shared C# project (`PersonalRecorder.Contracts`) consumed by service, clients, and web

Deferred until a measured need or multi-node deployment: Redis (cache/backplane), MassTransit + RabbitMQ. Hard requirement: vendor neutrality — S3 *API* only, SMTP for mail, pluggable push (ADR-007), so the backend can move providers or on-prem under pressure.

## Consequences
- (+) `docker compose up` gives a complete, self-hostable system; fewer failure modes; smaller attack surface.
- (+) Clean scale path documented (backplane, broker, replicas) without paying for it now.
- (−) PostgreSQL does triple duty (data, queue, pub/sub) at MVP scale; must monitor and split when load demands.

## Alternatives Considered
- **Node.js/Fastify** (original): weaker fit for team competency and real-time/multi-threaded workloads.
- **Full spec §4.3 stack from day 1**: operational overhead without MVP-scale justification.
- **Serverless/managed-cloud-native**: conflicts with self-hosting and vendor-neutrality requirements.
