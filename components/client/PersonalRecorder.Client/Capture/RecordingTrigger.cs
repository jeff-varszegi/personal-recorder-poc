namespace PersonalRecorder.Client.Capture;

/// <summary>What moved the capture core into <see cref="CaptureState.Recording"/>.</summary>
public enum RecordingTrigger
{
    /// <summary>The user pressed record.</summary>
    Manual,

    /// <summary>The panic button (<c>EMR-001</c>). The sealed lookback uploads at top priority.</summary>
    Panic,

    /// <summary>A context rule such as a geofence, evaluated on-device (<c>LOC-003</c>, ADR-008).</summary>
    ContextRule,
}
