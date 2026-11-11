# ADR-003: Native MAUI XAML for the MVP Shell, Not Blazor Hybrid

## Status
Accepted — amended by [ADR-015](./015-consolidated-layout-and-service-stack.md) in names only: `Client.Core`, `Contracts` and `Evidence` are now namespaces of `PersonalRecorder.Client` and `PersonalRecorder.Core`.

## Context
Because the whole system is C#, a tempting option exists: write the mobile UI as Razor components in a MAUI Blazor Hybrid host and share those components with the Blazor WebAssembly dashboard. One UI codebase for phone and browser.

The pull is real, but the two surfaces do different jobs. The phone app is a capture and panic instrument used under stress, often one-handed, sometimes with the screen dimmed to black. The web app is a browsing, review and monitoring surface. Their screens have almost nothing in common except the data behind them — and that data layer is already shared through `Client.Core`, `Contracts` and `Evidence`.

## Decision
- The MVP shell is **native MAUI XAML**. The panic path in particular runs on native controls, with no WebView between the user's thumb and the recorder.
- Component sharing with the web is **re-evaluated in Phase 2** for content screens only — the artifact browser, tag management, settings — where a WebView's cold-start cost is irrelevant and the screens genuinely resemble their web counterparts.
- Sharing happens through view-models, not markup: `Client.Core` is where phone and web already meet.

## Consequences
- (+) The critical path keeps native responsiveness, native accessibility trees, and native camera-preview embedding.
- (+) One UI paradigm in the app during the phase when the team is smallest and the unknowns are largest.
- (−) Content screens get written twice (XAML for phone, Razor for web). Accepted: they are the cheap screens, and Phase 2 may reclaim some of it.
- (−) If Phase 2 does adopt Blazor Hybrid for content screens, the app carries two UI technologies. That is a deliberate trade to be made with evidence, not now.

## Alternatives Considered
- **Blazor Hybrid for the whole shell**: maximum code sharing; pays WebView cold-start and an interop layer on the one interaction that must never hesitate, and weakens native accessibility integration for the senior and DV-survivor user populations.
- **Blazor Hybrid for content screens from day 1**: attractive, but it front-loads a second UI stack before we know what the content screens even look like.
