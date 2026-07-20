using System;
using Xunit;
using NAudioEffects;

namespace NAudioEffects.Tests
{
    public class SilenceDetectorTests
    {
        private const int SampleRate = 44100;
        private const int Channels = 2;
        private const float ThresholdDb = -50f;
        private const double MinDurationSeconds = 0.5;

        private SilenceDetector CreateDetector()
        {
            return new SilenceDetector(SampleRate, Channels, ThresholdDb, MinDurationSeconds);
        }

        private float[] CreateBuffer(int seconds, float amplitude)
        {
            int frames = SampleRate * seconds;
            int samples = frames * Channels;
            var buffer = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                buffer[i] = amplitude;
            }
            return buffer;
        }

        [Fact]
        public void PureSilence_IsDetected()
        {
            var detector = CreateDetector();
            var buffer = CreateBuffer(1, 0f); // 1 second of silence
            detector.Process(buffer, 0, buffer.Length);
            detector.Complete();

            Assert.Single(detector.Regions);
            var region = detector.Regions[0];
            Assert.Equal(TimeSpan.FromSeconds(1), region.Duration);
            Assert.Equal(TimeSpan.Zero, region.Start);
            Assert.Equal(TimeSpan.FromSeconds(1), region.End);
        }

        [Fact]
        public void SignalAboveThreshold_IsNotDetected()
        {
            var detector = CreateDetector();
            // 0.1 amplitude ≈ -20 dB, above -50 dB threshold
            var buffer = CreateBuffer(1, 0.1f);
            detector.Process(buffer, 0, buffer.Length);
            detector.Complete();

            Assert.Empty(detector.Regions);
        }

        [Fact]
        public void ThresholdBoundary_Behavior()
        {
            var detector = CreateDetector();

            // Amplitude exactly at threshold (-50 dB)
            float amplitudeAtThreshold = (float)Math.Pow(10.0, ThresholdDb / 20.0);
            var bufferAtThreshold = CreateBuffer(1, amplitudeAtThreshold);
            detector.Process(bufferAtThreshold, 0, bufferAtThreshold.Length);
            detector.Complete();
            Assert.Empty(detector.Regions);

            // Amplitude just below threshold (slightly quieter)
            float amplitudeBelowThreshold = amplitudeAtThreshold * 0.9f;
            var bufferBelowThreshold = CreateBuffer(1, amplitudeBelowThreshold);
            detector = CreateDetector(); // reset
            detector.Process(bufferBelowThreshold, 0, bufferBelowThreshold.Length);
            detector.Complete();
            Assert.Single(detector.Regions);
        }

        [Fact]
        public void ShortSilenceBelowMinimumDuration_IsIgnored()
        {
            var detector = CreateDetector();
            // 0.3 seconds of silence, below the 0.5s minimum
            int frames = (int)(SampleRate * 0.3);
            int samples = frames * Channels;
            var buffer = new float[samples]; // all zeros
            detector.Process(buffer, 0, buffer.Length);
            detector.Complete();

            Assert.Empty(detector.Regions);
        }
    }
}
