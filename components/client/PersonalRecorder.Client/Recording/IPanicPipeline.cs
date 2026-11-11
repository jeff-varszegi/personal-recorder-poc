namespace PersonalRecorder.Client.Recording;

/// <summary>Options for one panic activation.</summary>
/// <param name="Silent">Suppress every local indication (<c>EMR-007</c>). Side-load builds only (ADR-013).</param>
public sealed record PanicOptions(bool Silent);

/// <summary>What the panic pipeline achieved, with the timings <c>PERF-002</c> is judged on.</summary>
/// <param name="Recording">The recording that was started or continued.</param>
/// <param name="TimeToRecording">Press to <see cref="Capture.CaptureState.Recording"/>.</param>
/// <param name="TimeToFirstAlertAcknowledged">Press to the first delivery receipt from any channel, if one arrived.</param>
/// <param name="TimeToFirstSegmentAcknowledged">Press to the service acknowledging the first uploaded segment, if it did.</param>
public sealed record PanicOutcome(
    RecordingId Recording,
    TimeSpan TimeToRecording,
    TimeSpan? TimeToFirstAlertAcknowledged,
    TimeSpan? TimeToFirstSegmentAcknowledged);

/// <summary>
/// The panic path (ARCHITECTURE.md §6.3, §11.1): seal the ring, start recording, alert the
/// circle over every channel, present the emergency-dial overlay, and keep recording
/// throughout. Everything that can be pre-done while <see cref="Capture.CaptureState.Hot"/>
/// already is, so the press-to-stream budget of two seconds is met without a round trip.
/// Never dials on the user's behalf (ARCHITECTURE.md §11.3).
/// </summary>
public interface IPanicPipeline
{
    /// <summary>Runs the panic sequence. Returns once recording is guaranteed active; alerts and uploads continue in the background.</summary>
    Task<PanicOutcome> TriggerAsync(PanicOptions options, CancellationToken cancellationToken);
}
