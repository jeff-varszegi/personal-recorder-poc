namespace PersonalRecorder;

/// <summary>
/// The channels an alert can travel over (ADR-011). Delivery is attempted on every channel
/// available for a recipient and tracked per channel, so that no single platform owner can
/// silently sever the alert path.
/// </summary>
public enum AlertChannel
{
    /// <summary>Firebase Cloud Messaging: standard Android.</summary>
    Fcm,

    /// <summary>Apple Push Notification service: iOS. No alternative exists for background wake.</summary>
    Apns,

    /// <summary>UnifiedPush through a self-hostable distributor: first-class for de-Googled devices.</summary>
    UnifiedPush,

    /// <summary>Direct delivery over a live SignalR connection: lowest latency, no third party.</summary>
    SignalR,

    /// <summary>SMS through a pluggable provider: an optional last resort for panic alerts, carrying no payload.</summary>
    Sms,
}
