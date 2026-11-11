# Personal Recorder: Personal Recording & Emergency Response System

## Technical Specification Document
**Status:** Requirements baseline — Phase 0 (Foundations). Architecture ratified; see [docs/ARCHITECTURE.md](./docs/ARCHITECTURE.md).

> **How to read this document.** This is the *what*: requirements, their IDs, and their
> priorities. The *how* — technology choices, component structure, evidence format,
> cryptography, distribution, phasing — lives in [docs/ARCHITECTURE.md](./docs/ARCHITECTURE.md)
> and the [ADRs](./docs/adr/). Where the two disagree on design, the architecture document wins;
> where they disagree on requirements, this one does.

---

## 1. Executive Summary

Personal Recorder is a personal recording and emergency-response system designed for legal observers, witnesses of government activities, and individuals in vulnerable situations. The system prioritizes secure evidence preservation, mutual support networks, and rapid emergency response.

### Primary Use Cases
- **Legal Observers/Witnesses**: Documentation of government agent interactions, including unlawful treatment, illegal vehicle entry, and false arrests
- **Domestic Violence Survivors**: Discreet emergency triggering and evidence collection
- **Home-Alone Seniors**: Medical emergency response with check-in features
- **General Personal Safety**: Evidence-backed emergency response for any citizen

### Design Philosophy
> "Evidence that cannot be deleted, confiscated, or suppressed; help that cannot be blocked or delayed."

---

## 2. Competitive Analysis

### 2.1 ACLU Mobile Justice (Defunct)
**Status**: Discontinued; some local ACLU chapters may host older versions

**Features (Historical)**:
- Video recording with automatic upload to ACLU servers
- Know Your Rights information
- Incident reporting
- Local ACLU affiliate notifications

**Gaps Relevant to Personal Recorder**:
- No circle-of-trust features
- No heartbeat/check-in system
- No real-time streaming during recording
- No mutual aid or conferencing
- Centralized (ACLU-dependent) infrastructure

---

### 2.2 Citizen App
**Status**: Active (Freemium model)

**Features**:
| Feature | Free | Premium |
|---------|------|---------|
| Real-time 911 incident alerts | ✓ | ✓ |
| Live video streaming from incidents | ✓ | ✓ |
| User-submitted incident reports | ✓ | ✓ |
| Police/Emergency radio listening | ✗ | ✓ |
| Historical incidents (>24 hours) | ✗ | ✓ |
| Registered offender maps | ✗ | ✓ |
| Customizable alert radius | ✗ | ✓ |
| Family plan sharing | ✗ | ✓ |

**Strengths**:
- Excellent real-time incident awareness
- Large user base for crowdsourced information
- Live video broadcasting capability
- Speed advantage over official alerts (Amber Alerts, etc.)

**Gaps Relevant to Personal Recorder**:
- ❌ Not designed for personal evidence preservation
- ❌ No rolling buffer / lookback recording
- ❌ No heartbeat check-in system
- ❌ No panic button for personal emergencies
- ❌ No end-to-end encrypted storage for users
- ❌ No mutual aid / circle-of-trust coordination
- ❌ Passive consumption focus vs. active personal protection

---

### 2.3 Life360
**Status**: Active (Freemium, ASX-listed company)

**Features**:
| Feature | Description |
|---------|-------------|
| Location Sharing | Real-time GPS tracking visible to circle members |
| Circles | Multiple groups (family, friends, teams) with separate visibility |
| Places (Geofences) | Alerts when members enter/leave defined locations |
| Crash Detection | Automatic 911 contact on detected car accidents |
| Bubbles | Approximate location sharing (1-25 mile radius) for privacy |
| Tile Integration | Bluetooth tracker support for items |
| Driver Reports | Speed, phone usage while driving |
| Roadside Assistance | Premium feature (Driver Protect) |
| 24/7 Live Advisor | Premium emergency support |

**Strengths**:
- Mature circle/group management
- Crash detection with automatic emergency calling
- Geofence notifications
- Large established user base (48M+ MAU)

**Gaps Relevant to Personal Recorder**:
- ❌ No video/audio recording or streaming
- ❌ No evidence preservation features
- ❌ No rolling buffer storage
- ❌ Designed for family tracking, not incident documentation
- ❌ Privacy concerns (historical data selling, stalkerware classification)
- ❌ No heartbeat/check-in for detention scenarios
- ❌ No conferencing during emergencies

---

### 2.4 Feature Gap Analysis Summary

| Feature | Mobile Justice | Citizen | Life360 | Personal Recorder |
|---------|---------------|---------|---------|--------------|
| Video Recording | ✓ | Broadcast only | ✗ | ✓ |
| Audio Recording | ✓ | ✗ | ✗ | ✓ |
| Cloud Streaming | Upload after | Live broadcast | ✗ | Real-time secure |
| Rolling Buffer | ✗ | ✗ | ✗ | ✓ |
| Heartbeat Check-in | ✗ | ✗ | ✗ | ✓ |
| Location Tracking | Limited | Incident-based | ✓ | ✓ |
| Circle of Trust | ✗ | Limited | ✓ | ✓ |
| Emergency Calling | ✗ | ✗ | Crash only | ✓ |
| Feed Sharing | ✗ | Public | Location only | Private/Secure |
| Conferencing | ✗ | ✗ | ✗ | ✓ |
| End-to-End Encryption | ✗ | ✗ | ✗ | ✓ |
| Side-loadable | Unknown | ✗ | ✗ | ✓ |
| Dashcam Integration | ✗ | ✗ | ✗ | Planned |
| Home Security Integration | ✗ | ✗ | Partial | Planned |

