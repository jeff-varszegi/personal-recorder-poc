namespace PersonalRecorder;

/// <summary>
/// Common shape of the strongly-typed identifiers. Each wraps a 128-bit, time-ordered
/// value (UUID version 7) in its own struct, so a <see cref="RecordingId"/> can never be
/// handed to something that expects a <see cref="CircleId"/>. Generation is shared through
/// <see cref="Identifier"/>, so changing the underlying scheme is one edit.
/// </summary>
/// <typeparam name="TSelf">The implementing identifier type.</typeparam>
public interface IIdentifier<TSelf> : IEquatable<TSelf>
    where TSelf : struct, IIdentifier<TSelf>
{
    /// <summary>The underlying value.</summary>
    Guid Value { get; }

    /// <summary>Creates a new, unique identifier.</summary>
    static abstract TSelf New();

    /// <summary>Wraps an existing value, for example one read from storage or the wire.</summary>
    static abstract TSelf From(Guid value);
}

/// <summary>Generation shared by every identifier.</summary>
public static class Identifier
{
    /// <summary>
    /// A new UUID version 7: random, and sortable by creation time at millisecond resolution,
    /// which keeps index pages warm on append-heavy tables such as segments and location tracks.
    /// </summary>
    public static Guid NewValue() => Guid.CreateVersion7();
}

/// <summary>Identifies a user account.</summary>
public readonly record struct UserId(Guid Value) : IIdentifier<UserId>
{
    public static UserId New() => new(Identifier.NewValue());

    public static UserId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

/// <summary>Identifies one enrolled device of a user; the holder of a device signing key (ADR-009).</summary>
public readonly record struct DeviceId(Guid Value) : IIdentifier<DeviceId>
{
    public static DeviceId New() => new(Identifier.NewValue());

    public static DeviceId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

/// <summary>Identifies a circle of trust (<c>COT-001</c>).</summary>
public readonly record struct CircleId(Guid Value) : IIdentifier<CircleId>
{
    public static CircleId New() => new(Identifier.NewValue());

    public static CircleId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

/// <summary>Identifies a recording: one capture session's chain of segments (ARCHITECTURE.md §7).</summary>
public readonly record struct RecordingId(Guid Value) : IIdentifier<RecordingId>
{
    public static RecordingId New() => new(Identifier.NewValue());

    public static RecordingId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

/// <summary>Identifies a non-recording artifact: a snapshot, a note, or a location track.</summary>
public readonly record struct ArtifactId(Guid Value) : IIdentifier<ArtifactId>
{
    public static ArtifactId New() => new(Identifier.NewValue());

    public static ArtifactId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

/// <summary>Identifies an alert sent to a circle (<c>EMR-004</c>, <c>HB-003</c>).</summary>
public readonly record struct AlertId(Guid Value) : IIdentifier<AlertId>
{
    public static AlertId New() => new(Identifier.NewValue());

    public static AlertId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

/// <summary>Identifies a capability link issued to a non-user (ADR-010).</summary>
public readonly record struct ShareLinkId(Guid Value) : IIdentifier<ShareLinkId>
{
    public static ShareLinkId New() => new(Identifier.NewValue());

    public static ShareLinkId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

/// <summary>Identifies a heartbeat (check-in) session (<c>HB-001</c>).</summary>
public readonly record struct HeartbeatSessionId(Guid Value) : IIdentifier<HeartbeatSessionId>
{
    public static HeartbeatSessionId New() => new(Identifier.NewValue());

    public static HeartbeatSessionId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
