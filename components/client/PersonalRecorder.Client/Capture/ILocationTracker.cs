namespace PersonalRecorder.Client.Capture;

/// <summary>Sampling profiles, trading accuracy for battery (TECHNICAL_SPECIFICATION.md §5.2.3).</summary>
public enum LocationProfile
{
    /// <summary>GPS, about once a second: an active recording (<c>LOC-001</c>).</summary>
    ActiveRecording,

    /// <summary>Balanced, about every 30 seconds: armed but not recording.</summary>
    Standby,

    /// <summary>Network only, about every 5 minutes: heartbeat sessions in the background.</summary>
    BackgroundHeartbeat,
}

/// <summary>One location fix. Encrypted on-device before it leaves the client (ADR-008).</summary>
/// <param name="Timestamp">When the fix was taken.</param>
/// <param name="Latitude">Degrees, WGS 84.</param>
/// <param name="Longitude">Degrees, WGS 84.</param>
/// <param name="HorizontalAccuracyMeters">Radius of 68 % confidence, when the platform reports it.</param>
/// <param name="AltitudeMeters">Altitude above the WGS 84 ellipsoid, when available.</param>
/// <param name="SpeedMetersPerSecond">Ground speed, when available.</param>
/// <param name="HeadingDegrees">Direction of travel, clockwise from true north, when available.</param>
public sealed record LocationSample(
    DateTimeOffset Timestamp,
    double Latitude,
    double Longitude,
    double? HorizontalAccuracyMeters,
    double? AltitudeMeters,
    double? SpeedMetersPerSecond,
    double? HeadingDegrees);

/// <summary>Arguments of <see cref="ILocationTracker.Sampled"/>.</summary>
public sealed class LocationSampledEventArgs(LocationSample sample) : EventArgs
{
    /// <summary>The fix.</summary>
    public LocationSample Sample { get; } = sample;
}

/// <summary>
/// Background-capable location tracking (<c>LOC-*</c>), backed by the fused location provider
/// on Android and CoreLocation on iOS.
/// </summary>
public interface ILocationTracker
{
    /// <summary>Raised for each fix.</summary>
    event EventHandler<LocationSampledEventArgs> Sampled;

    /// <summary>Starts, or switches to, the given profile.</summary>
    Task StartAsync(LocationProfile profile, CancellationToken cancellationToken);

    /// <summary>Stops sampling.</summary>
    Task StopAsync(CancellationToken cancellationToken);
}
