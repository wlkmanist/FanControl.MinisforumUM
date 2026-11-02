namespace FanControl.MinisforumUMSeries;

internal sealed class PawnIoF7bslReadOnlyBackend : IDisposable
{
    private readonly PawnIoTransport transport;

    private static readonly ushort[] TelemetryAddresses =
    [
        0x181E, 0x181F, 0x181E,
        0x1820, 0x1821, 0x1820,
        0x0309, 0x0304, 0x0305,
        0x1803, 0x1804,
    ];

    internal PawnIoF7bslReadOnlyBackend()
    {
        HostIdentitySnapshot host = HostIdentity.Read();

        if (!string.Equals(host.Board, "F7BSL", StringComparison.Ordinal) ||
            !string.Equals(host.BoardVersion, "1.0", StringComparison.Ordinal) ||
            host.EcMajor != 0 ||
            host.EcMinor != 4)
        {
            throw new PlatformNotSupportedException(
                $"Expected F7BSL 1.0 / EC 0.4, found: {host}");
        }

        F7PlatformProfile identityProfile = F7ProfileCatalog.Get("f7bsi");
        transport = new PawnIoTransport(identityProfile);
    }

    internal F7bslTelemetry ReadTelemetry()
    {
        byte[] v = transport.Read(TelemetryAddresses);

        return new F7bslTelemetry(
            DecodeRpm(v.AsSpan(0, 3), "CPU"),
            DecodeRpm(v.AsSpan(3, 3), "system"),
            v[6],
            v[7],
            v[8],
            v[9],
            v[10]);
    }

    public void Dispose() => transport.Dispose();

    private static int DecodeRpm(
        ReadOnlySpan<byte> sample,
        string name)
    {
        if (sample.Length != 3 || sample[0] != sample[2])
        {
            throw new IOException(
                $"unstable tacho {name}.");
        }

        ushort counter = (ushort)(sample[0] | (sample[1] << 8));
        return counter == 0 ? 0 : 2156250 / counter;
    }
}

internal sealed record F7bslTelemetry(
    int CpuFanRpm,
    int SystemFanRpm,
    int Temperature0309,
    int Temperature0304,
    int Temperature0305,
    int CpuControlCode,
    int SystemControlCode);
