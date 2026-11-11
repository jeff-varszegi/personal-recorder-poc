namespace PersonalRecorder.Client.Sync;

/// <summary>Upload lanes, drained strictly in this order (ARCHITECTURE.md §6.4). Lower is more urgent.</summary>
public enum UploadPriority
{
    /// <summary>Segments of a panic-triggered recording, the sealed lookback included.</summary>
    Panic = 0,

    /// <summary>Live segments of an active recording.</summary>
    Live = 1,

    /// <summary>Snapshots, notes, and tag or metadata mutations.</summary>
    Metadata = 2,

    /// <summary>Earlier local-only recordings being backfilled (<c>REC-007</c>).</summary>
    Backfill = 3,
}