**Conclusion**: No existing application provides the complete feature set required. Personal Recorder addresses a genuine gap in the market.

---

## 3. System Requirements

### 3.1 Functional Requirements

#### 3.1.1 Recording & Streaming
| ID | Requirement | Priority |
|----|-------------|----------|
| REC-001 | Local video recording with configurable quality (720p/1080p/4K) | P0 |
| REC-002 | Local audio recording | P0 |
| REC-003 | Pre-engagement camera warm-up to eliminate startup lag | P0 |
| REC-004 | Configurable lookback buffer (default: 30 seconds, range: 10s-5min) | P0 |
| REC-005 | Real-time secure streaming to cloud on trigger | P0 |
| REC-006 | Continued local recording if network unavailable | P0 |
| REC-007 | Automatic upload of local recordings when connectivity restored | P1 |
| REC-008 | Multiple camera support (front/back simultaneous) | P2 |
| REC-009 | External device support (dashcams) | P2 |
| REC-010 | Home security system integration | P3 |
| REC-011 | **Snapshot capture**: One-tap photo with auto-metadata (timestamp, GPS, orientation) | P0 |
| REC-012 | **Voice-only recording**: Audio capture mode with minimal battery/bandwidth | P0 |
| REC-013 | Voice-to-text transcription for voice recordings | P2 |
| REC-014 | Voice activation trigger for hands-free recording start | P2 |

#### 3.1.2 Storage & Retention
| ID | Requirement | Priority |
|----|-------------|----------|
| STR-001 | Rolling window cloud storage (configurable, default: 7 days) | P0 |
| STR-002 | Auto-disable recording after configurable continuous period without confirmation | P0 |
| STR-003 | User-initiated "pin" to prevent auto-deletion | P0 |
| STR-004 | Automatic archival/compression for older recordings | P1 |
| STR-005 | Monthly review notification for pinned/archived items | P1 |
| STR-006 | Lenient soft-delete policy on backend (30+ days before permanent deletion) | P1 |
| STR-007 | Export functionality (local download, share to legal counsel) | P1 |
| STR-008 | End-to-end encryption for all stored content | P0 |
| STR-009 | **Secure erasure**: Cryptographic deletion ensuring unrecoverable data removal from cloud storage | P1 |

#### 3.1.3 Notes & Annotations
| ID | Requirement | Priority |
|----|-------------|----------|
| NOT-001 | Quick text note entry with automatic timestamp and location | P0 |
| NOT-002 | Voice-to-text note capture option | P1 |
| NOT-003 | Attach notes to existing recordings/snapshots | P0 |
| NOT-004 | Standalone notes (not attached to media) | P0 |
| NOT-005 | Rich text support (bold, lists) | P2 |

#### 3.1.4 Tagging & Organization
| ID | Requirement | Priority |
|----|-------------|----------|
| TAG-001 | Quick-tag interface for all artifacts (recordings, snapshots, notes) | P0 |
| TAG-002 | Pre-defined tag library (traffic-stop, protest, home-entry, verbal-threat, etc.) | P0 |
| TAG-003 | Custom user-defined tags | P0 |
| TAG-004 | Smart tag sorting: most-used and most-recent first | P0 |
| TAG-005 | Full tag browser with search | P0 |
| TAG-006 | Multi-select batch tagging | P1 |
| TAG-007 | Tag-based filtering and search across all artifacts | P0 |
| TAG-008 | Auto-suggested tags based on context (location, time, recent activity) | P2 |

#### 3.1.5 Heartbeat & Check-in
| ID | Requirement | Priority |
|----|-------------|----------|
| HB-001 | Configurable automatic check-in interval during active sessions | P0 |
| HB-002 | Manual check-in button | P0 |
| HB-003 | Missed check-in alerts to circle of trust | P0 |
| HB-004 | Missed check-in preserves rolling window (prevents wipe) | P0 |
| HB-005 | Configurable escalation (1 miss = alert, 2 = emergency) | P1 |
| HB-006 | Monthly/periodic review check-in for archived content | P2 |

#### 3.1.6 Location Services
| ID | Requirement | Priority |
|----|-------------|----------|
| LOC-001 | Real-time GPS tracking during active sessions | P0 |
| LOC-002 | Location history with timeline view | P1 |
| LOC-003 | Geofence alerts (enter/exit) | P2 |
| LOC-004 | Location sharing with circle of trust | P0 |
| LOC-005 | Location attached to all recordings as metadata | P0 |

