namespace PersonalRecorder.Client.Capture;

/// <summary>
/// One encrypted, independently decryptable ~2-second CMAF segment as produced by the capture
/// core (ARCHITECTURE.md §6.2). This describes the segment; its bytes live in the
/// <see cref="ICaptureBuffer"/> or the upload queue.
/// </summary>
/// <param name="Recording">The recording the segment belongs to.</param>
/// <param name="Index">Zero-based position in the recording; also the chain and nonce index.</param>
/// <param name="Flags">Header flags.</param>
/// <param name="CaptureStart">Wall-clock time of the first frame.</param>
/// <param name="CaptureEnd">Wall-clock time after the last frame.</param>
/// <param name="SizeBytes">Size of the encrypted segment, header and tag included.</param>
public sealed record SegmentDescriptor(
    RecordingId Recording,
    uint Index,
    SegmentFlags Flags,
    DateTimeOffset CaptureStart,
    DateTimeOffset CaptureEnd,
    long SizeBytes);
