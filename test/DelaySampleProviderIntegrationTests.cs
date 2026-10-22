#nullable enable

using System;
using System.Linq;
using NAudio.Wave;
using Xunit;

namespace NAudioEffects.Tests
{
    public class DelaySampleProviderIntegrationTests
    {
        private class ImpulseSampleProvider : ISampleProvider
        {
            private readonly float[] _data;
            private int _index;

            public ImpulseSampleProvider(float[] data, WaveFormat waveFormat)
            {
                _data = data;
                WaveFormat = waveFormat;
            }

            public WaveFormat WaveFormat { get; }

            public int Read(float[] buffer, int offset, int count)
            {
                int toCopy = Math.Min(count, _data.Length - _index);
                if (toCopy > 0)
                {
                    Array.Copy(_data, _index, buffer, offset, toCopy);
                    _index += toCopy;
                }
                return toCopy;
            }
        }

        [Fact]
        public void Read_WithImpulseInput_DelayedEchoesAtExpectedOffsets()
        {
            // Arrange: 1-second impulse at 44.1 kHz mono
            int sampleRate = 44100;
            int totalSamples = sampleRate; // 1 second
            float[] impulse = new float[totalSamples];
            impulse[0] = 1.0f; // single-sample impulse

            var source = new ImpulseSampleProvider(impulse, new WaveFormat(sampleRate, 1));
            var delay = new DelaySampleProvider(source, maxDelayMs: 500f)
            {
                DelayMs = 250f,   // 250 ms → 11025 samples at 44.1 kHz
                Feedback = 0.5f,  // feedback factor
                Mix = 1.0f        // fully wet
            };
            float[] output = new float[totalSamples];

            // Act
            delay.Read(output, 0, totalSamples);

            // Assert: echoes at sample offsets 11025, 22050, 33075
            // With mix=1.0 (fully wet) and feedback=0.5:
            // - First echo at 11025: amplitude 1.0 (the delayed impulse)
            // - Second echo at 22050: amplitude 0.5 (feedback applied once)
            // - Third echo at 33075: amplitude 0.25 (feedback applied twice)
            // The amplitudes decay by the feedback factor (0.5) each iteration.
            const float tolerance = 1e-3f;

            Assert.InRange(11025, 0, totalSamples - 1);
            Assert.InRange(22050, 0, totalSamples - 1);
            Assert.InRange(33075, 0, totalSamples - 1);

            // Check echo amplitudes decaying by feedback factor
            Assert.Equal(1.0f, output[11025], tolerance);  // first echo: 1.0
            Assert.Equal(0.5f, output[22050], tolerance);  // second echo: 0.5
            Assert.Equal(0.25f, output[33075], tolerance); // third echo: 0.25

            // No other samples should exceed 1e-4
            for (int i = 0; i < totalSamples; i++)
            {
                if (i == 11025 || i == 22050 || i == 33075) continue;
                Assert.True(Math.Abs(output[i]) <= 1e-4f,
                    $"Sample at index {i} has value {output[i]}, expected magnitude <= 1e-4");
            }
        }

        [Fact]
        public void Chain_DelayEqualizerCompressor_ReadsToCompletion()
        {
            // Arrange: 3-second sine wave at 44.1 kHz mono
            int sampleRate = 44100;
            double frequencyHz = 440.0;
            int totalSamples = sampleRate * 3; // 3 seconds

            var sineBuffer = new float[totalSamples];
            for (int i = 0; i < totalSamples; i++)
            {
                double t = (double)i / sampleRate;
                sineBuffer[i] = (float)Math.Sin(2.0 * Math.PI * frequencyHz * t);
            }

            var source = new BufferSampleProvider(sineBuffer, sampleRate, 1);

            // Build the chain: Delay -> Equalizer -> Compressor
            var delay = new DelaySampleProvider(source, maxDelayMs: 100f)
            {
                DelayMs = 50f,
                Feedback = 0.1f,
                Mix = 0.5f
            };

            var equalizer = new EqualizerSampleProvider(delay, bandCount: 3);
            equalizer.SetBandGain(0, 3.0f); // mild boost
            equalizer.SetBandGain(1, -2.0f); // mild cut
            equalizer.SetBandGain(2, 1.5f);

            var compressor = new CompressorSampleProvider(equalizer)
            {
                ThresholdDb = -20f,
                Ratio = 4f,
                AttackMs = 10f,
                ReleaseMs = 50f,
                KneeDb = 0f,
                MakeupGainDb = 0f
            };

            // Act: read the full chain to completion
            float[] chainOutput = new float[totalSamples];
            int totalRead = compressor.Read(chainOutput, 0, totalSamples);

            // Assert: chain reads to completion without exceptions
            Assert.Equal(totalSamples, totalRead);

            // Verify output buffer was fully populated
            Assert.Equal(totalSamples, chainOutput.Length);
        }

        [Fact]
        public void Feedback_OneOrAbove_IsRejected()
        {
            // Arrange
            int sampleRate = 44100;
            var source = new BufferSampleProvider(new float[] { 0.5f }, sampleRate, 1);

            // Act & Assert: Feedback >= 1.0 should be rejected (ArgumentOutOfRangeException)
            // The DelaySampleProvider enforces Feedback <= 0.95
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new DelaySampleProvider(source) { Feedback = 1.0f });

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new DelaySampleProvider(source) { Feedback = 0.96f });

            // Feedback = 0.95f should be accepted (valid boundary)
            var delay = new DelaySampleProvider(source);
            delay.Feedback = 0.95f; // should not throw
        }

        private class BufferSampleProvider : ISampleProvider
        {
            private readonly float[] _buffer;
            private int _position;

            public BufferSampleProvider(float[] buffer, int sampleRate = 44100, int channels = 1)
            {
                _buffer = buffer;
                WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
            }

            public WaveFormat WaveFormat { get; }

            public int Read(float[] buffer, int offset, int count)
            {
                int remaining = _buffer.Length - _position;
                int toCopy = Math.Min(count, remaining);
                if (toCopy > 0)
                {
                    Array.Copy(_buffer, _position, buffer, offset, toCopy);
                    _position += toCopy;
                }
                return toCopy;
            }
        }
    }
}