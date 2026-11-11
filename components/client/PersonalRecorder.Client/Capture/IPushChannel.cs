namespace PersonalRecorder.Client.Capture;

/// <summary>A registered push endpoint, published to the service so that alerts can be routed to this device.</summary>
/// <param name="Channel">The channel the endpoint belongs to.</param>
/// <param name="Endpoint">The channel-specific token or URL.</param>
public sealed record PushRegistration(AlertChannel Channel, string Endpoint);

/// <summary>Arguments of <see cref="IPushChannel.MessageReceived"/>.</summary>
public sealed class PushMessageReceivedEventArgs(ReadOnlyMemory<byte> sealedPayload) : EventArgs
{
    /// <summary>The alert payload, sealed to this device's keys. The channel never sees content (ADR-011).</summary>
    public ReadOnlyMemory<byte> SealedPayload { get; } = sealedPayload;
}

/// <summary>
/// The device side of one alert channel (ADR-011): registration, and delivery of the sealed
/// alert payloads the service routes. Implementations exist per platform and per channel; a
/// de-Googled device will typically offer <see cref="AlertChannel.UnifiedPush"/> and
/// <see cref="AlertChannel.SignalR"/> only.
/// </summary>
public interface IPushChannel
{
    /// <summary>Which channel this is.</summary>
    AlertChannel Channel { get; }

    /// <summary>Whether the channel can work on this device: a distributor is installed, Play services exist, and so on.</summary>
    bool IsAvailable { get; }

    /// <summary>Registers with the channel and returns the endpoint to publish to the service.</summary>
    Task<PushRegistration> RegisterAsync(CancellationToken cancellationToken);

    /// <summary>Raised with the sealed payload of each alert received over this channel.</summary>
    event EventHandler<PushMessageReceivedEventArgs> MessageReceived;
}
