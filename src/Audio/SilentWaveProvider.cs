using NAudio.Wave;

namespace HocoKeepAlive.Audio;

/// <summary>
/// Provides a continuous 48,000 Hz, 16-bit stereo stream of digital silence with optional
/// sub-audible 1-LSB dither (-90.3 dB) to keep Bluetooth speaker DSPs and amplifiers awake.
/// Extremely low CPU and memory overhead; zero dynamic allocations in the callback.
/// </summary>
public sealed class SilentWaveProvider : IWaveProvider
{
    private int _sampleCounter;
    public WaveFormat WaveFormat { get; }
    public bool UseAntiSleepDither { get; set; }

    public SilentWaveProvider(int sampleRate = 48000, int channels = 2, bool useAntiSleepDither = false)
    {
        WaveFormat = new WaveFormat(sampleRate, 16, channels);
        UseAntiSleepDither = useAntiSleepDither;
    }

    public int Read(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        if (offset < 0 || count < 0 || offset + count > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Invalid buffer offset or count specified.");
        }

        // Fill buffer slice with digital silence
        Array.Clear(buffer, offset, count);

        if (UseAntiSleepDither)
        {
            // Inject sub-audible 1-LSB dither roughly once every 480 samples (~100 Hz at 48kHz).
            // Inaudible to human ears, but signals audio activity to hardware DSPs that measure amplitude.
            int sampleCount = count / 2; // 16-bit samples (2 bytes per sample)
            for (int i = 0; i < sampleCount; i++)
            {
                _sampleCounter++;
                if (_sampleCounter % 480 == 0)
                {
                    short dither = (_sampleCounter / 480 % 2 == 0) ? (short)1 : (short)-1;
                    int byteOffset = offset + (i * 2);
                    if (byteOffset + 1 < offset + count)
                    {
                        buffer[byteOffset] = (byte)(dither & 0xFF);
                        buffer[byteOffset + 1] = (byte)((dither >> 8) & 0xFF);
                    }
                }
            }
        }

        return count;
    }
}
