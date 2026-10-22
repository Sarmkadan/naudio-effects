#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using Xunit;

namespace NAudioEffects.Tests
{
    public class ChorusSampleProviderTests
    {
        private class SineWaveProvider : ISampleProvider
        {
            private readonly float _frequency;
            private readonly float _sampleRate;
            private readonly int _channels;
            private float _phase;

            public SineWaveProvider(float frequency, float sampleRate, int channels = 2)
            {
                _frequency = frequency;
                _sampleRate = sampleRate;
                _channels = channels;
                _phase = 0f;
            }

            public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);

            public int Read(float[] buffer, int offset, int count)
            {
                for (int i = 0; i < count; i++)
                {
                    // Generate stereo sine: left and right channels are the same
                    var sample = (float)Math.Sin(_phase);
                    buffer[offset + i] = sample;

                    // Advance phase
                    _phase += 2.0f * (float)Math.PI * _frequency / _sampleRate;
                    if (_phase >= (float)(2.0 * Math.PI))
                    {
                        _phase -= (float)(2.0 * Math.PI);
                    }
                }
                return count;
            }
        }

        [Fact]
        public void Read_TotalSamplesEqualsSourceLength()
        {
            // Arrange
            int sampleRate = 44100;
            int durationSeconds = 2;
            int totalSamples = sampleRate * durationSeconds * 2; // stereo = 2 channels

            var source = new SineWaveProvider(440f, sampleRate, 2);
            var chorus = new ChorusSampleProvider(source)
            {
                RateHz = 1.5f,
                DepthMs = 5f,
                Mix = 0.5f
            };

            float[] buffer = new float[totalSamples];
            int samplesRead = chorus.Read(buffer, 0, totalSamples);

            // Assert
            Assert.Equal(totalSamples, samplesRead);
        }

        [Fact]
        public void Read_OutputNotIdenticalToInput_ChorusModifiesSignal()
        {
            // Arrange
            int sampleRate = 44100;
            int durationSeconds = 2;
            int totalSamples = sampleRate * durationSeconds * 2; // stereo = 2 channels

            var source = new SineWaveProvider(440f, sampleRate, 2);
            var chorus = new ChorusSampleProvider(source)
            {
                RateHz = 1.5f,
                DepthMs = 5f,
                Mix = 0.5f
            };

            float[] input = new float[totalSamples];
            int samplesRead = source.Read(input, 0, totalSamples);

            // Need a fresh source for the chorus since the first one was consumed
            var source2 = new SineWaveProvider(440f, sampleRate, 2);
            var chorus2 = new ChorusSampleProvider(source2)
            {
                RateHz = 1.5f,
                DepthMs = 5f,
                Mix = 0.5f
            };

            float[] output = new float[totalSamples];
            chorus2.Read(output, 0, totalSamples);

            // Assert: output should not be bit-identical to input (chorus effect modifies signal)
            bool identical = true;
            for (int i = 0; i < totalSamples; i++)
            {
                if (Math.Abs(input[i] - output[i]) > 0.001f)
                {
                    identical = false;
                    break;
                }
            }
            Assert.False(identical, "Chorus output should differ from input when Mix > 0 and RateHz > 0");
        }

        [Fact]
        public void Read_PeakAmplitudeNeverExceedsOne()
        {
            // Arrange
            int sampleRate = 44100;
            int durationSeconds = 2;
            int totalSamples = sampleRate * durationSeconds * 2; // stereo = 2 channels

            var source = new SineWaveProvider(440f, sampleRate, 2);
            var chorus = new ChorusSampleProvider(source)
            {
                RateHz = 1.5f,
                DepthMs = 5f,
                Mix = 0.5f
            };

            float[] buffer = new float[totalSamples];
            int samplesRead = chorus.Read(buffer, 0, totalSamples);

            // Assert: peak amplitude never exceeds 1.0
            for (int i = 0; i < samplesRead; i++)
            {
                Assert.True(buffer[i] <= 1.0f, $"Sample at index {i} exceeds 1.0: {buffer[i]}");
                Assert.True(buffer[i] >= -1.0f, $"Sample at index {i} below -1.0: {buffer[i]}");
            }
        }

        [Fact]
        public void Read_MixZeroYieldsBitIdenticalOutput()
        {
            // Arrange
            int sampleRate = 44100;
            int durationSeconds = 1; // 1 second for speed
            int totalSamples = sampleRate * durationSeconds * 2; // stereo = 2 channels

            // First read: get the raw input
            var source1 = new SineWaveProvider(440f, sampleRate, 2);
            float[] input = new float[totalSamples];
            int samplesReadFromSource = source1.Read(input, 0, totalSamples);

            // Second read: pass through chorus with Mix=0
            var source2 = new SineWaveProvider(440f, sampleRate, 2);
            var chorus = new ChorusSampleProvider(source2)
            {
                RateHz = 1.5f,
                DepthMs = 5f,
                Mix = 0f // dry only
            };

            float[] output = new float[totalSamples];
            chorus.Read(output, 0, totalSamples);

            // Assert: with mix=0, output should be bit-identical to input
            for (int i = 0; i < samplesReadFromSource; i++)
            {
                Assert.Equal(input[i], output[i], 7); // 7 decimal places for float equality
            }
        }

