namespace PersonalRecorder.Client.Capture;

/// <summary>
/// Hardware-backed key custody (<c>STR-008</c>): the Android Keystore (StrongBox where
/// present) or the iOS Keychain and Secure Enclave. Keys named here never leave the hardware;
/// the store wraps and unwraps the software keys that encrypt the ring, the queue and the
/// local database (ADR-009).
/// </summary>
public interface ISecureStore
{
    /// <summary>
    /// Whether the wrapping key lives in dedicated hardware rather than software emulation.
    /// Surfaced to the user; never silently degraded.
    /// </summary>
    bool IsHardwareBacked { get; }

    /// <summary>Encrypts <paramref name="key"/> under the hardware-resident key called <paramref name="keyName"/>, creating that key on first use.</summary>
    ValueTask<byte[]> WrapAsync(string keyName, ReadOnlyMemory<byte> key, CancellationToken cancellationToken);

    /// <summary>Reverses <see cref="WrapAsync"/>.</summary>
    ValueTask<byte[]> UnwrapAsync(string keyName, ReadOnlyMemory<byte> wrappedKey, CancellationToken cancellationToken);
}
