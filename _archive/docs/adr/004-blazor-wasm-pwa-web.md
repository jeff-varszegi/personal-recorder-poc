# ADR-004: Blazor WebAssembly PWA for Web Surfaces

## Status
Accepted (2026-07-31)

## Context
The system needs cross-browser web surfaces: an artifact dashboard, circle management, an emergency console that must work for a non-technical circle member on any device with zero install, E2EE playback, and — because platform owners control native install channels — an install-free fallback recorder that cannot be de-platformed. The stack is all-C# (ADR-001..003).

## Decision
A single **Blazor WebAssembly** application, delivered as an installable PWA, consuming `PersonalRecorder.Contracts` directly:

- Dashboard: artifact browser with tag filtering, retention/pin review, exports
- Emergency console: alert link → live location + feed viewing in any modern browser
- E2EE playback: encrypted CMAF segments fetched over HTTPS, decrypted client-side (WebCrypto/libsodium.js), played via Media Source Extensions (iOS 17.1+ Managed Media Source as the Safari floor)
- Fallback recorder: `getUserMedia` + MediaRecorder with chunked encrypted upload — foreground-only, clearly labeled degraded mode
- JS interop is limited to thin shims (MediaRecorder/MSE glue); application logic stays in C#

## Consequences
- (+) C# end-to-end; contracts shared without code generation; PWA is the unblockable distribution floor.
- (+) One web app serves dashboard, console, and fallback duty.
- (−) WASM payload size and cold-load must be watched (AOT selectively; lazy-load media modules).
- (−) Browser recorder can never match native capture (no pre-warm buffer, background, or reliability guarantees) — messaging must be honest about this.

## Alternatives Considered
- **Blazor Server**: lower payload but requires a persistent circuit — wrong for an emergency console on flaky networks.
- **TypeScript SPA (Angular/React)**: splits the stack and reintroduces contract generation; no team appetite for React.
- **Uno WebAssembly**: viable, but couples web delivery to the mobile shell decision; Blazor stands alone regardless of ADR-003's fallback ladder.