        [Fact]
        public void ChunkedReadsAndFullReadProduceIdenticalOutput()
        {
            // Arrange
            int sampleRate = 44100;
            int durationSeconds = 1;
            int totalSamples = sampleRate * durationSeconds * 2; // stereo = 2 channels

            // Full read
            var source1 = new SineWaveProvider(440f, sampleRate, 2);
            var chorus1 = new ChorusSampleProvider(source1)
            {
                RateHz = 1.5f,
                DepthMs = 5f,
                Mix = 0.3f
            };

            float[] fullBuffer = new float[totalSamples];
            chorus1.Read(fullBuffer, 0, totalSamples);

            // Chunked reads (1024-sample chunks) - need fresh source and chorus
            var source2 = new SineWaveProvider(440f, sampleRate, 2);
            var chorus2 = new ChorusSampleProvider(source2)
            {
                RateHz = 1.5f,
                DepthMs = 5f,
                Mix = 0.3f
            };

            float[] chunkedBuffer = new float[totalSamples];
            int offset = 0;
            int remaining = totalSamples;
            while (remaining > 0)
            {
                int chunkSize = Math.Min(1024, remaining);
                int samplesRead = chorus2.Read(chunkedBuffer, offset, chunkSize);
                offset += samplesRead;
                remaining -= samplesRead;
            }

            // Assert: chunked reads produce identical output to full read
            for (int i = 0; i < totalSamples; i++)
            {
                Assert.Equal(fullBuffer[i], chunkedBuffer[i], 7); // 7 decimal places for float equality
            }
        }

        [Fact]
        public void Constructor_WithNullOptions_ThrowsArgumentNullException()
        {
            var source = new SineWaveProvider(440f, 44100f);

            Assert.Throws<ArgumentNullException>(() => new ChorusSampleProvider(source, null!));
        }

        [Fact]
        public void Constructor_WithOptions_CopiesValues()
        {
            var source = new SineWaveProvider(440f, 44100f);
            var options = new ChorusOptions { RateHz = 2f, DepthMs = 10f, Mix = 0.5f };

            var chorus = new ChorusSampleProvider(source, options);

            Assert.Equal(2f, chorus.RateHz);
            Assert.Equal(10f, chorus.DepthMs);
            Assert.Equal(0.5f, chorus.Mix);

            // Later changes to the options object must not leak into the provider.
            options.RateHz = 4f;
            Assert.Equal(2f, chorus.RateHz);
        }

        [Fact]
        public void Constructor_WithInvalidOptions_ThrowsArgumentOutOfRange()
        {
            var source = new SineWaveProvider(440f, 44100f);
            var options = new ChorusOptions { DepthMs = 0f };

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new ChorusSampleProvider(source, options));
            Assert.Equal(nameof(ChorusOptions.DepthMs), ex.ParamName);
        }

        [Fact]
        public void Constructor_WithDefaultOptions_MatchesParameterlessConstructor()
        {
            var source = new SineWaveProvider(440f, 44100f);

            var chorus = new ChorusSampleProvider(source, ChorusOptions.Default);

            var reference = new ChorusSampleProvider(source);
            Assert.Equal(reference.RateHz, chorus.RateHz);
            Assert.Equal(reference.DepthMs, chorus.DepthMs);
            Assert.Equal(reference.Mix, chorus.Mix);
        }

        [Theory]
        [InlineData(float.Epsilon)]
        [InlineData(20f)]
        public void RateHz_WithinRange_IsAccepted(float rate)
        {
            var chorus = new ChorusSampleProvider(new SineWaveProvider(440f, 44100f));

            chorus.RateHz = rate;

            Assert.Equal(rate, chorus.RateHz);
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(-float.Epsilon)]
        [InlineData(20.0001f)]
        public void RateHz_OutsideRange_ThrowsAndKeepsPreviousValue(float rate)
        {
            var chorus = new ChorusSampleProvider(new SineWaveProvider(440f, 44100f));
            float previous = chorus.RateHz;

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => chorus.RateHz = rate);
            Assert.Equal(nameof(ChorusSampleProvider.RateHz), ex.ParamName);
            Assert.Equal(previous, chorus.RateHz);
        }

        [Theory]
        [InlineData(float.Epsilon)]
        [InlineData(50f)]
        public void DepthMs_WithinRange_IsAccepted(float depth)
        {
            var chorus = new ChorusSampleProvider(new SineWaveProvider(440f, 44100f));

            chorus.DepthMs = depth;

            Assert.Equal(depth, chorus.DepthMs);
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(-float.Epsilon)]
        [InlineData(50.0001f)]
        public void DepthMs_OutsideRange_ThrowsAndKeepsPreviousValue(float depth)
        {
            var chorus = new ChorusSampleProvider(new SineWaveProvider(440f, 44100f));
            float previous = chorus.DepthMs;

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => chorus.DepthMs = depth);
            Assert.Equal(nameof(ChorusSampleProvider.DepthMs), ex.ParamName);
            Assert.Equal(previous, chorus.DepthMs);
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(1f)]
        public void Mix_WithinRange_IsAccepted(float mix)
        {
            var chorus = new ChorusSampleProvider(new SineWaveProvider(440f, 44100f));

            chorus.Mix = mix;

            Assert.Equal(mix, chorus.Mix);
        }

        [Theory]
        [InlineData(-float.Epsilon)]
        [InlineData(1.0001f)]
        public void Mix_OutsideRange_ThrowsAndKeepsPreviousValue(float mix)
        {
            var chorus = new ChorusSampleProvider(new SineWaveProvider(440f, 44100f));
            float previous = chorus.Mix;

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => chorus.Mix = mix);
            Assert.Equal(nameof(ChorusSampleProvider.Mix), ex.ParamName);
            Assert.Equal(previous, chorus.Mix);
        }
    }
}