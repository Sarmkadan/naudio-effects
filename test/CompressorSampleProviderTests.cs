using System;
using NAudio.Wave;
using Xunit;

namespace NAudioEffects.Tests
{
    public class CompressorSampleProviderTests
    {
        private const int SampleRate = 44100;
        private const int Channels = 1;

        /// <summary>
        /// Generates a sine wave at the specified amplitude (linear, 0-1) for the given duration.
        /// </summary>
        private static float[] GenerateSineWave(float amplitude, double frequencyHz, int durationMs)
        {
            int sampleCount = SampleRate * durationMs / 1000;
            var buffer = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                double t = (double)i / SampleRate;
                buffer[i] = amplitude * (float)Math.Sin(2.0 * Math.PI * frequencyHz * t);
            }
            return buffer;
        }

        /// <summary>
        /// Generates a signal that alternates between quiet and loud sections.
        /// </summary>
        private static float[] GenerateAlternatingSignal(float quietAmplitude, float loudAmplitude, int sectionDurationMs, int numCycles)
        {
            int sectionSamples = SampleRate * sectionDurationMs / 1000;
            int totalSamples = sectionSamples * 2 * numCycles;
            var buffer = new float[totalSamples];

            for (int cycle = 0; cycle < numCycles; cycle++)
            {
                int baseIndex = cycle * sectionSamples * 2;

                // Quiet section
                for (int i = 0; i < sectionSamples; i++)
                {
                    double t = (double)(baseIndex + i) / SampleRate;
                    buffer[baseIndex + i] = quietAmplitude * (float)Math.Sin(2.0 * Math.PI * 440.0 * t);
                }

                // Loud section
                for (int i = 0; i < sectionSamples; i++)
                {
                    double t = (double)(baseIndex + sectionSamples + i) / SampleRate;
                    buffer[baseIndex + sectionSamples + i] = loudAmplitude * (float)Math.Sin(2.0 * Math.PI * 440.0 * t);
                }
            }

            return buffer;
        }

        /// <summary>
        /// Sample provider that reads from a pre-generated buffer.
        /// </summary>
        private class BufferSampleProvider : ISampleProvider
        {
            private readonly float[] _buffer;
            private int _position;

            public BufferSampleProvider(float[] buffer, int sampleRate = SampleRate, int channels = Channels)
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

        /// <summary>
        /// Computes RMS in dBFS of a buffer.
        /// </summary>
        private static float ComputeRmsDb(float[] buffer, int offset, int count)
        {
            if (count <= 0)
                return float.NegativeInfinity;

            double sumSquares = 0.0;
            for (int i = 0; i < count; i++)
            {
                float sample = buffer[offset + i];
                sumSquares += sample * sample;
            }

            float rms = (float)Math.Sqrt(sumSquares / count);
            return EffectSampleProviderBase.LinearToDb(rms);
        }

        /// <summary>
        /// Computes RMS in dBFS of a buffer (full buffer).
        /// </summary>
        private static float ComputeRmsDb(float[] buffer)
        {
            return ComputeRmsDb(buffer, 0, buffer.Length);
        }

        [Fact]
        public void Compressor_QuietSection_RmsUnchangedWithinTolerance()
        {
            // -30 dBFS = 10^(-30/20) = 0.03162
            // -3 dBFS = 10^(-3/20) = 0.7079
            float quietAmplitude = EffectSampleProviderBase.DbToLinear(-30f);
            float loudAmplitude = EffectSampleProviderBase.DbToLinear(-3f);

            var inputSignal = GenerateAlternatingSignal(quietAmplitude, loudAmplitude, 1000, 2);
            var source = new BufferSampleProvider(inputSignal);

            // Process only the quiet section (first 1 second) with a fresh compressor
            // to ensure the envelope follower starts from a clean state
            var compressor = new CompressorSampleProvider(source)
            {
                ThresholdDb = -20f,
                Ratio = 4f,
                AttackMs = 5f,
                ReleaseMs = 50f,
                KneeDb = 0f, // Hard knee for predictable behavior
                MakeupGainDb = 0f
            };

            // Process only the quiet section (first second of audio)
            int sectionSamples = SampleRate * 1000 / 1000; // 1 second at 44.1kHz
            var quietInputSection = new float[sectionSamples];
            Array.Copy(inputSignal, 0, quietInputSection, 0, sectionSamples);
            var quietOutputBuffer = new float[sectionSamples];
            compressor.Read(quietOutputBuffer, 0, sectionSamples);

            float quietInputRmsDb = ComputeRmsDb(quietInputSection, 0, sectionSamples);
            float quietOutputRmsDb = ComputeRmsDb(quietOutputBuffer, 0, sectionSamples);

            // Quiet section at -30 dBFS is well below threshold (-20 dBFS), should be unchanged
            float diff = Math.Abs(quietOutputRmsDb - quietInputRmsDb);
            Assert.True(diff <= 0.5f, $"Quiet section RMS changed by {diff:F3} dB (expected <= 0.5 dB). Input: {quietInputRmsDb:F2} dB, Output: {quietOutputRmsDb:F2} dB");
        }

