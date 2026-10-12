using System.IO;
using System.Media;
using System.Text;

namespace VpnKillSwitch;

internal static class AudioAlerts
{
    private static byte[]? _blockedWav;
    private static byte[]? _restoredWav;
    private static readonly object _gate = new();

    public static void PlayBlocked()
    {
        Task.Run(() =>
        {
            try
            {
                lock (_gate)
                {
                    _blockedWav ??= GenerateWave(
                        sampleRate: 44100,
                        (freq: 580.0, durationSec: 0.13, gain: 0.32),
                        (freq: 0.0,   durationSec: 0.03, gain: 0.00),
                        (freq: 360.0, durationSec: 0.24, gain: 0.36)
                    );
                }

                using var ms = new MemoryStream(_blockedWav);
                using var player = new SoundPlayer(ms);
                player.PlaySync();
            }
            catch
            {
                try
                {
                    Console.Beep(600, 120);
                    Thread.Sleep(30);
                    Console.Beep(350, 220);
                }
                catch
                {
                }
            }
        });
    }

    public static void PlayRestored()
    {
        Task.Run(() =>
        {
            try
            {
                lock (_gate)
                {
                    _restoredWav ??= GenerateWave(
                        sampleRate: 44100,
                        (freq: 523.25, durationSec: 0.11, gain: 0.28),
                        (freq: 0.0,    durationSec: 0.02, gain: 0.00),
                        (freq: 783.99, durationSec: 0.20, gain: 0.32)
                    );
                }

                using var ms = new MemoryStream(_restoredWav);
                using var player = new SoundPlayer(ms);
                player.PlaySync();
            }
            catch
            {
                try
                {
                    Console.Beep(523, 100);
                    Thread.Sleep(20);
                    Console.Beep(784, 160);
                }
                catch
                {
                }
            }
        });
    }

    private static byte[] GenerateWave(int sampleRate, params (double freq, double durationSec, double gain)[] segments)
    {
        int totalSamples = 0;
        foreach (var seg in segments)
            totalSamples += (int)(sampleRate * seg.durationSec);

        int dataSize = totalSamples * sizeof(short);

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(dataSize + 36);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));

        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(sampleRate);
        writer.Write(sampleRate * sizeof(short));
        writer.Write((short)sizeof(short));
        writer.Write((short)16);

        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(dataSize);

        foreach (var seg in segments)
        {
            int count = (int)(sampleRate * seg.durationSec);
            if (seg.freq <= 0.0)
            {
                for (int i = 0; i < count; i++)
                    writer.Write((short)0);
                continue;
            }

            for (int i = 0; i < count; i++)
            {
                double t = (double)i / sampleRate;
                double env = Math.Sin(Math.PI * i / count);
                double sine = Math.Sin(2.0 * Math.PI * seg.freq * t);
                short sample = (short)(sine * env * seg.gain * short.MaxValue);
                writer.Write(sample);
            }
        }

        writer.Flush();
        return ms.ToArray();
    }
}
