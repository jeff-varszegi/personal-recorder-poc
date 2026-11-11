using System.Security.Cryptography;

namespace PersonalRecorder.Evidence;

/// <summary>
/// The per-recording hash chain (ARCHITECTURE.md §7.2):
/// <code>chain_hash[n] = SHA-256( chain_hash[n-1] ‖ SHA-256( header ‖ ciphertext ‖ tag ) )</code>
/// <para>
/// It is computed over the <em>encrypted</em> segment, so the service can extend and
/// countersign the chain from plaintext headers with no ability to decrypt, and a verifier
/// can recompute it from an export bundle to show that no segment was removed, reordered
/// or altered.
/// </para>
/// </summary>
public static class HashChain
{
    /// <summary>Size in bytes of every hash in the chain (SHA-256).</summary>
    public const int HashSize = 32;

    private static readonly byte[] GenesisValue = new byte[HashSize];

    /// <summary>
    /// The value of <c>chain_hash[-1]</c>: <see cref="HashSize"/> zero bytes. Every recording's
    /// first segment chains from this constant, so a chain is verifiable from its start with no
    /// out-of-band state.
    /// </summary>
    public static ReadOnlySpan<byte> Genesis => GenesisValue;

    /// <summary>Hashes one encrypted segment: <c>SHA-256(header ‖ ciphertext ‖ tag)</c>.</summary>
    /// <param name="header">The plaintext, authenticated segment header.</param>
    /// <param name="ciphertext">The AEAD ciphertext of the CMAF segment.</param>
    /// <param name="tag">The AEAD authentication tag.</param>
    /// <param name="destination">Receives the <see cref="HashSize"/>-byte segment hash.</param>
    public static void HashSegment(
        ReadOnlySpan<byte> header,
        ReadOnlySpan<byte> ciphertext,
        ReadOnlySpan<byte> tag,
        Span<byte> destination)
    {
        RequireHashSized(destination, nameof(destination));

        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(header);
        hash.AppendData(ciphertext);
        hash.AppendData(tag);
        hash.GetHashAndReset(destination);
    }

    /// <summary>Extends the chain by one segment: <c>SHA-256(previousChainHash ‖ segmentHash)</c>.</summary>
    /// <param name="previousChainHash">The chain head before this segment, or <see cref="Genesis"/> for the first.</param>
    /// <param name="segmentHash">The segment hash from <see cref="HashSegment"/>.</param>
    /// <param name="destination">Receives the new <see cref="HashSize"/>-byte chain head.</param>
    public static void Extend(ReadOnlySpan<byte> previousChainHash, ReadOnlySpan<byte> segmentHash, Span<byte> destination)
    {
        RequireHashSized(previousChainHash, nameof(previousChainHash));
        RequireHashSized(segmentHash, nameof(segmentHash));
        RequireHashSized(destination, nameof(destination));

        Span<byte> input = stackalloc byte[HashSize * 2];
        previousChainHash.CopyTo(input);
        segmentHash.CopyTo(input[HashSize..]);
        SHA256.HashData(input, destination);
    }

    /// <summary>Hashes the segment and extends the chain in one step, returning the new chain head.</summary>
    public static byte[] Next(
        ReadOnlySpan<byte> previousChainHash,
        ReadOnlySpan<byte> header,
        ReadOnlySpan<byte> ciphertext,
        ReadOnlySpan<byte> tag)
    {
        Span<byte> segmentHash = stackalloc byte[HashSize];
        HashSegment(header, ciphertext, tag, segmentHash);

        byte[] next = new byte[HashSize];
        Extend(previousChainHash, segmentHash, next);
        return next;
    }

    private static void RequireHashSized(ReadOnlySpan<byte> value, string paramName)
    {
        if (value.Length != HashSize)
        {
            throw new ArgumentException($"Expected {HashSize} bytes, got {value.Length}.", paramName);
        }
    }
}