        [Fact]
        public void Compressor_LoudSection_RmsReducedByExpectedAmount()
        {
            // -30 dBFS = 10^(-30/20) = 0.03162
            // -3 dBFS = 10^(-3/20) = 0.7079
            float quietAmplitude = EffectSampleProviderBase.DbToLinear(-30f);
            float loudAmplitude = EffectSampleProviderBase.DbToLinear(-3f);

            // Generate a signal with quiet then loud section
            int sectionSamples = SampleRate * 1000 / 1000; // 1 second at 44.1kHz
            var quietSection = GenerateSineWave(quietAmplitude, 440.0, 1000);
            var loudSection = GenerateSineWave(loudAmplitude, 440.0, 1000);

            // First process quiet section to let envelope settle to quiet level
            var quietSource = new BufferSampleProvider(quietSection);
            var compressor = new CompressorSampleProvider(quietSource)
            {
                ThresholdDb = -20f,
                Ratio = 4f,
                AttackMs = 5f,
                ReleaseMs = 50f,
                KneeDb = 0f, // Hard knee
                MakeupGainDb = 0f
            };
            var quietOutput = new float[sectionSamples];
            compressor.Read(quietOutput, 0, sectionSamples);

            // Now process loud section with the same compressor (envelope carries over)
            var loudSource = new BufferSampleProvider(loudSection);
            // Need to create a new compressor with the same envelope state
            // Since we can't share envelope, we'll process the full signal in one go
            // but measure after attack settles

            var fullSignal = new float[sectionSamples * 2];
            Array.Copy(quietSection, 0, fullSignal, 0, sectionSamples);
            Array.Copy(loudSection, 0, fullSignal, sectionSamples, sectionSamples);

            var fullSource = new BufferSampleProvider(fullSignal);
            var compressor2 = new CompressorSampleProvider(fullSource)
            {
                ThresholdDb = -20f,
                Ratio = 4f,
                AttackMs = 5f,
                ReleaseMs = 50f,
                KneeDb = 0f,
                MakeupGainDb = 0f
            };

            var fullOutput = new float[fullSignal.Length];
            compressor2.Read(fullOutput, 0, fullOutput.Length);

            // Measure loud section after attack has settled
            // Attack is 5ms, so after ~5 time constants (25ms) it should be settled
            int attackSettleSamples = (int)(SampleRate * 25f / 1000f); // 25ms
            int measureStart = sectionSamples + attackSettleSamples;
            int measureCount = sectionSamples - attackSettleSamples;

            float loudInputRmsDb = ComputeRmsDb(loudSection, 0, sectionSamples);
            float loudOutputRmsDb = ComputeRmsDb(fullOutput, measureStart, measureCount);

            // Expected reduction: input is -3 dBFS, threshold is -20 dBFS
            // Over-threshold = -3 - (-20) = 17 dB
            // With ratio 4:1, output over-threshold = 17 * (1/4) = 4.25 dB
            // Expected output level = threshold + 4.25 = -20 + 4.25 = -15.75 dBFS
            // Reduction = input - output = -3 - (-15.75) = 12.75 dB
            float expectedOutputDb = -20f + (loudInputRmsDb - (-20f)) / 4f;
            float expectedReductionDb = loudInputRmsDb - expectedOutputDb;
            float actualReductionDb = loudInputRmsDb - loudOutputRmsDb;

            Assert.True(Math.Abs(actualReductionDb - expectedReductionDb) <= 2f,
                $"Loud section reduction {actualReductionDb:F2} dB differs from expected {expectedReductionDb:F2} dB by more than 2 dB. Input: {loudInputRmsDb:F2} dB, Output: {loudOutputRmsDb:F2} dB");
        }