#### 3.1.7 Emergency Response
| ID | Requirement | Priority |
|----|-------------|----------|
| EMR-001 | Panic button (prominent, one-tap activation) | P0 |
| EMR-002 | Quick-dial overlay for 911 (one additional tap) | P0 |
| EMR-003 | Continue recording during/after emergency call | P0 |
| EMR-004 | Alert circle of trust on panic activation | P0 |
| EMR-005 | Optional auto-start streaming on panic | P0 |
| EMR-006 | Customizable emergency contacts beyond 911 | P1 |
| EMR-007 | Silent panic mode (no visible indication on device) | P1 |

#### 3.1.8 Circle of Trust
| ID | Requirement | Priority |
|----|-------------|----------|
| COT-001 | Create/manage multiple circles | P0 |
| COT-002 | Invite members via secure link/code | P0 |
| COT-003 | Permission levels (view location, view recordings, call on behalf) | P1 |
| COT-004 | Shared recording repository for circle | P1 |
| COT-005 | Real-time feed viewing during emergencies | P0 |
| COT-006 | Call emergency services on member's behalf | P1 |
| COT-007 | Circle-wide alerts on any member's emergency | P0 |
| COT-008 | **Capability-link sharing with non-users**: share an artifact or live feed with a lawyer, journalist or family member who has no account and no app — expiring, revocable, access-logged, key never disclosed to the server ([ADR-010](./docs/adr/010-capability-link-sharing.md)) | P1 |

#### 3.1.9 Communication & Conferencing
| ID | Requirement | Priority |
|----|-------------|----------|
| COM-001 | Text messaging within circles | P1 |
| COM-002 | Voice calling within circles | P1 |
| COM-003 | Video conferencing during emergencies | P1 |
| COM-004 | One-tap "broadcast to circle" for rapid coordination | P1 |

#### 3.1.10 User Controls & Privacy
| ID | Requirement | Priority |
|----|-------------|----------|
| PRV-001 | All features individually enable/disable | P0 |
| PRV-002 | Granular permission controls | P0 |
| PRV-003 | "Stealth mode" - minimal visible UI during recording | P1 |
| PRV-004 | Quick-exit / screen-off gestures | P1 |
| PRV-005 | Data sovereignty options (region selection for storage) | P2 |
| PRV-006 | Full data export (GDPR-style) | P1 |
| PRV-007 | Account deletion with verifiable data destruction | P1 |
| PRV-008 | **Duress PIN**: an alternate unlock code opening the app into an innocuous decoy state, for scenarios where someone else holds the unlocked device. Never destroys evidence. Side-load builds only; default off ([ADR-013](./docs/adr/013-duress-and-stealth-features.md)) | P1 |
| PRV-009 | Optional silent circle alert emitted on duress unlock, indistinguishable on-device from a normal unlock | P1 |

---

### 3.2 Non-Functional Requirements

#### 3.2.1 Performance
| ID | Requirement | Target |
|----|-------------|--------|
| PERF-001 | Camera engagement to recording start | < 500ms |
| PERF-002 | Panic button to stream start | < 2 seconds |
| PERF-003 | Lookback buffer inclusion in stream | 100% (no frames lost) |
| PERF-004 | App background operation | Continuous with minimal battery |

#### 3.2.2 Reliability
| ID | Requirement | Target |
|----|-------------|--------|
| REL-001 | Cloud service uptime | 99.9% |
| REL-002 | Recording reliability (no silent failures) | 99.99% |
| REL-003 | Offline operation capability | Full local recording |

#### 3.2.3 Security
| ID | Requirement | Target |
|----|-------------|--------|
| SEC-001 | End-to-end encryption for stored content | AES-256 |
| SEC-002 | Transport encryption | TLS 1.3 |
| SEC-003 | Zero-knowledge architecture option | P1 feature |
| SEC-004 | Tamper-evident recording (cryptographic chain) | P1 feature |
| SEC-005 | **Independent verification**: a third party can verify completeness, integrity, authorship and time from an exported bundle, using open-source tooling and without contacting our servers ([ADR-007](./docs/adr/007-shared-evidence-library.md), [ADR-014](./docs/adr/014-open-source-posture.md)) | P1 feature |
| SEC-006 | **E2EE covers every artifact type** — media, snapshots, notes, tags and location tracks — not media alone ([ADR-008](./docs/adr/008-e2ee-scope.md)) | AES-256 / X25519 |

---

## 4. Technical Architecture

### 4.1 System Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                         CLIENT LAYER                                 │
├─────────────────┬─────────────────┬─────────────────────────────────┤
│  Mobile App     │   Dashcam SDK   │   Home Security Integration     │
│  (iOS/Android)  │   (Future)      │   (Future)                      │
│  - .NET / C#    │                 │                                 │
│  - Side-loadable│                 │                                 │
└────────┬────────┴────────┬────────┴────────────────┬────────────────┘
         │                 │                          │
         │    HTTPS chunks │     REST API             │
         │                 │                          │
┌────────▼─────────────────▼──────────────────────────▼────────────────┐
│                         API GATEWAY                                   │
│                    (Authentication, Rate Limiting)                    │
└────────┬─────────────────┬──────────────────────────┬────────────────┘
         │                 │                          │
