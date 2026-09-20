using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using Xunit;

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

        [Fact]
        public async Task RenderToAsync_CompletesSuccessfully()
        {
            var source = new ConstantSampleProvider();
            using var destination = new MemoryStream();
            
            await ReverbSampleProviderExtensions.RenderToAsync(source, destination);
            
            Assert.True(destination.Length > 0);
        }

        [Fact]
        public async Task RenderToAsync_HonorsCancellation()
        {
            var source = new ConstantSampleProvider();
            using var destination = new MemoryStream();
            using var cts = new CancellationTokenSource();
            
            // Cancel immediately after first chunk
            cts.CancelAfter(1);
            
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => 
                ReverbSampleProviderExtensions.RenderToAsync(source, destination, cts.Token));
        }

        [Fact]
        public async Task RenderToAsync_ReportsProgress()
        {
            var source = new ConstantSampleProvider();
            using var destination = new MemoryStream();
            var progress = new Progress<float>();
            float reportedProgress = 0f;
            progress.ProgressChanged += (_, value) => reportedProgress = value;
            
            await ReverbSampleProviderExtensions.RenderToAsync(source, destination, progress: progress);
            
            Assert.Equal(1f, reportedProgress);
        }
    }
}