        [Fact]
        public void Compressor_NoOutputExceedsUnity()
        {
            // Use a very loud signal to test limiting behavior
            float loudAmplitude = EffectSampleProviderBase.DbToLinear(-1f); // -1 dBFS, close to full scale
            float quietAmplitude = EffectSampleProviderBase.DbToLinear(-30f);

            var inputSignal = GenerateAlternatingSignal(quietAmplitude, loudAmplitude, 1000, 2);
            var source = new BufferSampleProvider(inputSignal);

            var compressor = new CompressorSampleProvider(source)
            {
                ThresholdDb = -20f,
                Ratio = 10f, // High ratio
                AttackMs = 1f, // Fast attack
                ReleaseMs = 50f,
                KneeDb = 0f,
                MakeupGainDb = 0f
            };

            var outputBuffer = new float[inputSignal.Length];
            int totalRead = compressor.Read(outputBuffer, 0, outputBuffer.Length);
            Assert.Equal(inputSignal.Length, totalRead);

            // No sample should exceed 1.0 (digital full scale)
            foreach (float sample in outputBuffer)
            {
                Assert.True(sample <= 1.0f + 1e-6f, $"Output sample {sample} exceeds 1.0");
                Assert.True(sample >= -1.0f - 1e-6f, $"Output sample {sample} exceeds -1.0");
            }
        }

        [Fact]
        public void Compressor_RatioOneToOne_PassesAudioUnchanged()
        {
            float quietAmplitude = EffectSampleProviderBase.DbToLinear(-30f);
            float loudAmplitude = EffectSampleProviderBase.DbToLinear(-3f);

            var inputSignal = GenerateAlternatingSignal(quietAmplitude, loudAmplitude, 1000, 2);
            var source = new BufferSampleProvider(inputSignal);

            var compressor = new CompressorSampleProvider(source)
            {
                ThresholdDb = -20f,
                Ratio = 1f, // 1:1 = no compression
                AttackMs = 5f,
                ReleaseMs = 50f,
                KneeDb = 0f,
                MakeupGainDb = 0f
            };

            var outputBuffer = new float[inputSignal.Length];
            int totalRead = compressor.Read(outputBuffer, 0, outputBuffer.Length);
            Assert.Equal(inputSignal.Length, totalRead);

            // With 1:1 ratio, output should match input exactly (within floating point precision)
            for (int i = 0; i < inputSignal.Length; i++)
            {
                Assert.Equal(inputSignal[i], outputBuffer[i], 6); // 6 decimal places precision
            }
        }

        [Fact]
        public void Compressor_WithMakeupGain_AppliesGain()
        {
            float loudAmplitude = EffectSampleProviderBase.DbToLinear(-3f);
            float quietAmplitude = EffectSampleProviderBase.DbToLinear(-30f);

            var inputSignal = GenerateAlternatingSignal(quietAmplitude, loudAmplitude, 1000, 1);
            var source = new BufferSampleProvider(inputSignal);

            float makeupGainDb = 6f;
            var compressor = new CompressorSampleProvider(source)
            {
                ThresholdDb = -20f,
                Ratio = 4f,
                AttackMs = 5f,
                ReleaseMs = 50f,
                KneeDb = 0f,
                MakeupGainDb = makeupGainDb
            };

            var outputBuffer = new float[inputSignal.Length];
            int totalRead = compressor.Read(outputBuffer, 0, outputBuffer.Length);
            Assert.Equal(inputSignal.Length, totalRead);

            int sectionSamples = SampleRate * 1000 / 1000;

            // Check loud section - should have makeup gain applied
            float loudInputRmsDb = ComputeRmsDb(inputSignal, sectionSamples, sectionSamples);
            float loudOutputRmsDb = ComputeRmsDb(outputBuffer, sectionSamples, sectionSamples);

            // Expected: compressed level + makeup gain
            float expectedCompressedDb = -20f + (loudInputRmsDb - (-20f)) / 4f;
            float expectedWithMakeupDb = expectedCompressedDb + makeupGainDb;

            Assert.True(Math.Abs(loudOutputRmsDb - expectedWithMakeupDb) <= 2f,
                $"With makeup gain: output {loudOutputRmsDb:F2} dB vs expected {expectedWithMakeupDb:F2} dB");
        }