┌────────▼────────┐ ┌──────▼──────┐ ┌────────────────▼────────────────┐
│  MEDIA INGEST   │ │  CORE API   │ │      REAL-TIME SERVICES         │
│  SERVICE        │ │  SERVICE    │ │                                 │
│                 │ │             │ │  - WebSocket Server             │
│  - Chunk ingest │ │  - Users    │ │  - Heartbeat Monitor            │
│  - Blob store   │ │  - Circles  │ │  - Location Tracker             │
│  - Hash chain   │ │  - Recordings│ │  - Alert Dispatcher            │
│                 │ │  - Emergency│ │  - Presence Service             │
└────────┬────────┘ └──────┬──────┘ └────────────────┬────────────────┘
         │                 │                          │
┌────────▼─────────────────▼──────────────────────────▼────────────────┐
│                         DATA LAYER                                    │
├─────────────────┬─────────────────┬─────────────────────────────────┤
│  PostgreSQL     │  Redis          │  Object Storage (S3/GCS)        │
│  (Metadata)     │  (Cache/Pub-Sub)│  (Media Files)                  │
│                 │                 │  - Hot: Recent recordings       │
│                 │                 │  - Cold: Archived (Glacier)     │
└─────────────────┴─────────────────┴─────────────────────────────────┘
```

### 4.2 Mobile Application Architecture

#### Technology Stack Decision: All-C#/.NET Client

> The client is all-C#: a framework-independent, platform-native **capture core** (.NET for
> Android / .NET for iOS) beneath a thin **.NET MAUI** UI shell. Team depth is .NET rather
> than full-stack JavaScript, which rules out React Native and similar stacks. Full rationale
> and framework comparison (including the Godot evaluation):
> [ADR-002](./docs/adr/002-native-capture-core-maui-shell.md) and
> [ARCHITECTURE.md §4.2](./docs/ARCHITECTURE.md).

| Layer | Technology | Notes |
|-------|-----------|-------|
| UI shell | .NET MAUI (`net10.0-android`, later `net10.0-ios`) | Thin, replaceable; Uno Platform is the pre-agreed fallback |
| Shared client logic | `PersonalRecorder.Client.Core` (`net10.0`) | View-models, state machines, sync queue, crypto, API/SignalR clients |
| Capture core | `Capture.Android` / `Capture.Ios` platform libraries | CameraX/Camera2 + MediaCodec; AVCaptureSession + AVAssetWriter; foreground services; keystore/enclave |
| Encryption | libsodium (.NET bindings) | Content crypto; OS keystore for key custody |
| Local store | Encrypted SQLite + encrypted CMAF segment ring | Prioritized persistent upload queue |

Key properties:
1. The capture core never references a UI framework — the shell is swappable in weeks, not months
2. Full-fidelity first-party C# bindings to every platform API we need (.NET for Android/iOS)
3. One language across client, service, web, and contracts
4. Godot was evaluated and rejected for the phone client (C# mobile exports still experimental as of the current stable release, no C# web export, duplicate plugin layer for platform services) — see [ADR-002](./docs/adr/002-native-capture-core-maui-shell.md)

#### Key Components

```
┌─────────────────────────────────────────────────────┐
│                    APP SHELL                         │
├─────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │
│  │   Camera    │  │  Location   │  │  Network    │ │
│  │   Manager   │  │   Service   │  │   Manager   │ │
│  │             │  │             │  │             │ │
│  │ - Warm-up   │  │ - GPS Track │  │ - Streaming │ │
│  │ - Buffer    │  │ - Geofence  │  │ - Sync Queue│ │
│  │ - Record    │  │ - History   │  │ - Offline   │ │
│  └─────────────┘  └─────────────┘  └─────────────┘ │
│                                                     │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │
│  │  Heartbeat  │  │   Circle    │  │  Emergency  │ │
│  │   Service   │  │   Manager   │  │   Handler   │ │
│  │             │  │             │  │             │ │
│  │ - Check-in  │  │ - Sync      │  │ - Panic     │ │
│  │ - Alerts    │  │ - Sharing   │  │ - 911 Dial  │ │
│  │ - Escalate  │  │ - Comms     │  │ - Alerts    │ │
│  └─────────────┘  └─────────────┘  └─────────────┘ │
│                                                     │
│  ┌─────────────────────────────────────────────┐   │
│  │              LOCAL STORAGE                   │   │
│  │  - Encrypted SQLite (metadata)              │   │
│  │  - Encrypted file system (media buffer)     │   │
│  │  - Secure keychain (encryption keys)        │   │
│  └─────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────┘
```

### 4.3 Backend Services Architecture

#### Technology Stack Decision: .NET vs Node.js

| Aspect | .NET (ASP.NET Core) | Node.js |
|--------|---------------------|---------|
| **Language** | C# | JavaScript/TypeScript |
| **Performance** | Excellent (compiled, JIT optimized) | Good (V8, but single-threaded) |
| **Concurrency** | True multi-threading + async/await | Event loop only |
| **Real-time** | SignalR (excellent) | Socket.io/ws (good) |
| **WebRTC Signaling** | SignalR works well | Native ecosystem |
| **Memory** | Efficient, predictable GC | Can be unpredictable |
| **Type Safety** | Native (C#) | TypeScript layer over JS |
| **Team Experience** | Deep competency ✓ | Moderate |

**Decision: .NET (ASP.NET Core)**

Rationale:
1. Deep existing competency reduces development risk
2. Superior performance for real-time streaming and heartbeat monitoring
3. SignalR provides excellent WebSocket/real-time infrastructure
4. True multi-threading for CPU-intensive operations (transcoding, encryption)
5. Strong typing throughout the stack
6. Excellent tooling (Visual Studio, Rider, CLI)

#### Technology Stack (lean MVP — see [ADR-005](./docs/adr/005-aspnet-core-lean-backend.md))
- **Runtime**: .NET 10 (LTS)
- **API Framework**: ASP.NET Core Minimal APIs
- **Real-time**: SignalR for WebSocket, heartbeat, presence, alerts
- **ORM**: Entity Framework Core with PostgreSQL (Npgsql)
- **Object Storage**: S3 API via MinIO (dev/self-host) or any S3-compatible provider
- **Background Work**: .NET hosted services + PostgreSQL-backed outbox (MassTransit/RabbitMQ deferred until multi-node)
- **Cache**: none at MVP (PostgreSQL-first; Redis deferred until measured need)
- **Authentication**: ASP.NET Core Identity + JWT (+ TOTP 2FA)
- **API Documentation**: OpenAPI
- **Contracts**: shared C# project (`PersonalRecorder.Contracts`) consumed by service, clients, and Blazor web — no TypeScript generation needed

### 4.4 Data Models

> *Shapes below are illustrative, in TypeScript notation; canonical definitions land as C# records in `PersonalRecorder.Contracts`.*

#### User
```typescript
interface User {
  id: string;              // UUID
  email: string;
  phone?: string;
  passwordHash: string;
  encryptionPublicKey: string;
  createdAt: Date;
  settings: UserSettings;
  subscription: SubscriptionTier;
}

