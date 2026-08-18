using Xunit;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudioEffects;
using System;

namespace NAudioEffects.Tests
{
    public class SampleProviderAnalysisExtensionsTests
    {
        [Fact]
        public void MeasurePeak_SineWave_ReturnsCorrectValue()
        {
            var signalGenerator = new SignalGenerator(44100, 1)
            {
                Type = SignalGeneratorType.Sin,
                Gain = 0.5f,
                Frequency = 1000
            };

            // Measure peak for 1 second. Since it's a sine wave with amplitude 0.5, peak should be 0.5.
            float peak = signalGenerator.MeasurePeak(1.0);
            Assert.Equal(0.5f, peak, precision: 2);
        }

        [Fact]
        public void MeasureRms_SineWave_ReturnsCorrectValue()
        {
            var signalGenerator = new SignalGenerator(44100, 1)
            {
                Type = SignalGeneratorType.Sin,
                Gain = 0.5f,
                Frequency = 1000
            };

            // Measure RMS for 1 second. Sine wave RMS is amplitude / sqrt(2).
            // 0.5 / sqrt(2) ≈ 0.35355
            float rms = signalGenerator.MeasureRms(1.0);
            Assert.Equal(0.35355f, rms, precision: 3);
        }
    }
}
