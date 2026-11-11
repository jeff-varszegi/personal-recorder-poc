namespace PersonalRecorder.Client.Capture;

/// <summary>
/// The capture core as the rest of the client sees it (ADR-002). Implementations own the
/// camera, microphone, hardware encoder and encrypted ring; callers only drive state
/// transitions and observe segments. Nothing above this interface may touch a camera API,
/// which is what keeps the UI shell replaceable (ARCHITECTURE.md §4.3).
/// </summary>
public interface IRecordingEngine
{
    /// <summary>The current state (ARCHITECTURE.md §6.1).</summary>
    CaptureState State { get; }

    /// <summary>
    /// The recording being produced while <see cref="State"/> is <see cref="CaptureState.Recording"/>;
    /// otherwise <see langword="null"/>.
    /// </summary>
    RecordingId? ActiveRecording { get; }

    /// <summary>Raised after every transition, including those the engine makes on its own (errors, timeouts).</summary>
    event EventHandler<CaptureStateChangedEventArgs> StateChanged;

    /// <summary>
    /// Raised when a segment has been encrypted and written to the ring or the queue. The
    /// watchdog (<c>REL-002</c>) asserts that these arrive at the expected cadence.
    /// </summary>
    event EventHandler<SegmentProducedEventArgs> SegmentProduced;

    /// <summary><see cref="CaptureState.Cold"/> → <see cref="CaptureState.Warm"/>: open the camera with a low-rate preview and no encoder.</summary>
    Task WarmUpAsync(CancellationToken cancellationToken);

    /// <summary>
    /// <see cref="CaptureState.Warm"/> → <see cref="CaptureState.Hot"/>: start the encoder into the
    /// lookback ring. Also pre-generates the recording identity and key material and pre-creates
    /// the upload session, so that a later panic never waits on a round trip (ARCHITECTURE.md §6.3).
    /// </summary>
    Task ArmAsync(CancellationToken cancellationToken);

    /// <summary><see cref="CaptureState.Hot"/> → <see cref="CaptureState.Warm"/>: stop the encoder and discard the ring.</summary>
    Task DisarmAsync(CancellationToken cancellationToken);

    /// <summary>
    /// <see cref="CaptureState.Warm"/> or <see cref="CaptureState.Hot"/> → <see cref="CaptureState.Recording"/>.
    /// From <see cref="CaptureState.Hot"/> the ring is sealed first and becomes the lookback
    /// (<c>REC-004</c>, <c>PERF-003</c>). <c>PERF-001</c> (under 500 ms) is measured from
    /// <see cref="CaptureState.Warm"/> to the first segment of the returned recording.
    /// </summary>
    Task<RecordingId> StartRecordingAsync(RecordingTrigger trigger, CancellationToken cancellationToken);

    /// <summary><see cref="CaptureState.Recording"/> → <see cref="CaptureState.Hot"/>: finalize the recording; the ring keeps running.</summary>
    Task StopRecordingAsync(CancellationToken cancellationToken);

    /// <summary>Any state → <see cref="CaptureState.Cold"/>: release the camera. An active recording is finalized first.</summary>
    Task ShutdownAsync(CancellationToken cancellationToken);
}

/// <summary>Arguments of <see cref="IRecordingEngine.StateChanged"/>.</summary>
public sealed class CaptureStateChangedEventArgs(CaptureState previous, CaptureState current) : EventArgs
{
    /// <summary>The state before the transition.</summary>
    public CaptureState Previous { get; } = previous;

    /// <summary>The state after the transition.</summary>
    public CaptureState Current { get; } = current;
}

/// <summary>Arguments of <see cref="IRecordingEngine.SegmentProduced"/>.</summary>
public sealed class SegmentProducedEventArgs(SegmentDescriptor segment) : EventArgs
{
    /// <summary>The segment that was produced.</summary>
    public SegmentDescriptor Segment { get; } = segment;
}
