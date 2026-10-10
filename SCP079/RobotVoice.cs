// ═══════════════════════════════════════════════════════════════
// SCP-079 — искусственный интеллект
// Автор: vl1doff
// GitHub: https://github.com/vl1doff/SCP079
// Copyright © vl1doff 2026. Все права защищены.
// Signature: SCP079-VL1DOFF-2026-UNIQUE
// ═══════════════════════════════════════════════════════════════
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.IO;

namespace SCP079
{
    public static class RobotVoice
    {
        public static byte[] Transform(byte[] inputWav)
        {
            using var inputStream = new MemoryStream(inputWav);
            using var reader = new WaveFileReader(inputStream);

            ISampleProvider samples = reader.ToSampleProvider();

            samples = new WdlResamplingSampleProvider(samples, 8000);
            samples = new RingModSampleProvider(samples, 60.0);
            samples = new DistortionSampleProvider(samples, 0.4f);
            samples = new BitcrushSampleProvider(samples, 0.08f);
            samples = new SoftLimiterSampleProvider(samples);

            using var outputStream = new MemoryStream();
            var waveWriter = new WaveFileWriter(outputStream, samples.WaveFormat);

            float[] buffer = new float[samples.WaveFormat.SampleRate * samples.WaveFormat.Channels];
            int read;

            while ((read = samples.Read(buffer)) > 0)
            {
                for (int i = 0; i < read; i++)
                    waveWriter.WriteSample(buffer[i]);
            }
            waveWriter.Flush();

            return outputStream.ToArray();
        }
    }

    public class RingModSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private readonly double _freq;
        private double _phase;

        public WaveFormat WaveFormat => _source.WaveFormat;

        public RingModSampleProvider(ISampleProvider source, double freq)
        {
            _source = source;
            _freq = freq;
        }

        public int Read(Span<float> buffer)
        {
            int read = _source.Read(buffer);
            double step = 2 * Math.PI * _freq / WaveFormat.SampleRate;

            for (int i = 0; i < read; i++)
            {
                buffer[i] *= (float)Math.Sin(_phase);
                _phase += step;
                if (_phase > 2 * Math.PI) _phase -= 2 * Math.PI;
            }
            return read;
        }
    }

    public class DistortionSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private readonly float _drive;

        public WaveFormat WaveFormat => _source.WaveFormat;

        public DistortionSampleProvider(ISampleProvider source, float drive)
        {
            _source = source;
            _drive = drive;
        }

        public int Read(Span<float> buffer)
        {
            int read = _source.Read(buffer);
            for (int i = 0; i < read; i++)
            {
                float x = buffer[i] * (1f + _drive * 5f);
                buffer[i] = (float)Math.Tanh(x);
            }
            return read;
        }
    }

    public class BitcrushSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private readonly float _step;

        public WaveFormat WaveFormat => _source.WaveFormat;

        public BitcrushSampleProvider(ISampleProvider source, float step)
        {
            _source = source;
            _step = step;
        }

        public int Read(Span<float> buffer)
        {
            int read = _source.Read(buffer);
            for (int i = 0; i < read; i++)
                buffer[i] = MathF.Round(buffer[i] / _step) * _step;
            return read;
        }
    }

    public class SoftLimiterSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;
        public WaveFormat WaveFormat => _source.WaveFormat;

        public SoftLimiterSampleProvider(ISampleProvider source) { _source = source; }

        public int Read(Span<float> buffer)
        {
            int read = _source.Read(buffer);
            for (int i = 0; i < read; i++)
                buffer[i] = Math.Clamp(buffer[i], -0.95f, 0.95f);
            return read;
        }
    }
}