interface UserSettings {
  recordingQuality: '720p' | '1080p' | '4K';
  lookbackSeconds: number;      // 10-300
  heartbeatIntervalMinutes: number;
  autoDisableMinutes: number;   // Continuous recording limit
  silentPanicEnabled: boolean;
  emergencyContacts: EmergencyContact[];
}
```

#### Circle
```typescript
interface Circle {
  id: string;
  name: string;
  ownerId: string;
  members: CircleMember[];
  sharedRecordings: string[];   // Recording IDs
  createdAt: Date;
}

interface CircleMember {
  userId: string;
  role: 'owner' | 'admin' | 'member';
  permissions: Permission[];
  joinedAt: Date;
}

type Permission = 
  | 'view_location'
  | 'view_recordings'
  | 'view_live_feed'
  | 'call_emergency_on_behalf'
  | 'receive_alerts';
```

#### Recording
```typescript
interface Recording {
  id: string;
  userId: string;
  circleIds: string[];          // Shared with
  type: 'video' | 'audio' | 'snapshot';
  startTime: Date;
  endTime?: Date;
  duration: number;
  status: 'recording' | 'uploading' | 'complete' | 'archived';
  storageClass: 'hot' | 'warm' | 'cold';
  
  // Media
  videoUrl?: string;
  audioUrl?: string;
  thumbnailUrl?: string;
  
  // Metadata
  location: LocationTrack[];
  tags: string[];               // Tag IDs
  notes: string[];              // Note IDs attached to this recording
  isPinned: boolean;
  pinnedAt?: Date;
  
  // Retention
  expiresAt?: Date;
  archivedAt?: Date;
  deletedAt?: Date;             // Soft delete
  
  // Integrity
  hashChain: string[];          // Tamper evidence
  encryptionKeyId: string;
}

interface Snapshot {
  id: string;
  userId: string;
  circleIds: string[];
  capturedAt: Date;
  status: 'uploading' | 'complete' | 'archived';
  storageClass: 'hot' | 'warm' | 'cold';
  
  // Media
  imageUrl: string;
  thumbnailUrl: string;
  
  // Metadata
  location: LocationPoint;
  deviceOrientation: 'portrait' | 'landscape';
  tags: string[];
  notes: string[];
  isPinned: boolean;
  
  // Integrity
  hash: string;
  encryptionKeyId: string;
}
```

#### Note
```typescript
interface Note {
  id: string;
  userId: string;
  circleIds: string[];
  createdAt: Date;
  updatedAt: Date;
  
  // Content
  content: string;
  contentType: 'plain' | 'markdown';
  source: 'typed' | 'voice-to-text';
  
  // Attachments (optional - note can be standalone or attached)
  attachedTo?: {
    type: 'recording' | 'snapshot';
    id: string;
  };
  
  // Metadata
  location: LocationPoint;
  tags: string[];
  isPinned: boolean;
  
  // Integrity
  hash: string;
  encryptionKeyId: string;
}
```

#### Tag
```typescript
interface Tag {
  id: string;
  userId: string;              // null for system-defined tags
  name: string;
  color?: string;              // For UI display
  isSystemTag: boolean;        // Pre-defined vs custom
  usageCount: number;          // For smart sorting
  lastUsedAt: Date;            // For recent sorting
  createdAt: Date;
}

