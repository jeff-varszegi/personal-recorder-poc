namespace PersonalRecorder.Client.Capture;

/// <summary>
/// Power and readiness states of the capture core (ARCHITECTURE.md §6.1). The headline
/// latency target, <c>PERF-001</c>, is measured from <see cref="Warm"/> to
/// <see cref="Recording"/>; <see cref="Hot"/> is what makes lookback possible.
/// </summary>
public enum CaptureState
{
    /// <summary>Camera closed, nothing running. The default; also after user disable or timeout.</summary>
    Cold,

    /// <summary>Camera bound with a low-rate preview and no encoder. Entered when the app is foregrounded.</summary>
    Warm,

    /// <summary>Encoder running into the encrypted lookback ring. Entered when the user arms ("Ready") or a context rule fires.</summary>
    Hot,

    /// <summary>Ring sealed and queued; segments upload continuously. Entered by panic or manual record.</summary>
    Recording,
}
