namespace PersonalRecorder.Client.Capture;

/// <summary>
/// The disk-backed, encrypted lookback ring (ARCHITECTURE.md §6.2). Holds the most recent
/// <see cref="Capacity"/> of encrypted segments while the engine is <see cref="CaptureState.Hot"/>;
/// on record or panic the contents are sealed and become the start of the recording
/// (<c>REC-004</c>). Disk-backed because a five-minute 1080p lookback does not fit in RAM and
/// must survive process death.
/// </summary>
public interface ICaptureBuffer
{
    /// <summary>How much capture the ring retains (<c>REC-004</c>: 10 s to 5 min, default 30 s).</summary>
    TimeSpan Capacity { get; }

    /// <summary>How much capture the ring currently holds.</summary>
    TimeSpan Buffered { get; }

    /// <summary>Appends an encrypted segment, evicting the oldest segments beyond <see cref="Capacity"/>.</summary>
    ValueTask AppendAsync(SegmentDescriptor segment, ReadOnlyMemory<byte> encryptedSegment, CancellationToken cancellationToken);

    /// <summary>
    /// Freezes the current contents as the lookback of the armed recording and returns them
    /// oldest first, ready to be queued at panic priority. Must complete within the first
    /// 50 ms of the panic budget (ARCHITECTURE.md §6.3).
    /// </summary>
    ValueTask<IReadOnlyList<SegmentDescriptor>> SealAsync(CancellationToken cancellationToken);

    /// <summary>Discards the ring, for example on disarm.</summary>
    ValueTask ClearAsync(CancellationToken cancellationToken);
}