// System-defined tags (pre-loaded)
const SYSTEM_TAGS = [
  { name: 'traffic-stop', color: '#FF6B6B' },
  { name: 'protest', color: '#4ECDC4' },
  { name: 'home-entry', color: '#FFE66D' },
  { name: 'verbal-threat', color: '#FF8C42' },
  { name: 'physical-contact', color: '#E63946' },
  { name: 'badge-request', color: '#457B9D' },
  { name: 'rights-assertion', color: '#2A9D8F' },
  { name: 'witness', color: '#9B5DE5' },
  { name: 'evidence', color: '#00BBF9' },
  { name: 'medical', color: '#F72585' },
  { name: 'property-damage', color: '#FB8500' },
  { name: 'false-arrest', color: '#D90429' },
];
```

---

## 5. Battery & Resource Management

### 5.1 Challenge
Continuous video recording, GPS tracking, and network streaming are extremely power-intensive operations. Unoptimized implementation could drain a typical smartphone battery in 1-2 hours.

### 5.2 Mitigation Strategies

#### 5.2.1 Camera Pre-warming
**Problem**: Cold camera start introduces 1-3 second lag, unacceptable for emergency use.

**Solution**:
```
┌─────────────────────────────────────────────────────┐
│                CAMERA STATES                         │
├─────────────────────────────────────────────────────┤
│                                                     │
│  COLD ──────► WARM ──────► HOT ──────► RECORDING   │
│   │            │            │              │        │
│   │ User opens │ App in     │ User engages │        │
│   │ app        │ foreground │ record mode  │        │
│   │            │            │              │        │
│   ▼            ▼            ▼              ▼        │
│  No power     Low power    Med power     Full power │
│  Camera off   Preview      Buffer        Stream     │
│               running      recording     + buffer   │
│                                                     │
└─────────────────────────────────────────────────────┘
```

- **COLD**: Camera hardware off, zero power consumption
- **WARM**: Camera initialized, preview running at low FPS (10fps), minimal buffer
- **HOT**: Full resolution, continuous rolling buffer (configurable seconds)
- **RECORDING**: Streaming + local recording + buffer preserved

**Transition triggers**:
- COLD → WARM: App opened / Background service started
- WARM → HOT: User taps "Ready" button or enters designated location
- HOT → RECORDING: Panic button or manual record start
- Any → COLD: User explicitly disables or timeout

#### 5.2.2 Adaptive Quality
```typescript
interface AdaptiveQualityConfig {
  batteryThresholds: {
    high: { level: 50, quality: 'configured' },   // User's preference
    medium: { level: 25, quality: '720p' },       // Force downgrade
    low: { level: 10, quality: '480p' },          // Survival mode
    critical: { level: 5, quality: 'audio_only' } // Audio + GPS only
  };
  networkAdaptation: boolean;  // Reduce quality on poor connection
  thermalThrottling: boolean;  // Reduce on device overheating
}
```

#### 5.2.3 Location Service Optimization
```typescript
interface LocationConfig {
  activeRecording: {
    accuracy: 'high',           // GPS
    interval: 1000,             // 1 second
  },
  standbyMode: {
    accuracy: 'balanced',       // Network + GPS
    interval: 30000,            // 30 seconds
  },
  backgroundHeartbeat: {
    accuracy: 'low',            // Network only
    interval: 300000,           // 5 minutes
  }
}
```

#### 5.2.4 Network Efficiency
- **Adaptive bitrate streaming**: Adjust based on connection quality
- **Delta uploads**: For location data, send changes only
- **Batch heartbeats**: Combine multiple signals when possible
- **Compression**: H.265/HEVC for video where supported

#### 5.2.5 Auto-Disable Safety Net
**Scenario**: User forgets app is recording in "HOT" or "RECORDING" state.

**Solution**:
```typescript
interface AutoDisableConfig {
  warningIntervalMinutes: 30;     // "Still recording?" notification
  maxContinuousMinutes: 120;      // Force prompt after 2 hours
  requireConfirmation: true;      // User must tap to continue
  notifyCircle: boolean;          // Alert circle if disabled due to timeout
}
```

---

## 6. Cost Model & Sustainability

### 6.1 Cloud Cost Estimates (Per Active User/Month)

| Component | Usage Assumption | Est. Cost |
|-----------|-----------------|-----------|
| Video Storage (Hot) | 2 GB (7-day rolling) | $0.05 |
| Video Storage (Archive) | 5 GB (Glacier) | $0.02 |
| Streaming Bandwidth | 5 GB transfer | $0.45 |
| API Requests | 100K requests | $0.04 |
| Database | Shared instance | $0.10 |
| Real-time Services | WebSocket hours | $0.15 |
| **Total Infrastructure** | | **~$0.81** |

### 6.2 Pricing Strategy Options

| Tier | Features | Target Price | Margin |
|------|----------|--------------|--------|
| **Free** | 24-hr rolling window, 1 circle, basic features | $0 | Subsidized |
| **Standard** | 7-day window, 3 circles, full features | $4.99/mo | ~$4.00 |
| **Pro** | 30-day window, unlimited circles, priority support | $9.99/mo | ~$9.00 |
| **Organization** | Custom retention, admin dashboard, API access | Custom | Negotiated |

### 6.3 Cost Control Mechanisms

1. **Storage tiering**: Automatic migration to cheaper storage classes
2. **Compression**: Aggressive encoding for archived content
3. **User quotas**: Soft limits with warnings before hard caps
4. **Pinning limits**: Maximum pinned recordings based on tier
5. **Review requirements**: Monthly confirmation to keep archived content

---

## 7. Emergency Calling Implementation

### 7.1 Regulatory Considerations
Direct programmatic 911 calling from apps faces significant regulatory and technical barriers:
- Carrier partnerships required
- Location accuracy requirements (E911)
- Callback capability mandates
- Liability concerns

### 7.2 Recommended Approach: Quick-Dial Overlay

Instead of auto-dialing, Personal Recorder will:

1. **On Panic Button Press**:
   - Start/continue recording and streaming
   - Display prominent overlay with pre-dialed 911
   - Single tap completes the call
   - User has full control over initiating voice contact

2. **Implementation**:
```typescript
async function handlePanicButton() {
  // 1. Ensure recording/streaming active
  await ensureRecordingActive();
  
  // 2. Alert circle of trust
  await alertCircle({ type: 'panic', location: currentLocation });
  
  // 3. Show emergency overlay (does NOT auto-dial)
  showEmergencyOverlay({
    primaryAction: {
      label: 'Call 911',
      action: () => Linking.openURL('tel:911'),
    },
    secondaryActions: [
      { label: 'Call Emergency Contact', ... },
      { label: 'Silent Alert Only', ... },
    ],
    // Overlay persists, recording continues behind it
    persistent: true,
  });
}
```

3. **Benefits**:
   - No regulatory barriers
   - User maintains control
   - Recording continues during/after call
   - Works immediately without carrier partnerships

### 7.3 Future Enhancement: RapidSOS Integration
When scale justifies the partnership cost, integrate with RapidSOS to provide:
- Direct data link to 911 call centers
- Automatic location transmission
- Rich context (user profile, medical info if provided)
- Two-way data channel

---

## 8. Platform & Distribution Strategy

### 8.1 Side-Loading Priority
Given the stated requirement for distribution independent of app store approval:

> **Android landscape note:** Google's developer-verification program requires sideloaded
> apps on certified Android devices to carry a Google-verified developer signature. It is
> rolling out in stages: a high-friction "advanced flow" for unverified apps — developer mode,
> restart, 24-hour wait, re-authentication — followed by hard enforcement, first in Brazil,
> Indonesia, Singapore and Thailand and then worldwide. Full channel strategy, mitigations
> and time-sensitive actions: [ADR-012](./docs/adr/012-distribution-and-side-loading.md)
> and [ARCHITECTURE.md §12](./docs/ARCHITECTURE.md).
> Publishing entity: NimbleJinn LLC (New Hampshire, forming).

#### Android
- **APK Distribution**: Direct download from website + **in-app self-updater from the first public build** (Signal model), mirrored on GitHub Releases
- **Own F-Droid repository**: Our repo, our signing, no main-repo policy gate (main F-Droid repo only if/when open-sourced)
- **Obtainium**: Supported via stable release URLs and versioning discipline
- **Google developer verification**: Register early; AOSP ROMs (GrapheneOS/CalyxOS) unaffected by enforcement
- **Signing transparency**: Published SHA-256 certificate fingerprints; documented key custody

#### iOS
- **App Store + TestFlight**: Primary US channels (store-clean build; safety positioning per §8.2)
- **EU Alternative Distribution**: DMA marketplaces (e.g., AltStore PAL) and web distribution — Apple notarization still applies
- **Japan**: Alternative stores available under the Mobile Software Competition Act
- **US side-loading**: Does not exist today; the PWA is the fallback floor

### 8.2 App Store Strategy (Parallel Track)
Despite side-loading priority, pursue app store presence for discoverability:
- Position as "personal safety" app
- Emphasize legitimate use cases (domestic violence, seniors, general safety)
- Avoid politically charged language in store listings
- Maintain compliant version if needed (reduced features)

### 8.3 Web Application
Blazor WebAssembly PWA (ADR-004) as the unblockable fallback:
- Degraded-mode recording (MediaRecorder, foreground only — no lookback buffer)
- Circle management, E2EE playback (MSE + client-side decryption), emergency console
- Emergency contact quick-dial
- No platform approval required — cannot be de-platformed

---

## 9. Security & Privacy Architecture

### 9.1 Encryption Strategy

```
┌─────────────────────────────────────────────────────────────────┐
│                    ENCRYPTION LAYERS                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Device                    Transport                   Storage  │
│  ┌─────────────────┐      ┌─────────────────┐      ┌─────────┐ │
│  │ Local Recording │      │                 │      │ Cloud   │ │
│  │                 │──────│   TLS 1.3       │──────│ Storage │ │
│  │ AES-256-GCM     │      │                 │      │         │ │
│  │ (Device Key)    │      │ + Certificate   │      │ AES-256 │ │
│  └─────────────────┘      │   Pinning       │      │ (User   │ │
│                           └─────────────────┘      │  Key)   │ │
│                                                    └─────────┘ │
│                                                                 │
│  Key Hierarchy:                                                │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │ Master Key (derived from password + device secret)       │  │
│  │     │                                                    │  │
│  │     ├── Recording Keys (per-recording, rotated)          │  │
│  │     ├── Communication Keys (per-circle, X25519)          │  │
│  │     └── Recovery Key (optional, escrowed)                │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 9.2 Recovery Model (see [ADR-009](./docs/adr/009-key-model-and-recovery.md))

