using HocoKeepAlive.Audio;
using Xunit;

namespace HocoKeepAlive.Tests;

public class SilentWaveProviderTests
{
    [Fact]
    public void WaveFormat_MatchesExpectedSpecs()
    {
        var provider = new SilentWaveProvider(48000, 2);

        Assert.Equal(48000, provider.WaveFormat.SampleRate);
        Assert.Equal(16, provider.WaveFormat.BitsPerSample);
        Assert.Equal(2, provider.WaveFormat.Channels);
    }

    [Fact]
    public void Read_FillsBufferWithDigitalZeros()
    {
        var provider = new SilentWaveProvider(useAntiSleepDither: false);
        byte[] buffer = new byte[1024];

        // Seed with non-zero bytes to ensure provider clears them
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = (byte)(i % 255 + 1);
        }

        int bytesRead = provider.Read(buffer, 0, buffer.Length);

        Assert.Equal(buffer.Length, bytesRead);
        Assert.All(buffer, b => Assert.Equal(0, b));
    }

    [Fact]
    public void Read_RespectsOffsetAndCount()
    {
        var provider = new SilentWaveProvider(useAntiSleepDither: false);
        byte[] buffer = new byte[100];
        Array.Fill(buffer, (byte)0xFF);

        int offset = 20;
        int count = 60;

        int bytesRead = provider.Read(buffer, offset, count);

        Assert.Equal(count, bytesRead);

        // Bytes before offset should be untouched
        for (int i = 0; i < offset; i++)
        {
            Assert.Equal((byte)0xFF, buffer[i]);
        }

        // Bytes inside range should be cleared to 0
        for (int i = offset; i < offset + count; i++)
        {
            Assert.Equal(0, buffer[i]);
        }

        // Bytes after range should be untouched
        for (int i = offset + count; i < buffer.Length; i++)
        {
            Assert.Equal((byte)0xFF, buffer[i]);
        }
    }

    [Fact]
    public void Read_ZeroMode_ReturnsAllZeros()
    {
        var provider = new SilentWaveProvider(useAntiSleepDither: false);
        byte[] buffer = new byte[2000];
        provider.Read(buffer, 0, buffer.Length);
        Assert.All(buffer, b => Assert.Equal(0, b));
    }

    [Fact]
    public void Read_DitherMode_ReturnsValuesInRangeMinusOneToOneWithNonZeroSample()
    {
        var provider = new SilentWaveProvider(useAntiSleepDither: true);
        byte[] buffer = new byte[4000]; // 2000 16-bit samples
        int bytesRead = provider.Read(buffer, 0, buffer.Length);

        Assert.Equal(buffer.Length, bytesRead);

        bool foundNonZero = false;
        for (int i = 0; i < buffer.Length; i += 2)
        {
            short sample = BitConverter.ToInt16(buffer, i);
            Assert.True(sample is >= -1 and <= 1, $"Sample value {sample} is out of expected range -1..1");
            if (sample != 0)
            {
                foundNonZero = true;
            }
        }

        Assert.True(foundNonZero, "Dither mode should produce at least one non-zero 16-bit sample");
    }

    [Fact]
    public void Read_ThrowsOnInvalidParameters()
    {
        var provider = new SilentWaveProvider();
        byte[] buffer = new byte[50];

        Assert.Throws<ArgumentNullException>(() => provider.Read(null!, 0, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => provider.Read(buffer, -1, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => provider.Read(buffer, 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => provider.Read(buffer, 45, 10)); // exceeds buffer
    }
}