        [Fact]
        public void Compressor_Bypass_PassesAudioUnchanged()
        {
            float quietAmplitude = EffectSampleProviderBase.DbToLinear(-30f);
            float loudAmplitude = EffectSampleProviderBase.DbToLinear(-3f);

            var inputSignal = GenerateAlternatingSignal(quietAmplitude, loudAmplitude, 1000, 1);
            var source = new BufferSampleProvider(inputSignal);

            var compressor = new CompressorSampleProvider(source)
            {
                ThresholdDb = -20f,
                Ratio = 4f,
                AttackMs = 5f,
                ReleaseMs = 50f,
                KneeDb = 0f,
                MakeupGainDb = 0f,
                Bypass = true
            };

            var outputBuffer = new float[inputSignal.Length];
            int totalRead = compressor.Read(outputBuffer, 0, outputBuffer.Length);
            Assert.Equal(inputSignal.Length, totalRead);

            for (int i = 0; i < inputSignal.Length; i++)
            {
                Assert.Equal(inputSignal[i], outputBuffer[i], 6);
            }
        }

        [Fact]
        public void Compressor_CurrentGainReductionDb_ReportsReduction()
        {
            float loudAmplitude = EffectSampleProviderBase.DbToLinear(-3f);
            var inputSignal = GenerateSineWave(loudAmplitude, 440.0, 1000);
            var source = new BufferSampleProvider(inputSignal);

            var compressor = new CompressorSampleProvider(source)
            {
                ThresholdDb = -20f,
                Ratio = 4f,
                AttackMs = 5f,
                ReleaseMs = 50f,
                KneeDb = 0f,
                MakeupGainDb = 0f
            };

            var outputBuffer = new float[inputSignal.Length];
            compressor.Read(outputBuffer, 0, outputBuffer.Length);

            // After processing, gain reduction should be negative (reduction)
            // For -3 dBFS input with -20 dBFS threshold and 4:1 ratio:
            // Over-threshold = 17 dB, reduction = 17 * (1 - 1/4) = 12.75 dB
            // So gain reduction should be around -12.75 dB
            Assert.True(compressor.CurrentGainReductionDb < -10f && compressor.CurrentGainReductionDb > -15f,
                $"CurrentGainReductionDb = {compressor.CurrentGainReductionDb:F2} dB, expected around -12.75 dB");
        }

        [Fact]
        public void Compressor_AttackRelease_TimingBehavior()
        {
            // Test that attack and release times affect the envelope
            float loudAmplitude = EffectSampleProviderBase.DbToLinear(-3f);
            float quietAmplitude = EffectSampleProviderBase.DbToLinear(-30f);

            // Fast attack, slow release
            var inputSignal = GenerateAlternatingSignal(quietAmplitude, loudAmplitude, 1000, 1);
            var source = new BufferSampleProvider(inputSignal);

            var compressor = new CompressorSampleProvider(source)
            {
                ThresholdDb = -20f,
                Ratio = 4f,
                AttackMs = 1f, // Very fast attack
                ReleaseMs = 500f, // Slow release
                KneeDb = 0f,
                MakeupGainDb = 0f
            };

            var outputBuffer = new float[inputSignal.Length];
            compressor.Read(outputBuffer, 0, outputBuffer.Length);

            int sectionSamples = SampleRate * 1000 / 1000;

            // After loud section ends, release should be slow
            // Check that gain reduction persists into the quiet section
            float gainReductionAtEndOfLoud = compressor.CurrentGainReductionDb;

            // Read a bit more to see release behavior (but we've consumed all input)
            // The gain reduction should still be significant due to slow release
            Assert.True(gainReductionAtEndOfLoud < -5f, $"Gain reduction at end of loud section: {gainReductionAtEndOfLoud:F2} dB");
        }
    }
}