The server never holds decryption keys in either mode:

- **Default — recovery-key escrow ON.** A printable, exportable recovery code wraps the user's identity key; the server stores only a blob it cannot open. Device loss or seizure does not mean evidence loss. Recovery-code UX must be aggressive (onboarding plus periodic re-verification) or the escrow is theater.
- **Opt-in — zero-knowledge hard mode.** Escrow disabled; keys derived client-side only; no recovery path, with unambiguous unrecoverable-loss warnings.

Rationale: evidence that dies with a confiscated phone and a forgotten password serves no one — but the choice stays the user's, and the hard-mode option is real rather than nominal. Under either mode, E2EE bounds *content* exposure only; the server-visible metadata surface is enumerated honestly in [ARCHITECTURE.md §8.5](./docs/ARCHITECTURE.md).

### 9.3 Tamper Evidence
Recordings include cryptographic integrity chain:
```typescript
interface RecordingChunk {
  sequenceNumber: number;
  timestamp: Date;
  dataHash: string;           // SHA-256 of chunk data
  previousChunkHash: string;  // Chain link
  signature: string;          // Signed with user's key
}
```

This allows verification that:
- No chunks were deleted
- No chunks were modified
- Recording is authentic to the claimed user

---

## 10. Development Roadmap

The authoritative roadmap lives in [ARCHITECTURE.md §15](./docs/ARCHITECTURE.md). Summary:

