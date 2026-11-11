using System.Security.Cryptography;
using PersonalRecorder.Evidence;

namespace PersonalRecorder.Tests.Core;

/// <summary>
/// Known-answer tests for the chain formula in docs/ARCHITECTURE.md §7.2. The expected values
/// were computed independently with Python's hashlib, not with the code under test.
/// </summary>
public sealed class HashChainTests
{
    // sha256(b"hdr-0" + b"ct-0" + b"tag-0")
    private static readonly byte[] Segment0Hash = Convert.FromHexString("20f9df2184acfa7b9a3f590e698d5679166047265f206b717cb4dfe008faf798");

    // sha256(bytes(32) + Segment0Hash)
    private static readonly byte[] Chain0 = Convert.FromHexString("2ecaf0ec5d0a0b4ed6c28b43d201d70c49e3cba9a38c08cee4223a1c9e250f45");

    // sha256(b"hdr-1" + b"ct-1" + b"tag-1")
    private static readonly byte[] Segment1Hash = Convert.FromHexString("345aff703b2525cdbc64bf617df9ef2f7d93bbe847fdf071a6e3b0212f7deb1b");

    // sha256(Chain0 + Segment1Hash)
    private static readonly byte[] Chain1 = Convert.FromHexString("0d98da3dd1edead5a69662bd509ea48ebe42da95e42dd902fb43425efe15bfcd");

    [Fact]
    public void Genesis_is_thirty_two_zero_bytes()
    {
        Assert.Equal(HashChain.HashSize, HashChain.Genesis.Length);
        Assert.All(HashChain.Genesis.ToArray(), b => Assert.Equal(0, b));
    }

    [Fact]
    public void Segment_hash_matches_the_independent_vector()
    {
        byte[] actual = new byte[HashChain.HashSize];
        HashChain.HashSegment("hdr-0"u8, "ct-0"u8, "tag-0"u8, actual);

        Assert.Equal(Segment0Hash, actual);
    }

    [Fact]
    public void Chain_matches_the_independent_vectors()
    {
        byte[] head0 = HashChain.Next(HashChain.Genesis, "hdr-0"u8, "ct-0"u8, "tag-0"u8);
        byte[] head1 = HashChain.Next(head0, "hdr-1"u8, "ct-1"u8, "tag-1"u8);

        Assert.Equal(Chain0, head0);
        Assert.Equal(Chain1, head1);
    }

    [Fact]
    public void Extend_alone_matches_the_independent_vector()
    {
        byte[] actual = new byte[HashChain.HashSize];
        HashChain.Extend(Chain0, Segment1Hash, actual);

        Assert.Equal(Chain1, actual);
    }

    [Fact]
    public void Segment_hash_is_over_the_concatenation_regardless_of_how_the_input_is_split()
    {
        byte[] whole = "hdr-0ct-0tag-0"u8.ToArray();
        byte[] expected = SHA256.HashData(whole);

        byte[] actual = new byte[HashChain.HashSize];
        HashChain.HashSegment("hdr-0"u8, "ct-0"u8, "tag-0"u8, actual);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Reordering_segments_changes_the_head()
    {
        byte[] reordered0 = HashChain.Next(HashChain.Genesis, "hdr-1"u8, "ct-1"u8, "tag-1"u8);
        byte[] reordered1 = HashChain.Next(reordered0, "hdr-0"u8, "ct-0"u8, "tag-0"u8);

        Assert.NotEqual(Chain1, reordered1);
    }

    [Fact]
    public void Altering_one_byte_of_ciphertext_changes_the_head()
    {
        byte[] head = HashChain.Next(HashChain.Genesis, "hdr-0"u8, "ct-1"u8, "tag-0"u8);

        Assert.NotEqual(Chain0, head);
    }

    [Fact]
    public void Wrongly_sized_hashes_are_rejected()
    {
        byte[] tooShort = new byte[HashChain.HashSize - 1];
        byte[] destination = new byte[HashChain.HashSize];

        Assert.Throws<ArgumentException>(() => HashChain.Extend(tooShort, Segment0Hash, destination));
        Assert.Throws<ArgumentException>(() => HashChain.Extend(Chain0, tooShort, destination));
        Assert.Throws<ArgumentException>(() => HashChain.Extend(Chain0, Segment0Hash, tooShort));
        Assert.Throws<ArgumentException>(() => HashChain.HashSegment("h"u8, "c"u8, "t"u8, tooShort));
    }
}
