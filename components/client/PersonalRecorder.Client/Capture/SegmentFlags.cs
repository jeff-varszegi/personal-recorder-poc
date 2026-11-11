namespace PersonalRecorder.Client.Capture;

/// <summary>Per-segment flags carried in the plaintext, authenticated segment header (ARCHITECTURE.md §7.1).</summary>
[Flags]
public enum SegmentFlags
{
    /// <summary>No flags.</summary>
    None = 0,

    /// <summary>The segment starts with a keyframe and is independently decodable.</summary>
    KeyFrame = 1 << 0,

    /// <summary>Captured while a panic was active.</summary>
    Panic = 1 << 1,

    /// <summary>Captured before the user pressed record and recovered from the lookback ring.</summary>
    FromLookback = 1 << 2,
}
