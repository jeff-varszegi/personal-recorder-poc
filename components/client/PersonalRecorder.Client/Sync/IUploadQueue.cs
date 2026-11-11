namespace PersonalRecorder.Client.Sync;

/// <summary>The kinds of artifact the queue carries. Every one of them is encrypted before it is enqueued (ADR-008).</summary>
public enum UploadKind
{
    /// <summary>An encrypted CMAF segment of a recording.</summary>
    Segment,

    /// <summary>A snapshot (<c>REC-011</c>).</summary>
    Snapshot,

    /// <summary>A note (<c>NOT-*</c>).</summary>
    Note,

    /// <summary>A tag assignment or other metadata change (<c>TAG-*</c>).</summary>
    TagMutation,

    /// <summary>A batch of location samples (<c>LOC-*</c>).</summary>
    LocationTrack,
}

/// <summary>One item in the queue. The payload at <paramref name="LocalPath"/> is already encrypted; the queue never sees plaintext.</summary>
/// <param name="Priority">The lane.</param>
/// <param name="Kind">What is being uploaded.</param>
/// <param name="LocalPath">Where the encrypted payload lives in the local store.</param>
/// <param name="SizeBytes">Payload size, for backpressure and progress.</param>
public sealed record UploadItem(UploadPriority Priority, UploadKind Kind, string LocalPath, long SizeBytes);

/// <summary>Arguments of <see cref="IUploadQueue.Completed"/>.</summary>
public sealed class UploadCompletedEventArgs(UploadItem item) : EventArgs
{
    /// <summary>The item the service acknowledged.</summary>
    public UploadItem Item { get; } = item;
}

/// <summary>
/// The one persistent, encrypted, prioritized upload queue (ARCHITECTURE.md §6.4). Everything
/// that leaves the device goes through it: segments, snapshots, notes, tags, location tracks.
/// Resumable (tus-style offsets), backpressure-aware, survives process death, resumes on
/// connectivity. Offline recording and later sync are this same path (<c>REC-006</c>,
/// <c>REC-007</c>), not a special case.
/// </summary>
public interface IUploadQueue
{
    /// <summary>Items not yet acknowledged by the service.</summary>
    int PendingCount { get; }

    /// <summary>Bytes not yet acknowledged by the service.</summary>
    long PendingBytes { get; }

    /// <summary>Raised when the service has acknowledged an item in full.</summary>
    event EventHandler<UploadCompletedEventArgs> Completed;

    /// <summary>Durably enqueues an item. Returns once the item is on disk, not once it is uploaded.</summary>
    ValueTask EnqueueAsync(UploadItem item, CancellationToken cancellationToken);
}
