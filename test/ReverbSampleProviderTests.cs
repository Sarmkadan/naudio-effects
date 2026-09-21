using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using Xunit;
using System.Collections.Generic;

namespace NAudioEffects.Tests
{
    public class ReverbSampleProviderTests
    {
        private class ConstantSampleProvider : ISampleProvider
        {
            public WaveFormat WaveFormat => WaveFormat.CreateIeeeFloatWaveFormat(44100, 1);
            public int Read(float[] buffer, int offset, int count)
            {
                for (int i = 0; i < count; i++) buffer[offset + i] = 0.5f;
                return count;
            }
        }

        private class FiniteSampleProvider : ISampleProvider
        {
            private readonly float[] _samples;
            private int _position;
            private readonly int _chunkSize;

            public FiniteSampleProvider(float[] samples, int chunkSize = 100, int sampleRate = 44100, int channels = 1)
            {
                _samples = samples;
                _chunkSize = chunkSize;
                WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
            }

            public WaveFormat WaveFormat { get; }

            public int Read(float[] buffer, int offset, int count)
            {
                if (_position >= _samples.Length)
                    return 0;

                int remaining = _samples.Length - _position;
                int toCopy = Math.Min(Math.Min(count, _chunkSize), remaining);
                Array.Copy(_samples, _position, buffer, offset, toCopy);
                _position += toCopy;
                return toCopy;
            }
        }

        [Fact]
        public async Task RenderToAsync_CompletesSuccessfully()
        {
            // Create a finite source of 1 second of silence (44100 samples of 0)
            var samples = new float[44100];
            var source = new FiniteSampleProvider(samples);
            using var destination = new MemoryStream();

            await ReverbSampleProviderExtensions.RenderToAsync(source, destination);

            Assert.True(destination.Length > 0);
            // Expect 44100 samples * 4 bytes per float = 176400 bytes
            Assert.Equal(44100 * 4, destination.Length);
        }

        [Fact]
        public async Task RenderToAsync_HonorsCancellation()
        {
            // Use a finite but long source to allow cancellation to trigger
            var samples = new float[44100 * 10]; // 10 seconds of silence
            var source = new FiniteSampleProvider(samples, chunkSize: 100);
            using var destination = new MemoryStream();
            using var cts = new CancellationTokenSource();

            // Cancel after a short delay
            cts.CancelAfter(50);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                ReverbSampleProviderExtensions.RenderToAsync(source, destination, cts.Token));
        }

        [Fact]
        public async Task RenderToAsync_ReportsProgress()
        {
            var samples = new float[44100]; // 1 second
            var source = new FiniteSampleProvider(samples, chunkSize: 100);
            using var destination = new MemoryStream();
            var progressValues = new List<float>();
            var progress = new Progress<float>();
            progress.ProgressChanged += (_, value) => progressValues.Add(value);

            await ReverbSampleProviderExtensions.RenderToAsync(source, destination, progress: progress);

            Assert.Equal(2, progressValues.Count);
            Assert.Equal(0f, progressValues[0]);
            Assert.Equal(1f, progressValues[1]);
        }

        [Fact]
        public void Create_ReturnsBuilder()
        {
            var builder = ReverbSampleProvider.Create();
            Assert.NotNull(builder);
            Assert.IsType<ReverbSampleProvider.ReverbBuilder>(builder);
        }

        [Fact]
        public void Builder_DefaultValues()
        {
            var builder = ReverbSampleProvider.Create();

            // Access via reflection or by building and checking properties
            var source = new ConstantSampleProvider();
            var reverb = builder.Build(source);

            Assert.Equal(0.5f, reverb.RoomSize);
            Assert.Equal(0.5f, reverb.Damping);
            Assert.Equal(0.33f, reverb.WetLevel);
            Assert.Equal(0.67f, reverb.DryLevel);
            Assert.Equal(0.0f, reverb.PreDelaySeconds);
        }

        [Fact]
        public void Builder_WithRoomSize_SetsValue()
        {
            var builder = ReverbSampleProvider.Create()
                .WithRoomSize(0.8f);

            var source = new ConstantSampleProvider();
            var reverb = builder.Build(source);

            Assert.Equal(0.8f, reverb.RoomSize);
        }

        [Fact]
        public void Builder_WithRoomSize_ClampsValues()
        {
            var builder = ReverbSampleProvider.Create()
                .WithRoomSize(1.5f); // Should clamp to 1.0

            var source = new ConstantSampleProvider();
            var reverb = builder.Build(source);

            Assert.Equal(1.0f, reverb.RoomSize);

            builder.WithRoomSize(-0.5f); // Should clamp to 0.0
            reverb = builder.Build(source);

            Assert.Equal(0.0f, reverb.RoomSize);
        }

        [Fact]
        public void Builder_WithDamping_SetsValue()
        {
            var builder = ReverbSampleProvider.Create()
                .WithDamping(0.2f);

            var source = new ConstantSampleProvider();
            var reverb = builder.Build(source);

            Assert.Equal(0.2f, reverb.Damping);
        }

        [Fact]
        public void Builder_WithWetDryMix_SetsValue()
        {
            var builder = ReverbSampleProvider.Create()
                .WithWetDryMix(0.7f);

            var source = new ConstantSampleProvider();
            var reverb = builder.Build(source);

            Assert.Equal(0.7f, reverb.WetLevel);
        }

        [Fact]
        public void Builder_WithPreDelay_SetsValue()
        {
            var builder = ReverbSampleProvider.Create()
                .WithPreDelay(0.1f);

            var source = new ConstantSampleProvider();
            var reverb = builder.Build(source);

            Assert.Equal(0.1f, reverb.PreDelaySeconds);
        }

        [Fact]
        public void Builder_WithPreDelay_ClampsValues()
        {
            var builder = ReverbSampleProvider.Create()
                .WithPreDelay(3.0f); // Should clamp to 2.0

            var source = new ConstantSampleProvider();
            var reverb = builder.Build(source);

            Assert.Equal(2.0f, reverb.PreDelaySeconds);

            builder.WithPreDelay(-0.5f); // Should clamp to 0.0
            reverb = builder.Build(source);

            Assert.Equal(0.0f, reverb.PreDelaySeconds);
        }

        [Fact]
        public void Builder_Build_Throws_OnNullSource()
        {
            var builder = ReverbSampleProvider.Create();

            Assert.Throws<ArgumentNullException>(() => builder.Build(null));
        }

        [Fact]
        public void Builder_ChainedMethods_AllApply()
        {
            var builder = ReverbSampleProvider.Create()
                .WithRoomSize(0.9f)
                .WithDamping(0.1f)
                .WithWetDryMix(0.8f)
                .WithPreDelay(0.2f);

            var source = new ConstantSampleProvider();
            var reverb = builder.Build(source);

            Assert.Equal(0.9f, reverb.RoomSize);
            Assert.Equal(0.1f, reverb.Damping);
            Assert.Equal(0.8f, reverb.WetLevel);
            Assert.Equal(0.67f, reverb.DryLevel); // Default dry level unchanged
            Assert.Equal(0.2f, reverb.PreDelaySeconds);
        }
    }
}