| Phase | Scope | Duration |
|-------|-------|----------|
| 0 — Foundations | ADRs, solution scaffold, CI/CD, Android capture-core spike, E2EE walking skeleton, MAUI shell spike | 3–4 wks |
| 1 — MVP | Android + Blazor web: recording (video/audio/snapshot) with lookback buffer, notes/tags, panic + 911 overlay, single circle, location, retention, distribution v1 (APK + self-updater + own F-Droid repo) | 10–12 wks |
| 2 — iOS + Circles | iOS capture core + TestFlight, multiple circles + permissions, live view, heartbeat/check-in, messaging | 8–10 wks |
| 3 — Hardening | Offline robustness, storage tiering, push redundancy, verifier CLI, external security audit, reproducible builds | 6–8 wks |
| 4+ — Advanced | Conferencing (LiveKit), geofencing, zero-knowledge hard mode, voice activation, on-device transcription, org accounts, dashcam/home-security integrations | ongoing |

---

## 11. Open Questions & Decisions Needed

| ID | Question | Options | Recommendation |
|----|----------|---------|----------------|
| Q1 | Open source vs. proprietary? | Full OSS / Core OSS / Proprietary | **Decided ([ADR-014](./docs/adr/014-open-source-posture.md)): proprietary initially; revisit ~Phase 3.** Verifier CLI **and the evidence format specification** are FOSS regardless — independent verification that requires trusting us is not independent |
| Q2 | Self-hosting option? | Yes / No | Yes, for orgs with compliance needs |
| Q3 | Jurisdiction for incorporated entity? | US / EU / Other | **Publishing entity: NimbleJinn LLC (New Hampshire, forming).** Service-hosting jurisdiction still open (legal question) |
| Q4 | Recording consent notifications? | Always / Configurable / Jurisdiction-based | Jurisdiction-based with safe defaults |
| Q5 | Insurance/liability coverage? | Required / Optional | Required for peace of mind |
| Q6 | Name - "Personal Recorder"? | Keep / Alternatives | **Decided: kept; namespaces `PersonalRecorder.*`.** Rebranding later needs no codebase change, so the name does not gate progress. Candidates preserved in [design/NAMING.md](./design/NAMING.md) |

---

## 12. Appendix A: Glossary

| Term | Definition |
|------|------------|
| **Rolling Window** | Continuous recording that automatically deletes oldest content to maintain a fixed duration |
| **Lookback Buffer** | Pre-recorded content captured before explicit "start recording" to avoid missing incident beginnings |
| **Heartbeat** | Periodic signal confirming user safety; missed heartbeats trigger alerts |
| **Circle of Trust** | User-defined group with shared access permissions |
| **Side-loading** | Installing apps outside official app stores |
| **SFU** | Selective Forwarding Unit - server that routes WebRTC streams efficiently |
| **Zero-knowledge** | Encryption architecture where the service provider cannot access user data |

---

## 13. Appendix B: Reference Links

- [WebRTC Documentation](https://webrtc.org/)
- [RapidSOS Partner Program](https://rapidsos.com/partners/)
- [ACLU Know Your Rights](https://www.aclu.org/know-your-rights/stopped-by-police)
- [F-Droid Inclusion Policy](https://f-droid.org/docs/Inclusion_Policy/)

---

*This document is a living specification and will be updated as the project evolves.*
