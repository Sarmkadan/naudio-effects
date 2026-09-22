using System;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using Xunit;

namespace NAudioEffects.Tests;

public class EqualizerSampleProviderTests
{
    private class ConstantSampleProvider : ISampleProvider
    {
        private readonly float _value;
        private readonly int _totalSamples;
        private int _readCount;

        public ConstantSampleProvider(float value, int totalSamples, int sampleRate = 44100, int channels = 1)
        {
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
            _value = value;
            _totalSamples = totalSamples;
        }

        public WaveFormat WaveFormat { get; }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesToRead = Math.Min(count, _totalSamples - _readCount);
            if (samplesToRead <= 0) return 0;

            for (int i = 0; i < samplesToRead; i++)
            {
                buffer[offset + i] = _value;
            }
            _readCount += samplesToRead;
            return samplesToRead;
        }
    }

    [Fact]
    public void Read_WithZeroGain_ReturnsUnchangedSamples()
    {
        var source = new ConstantSampleProvider(0.5f, 1024);
        var eq = new EqualizerSampleProvider(source, bandCount: 3);
        var buffer = new float[1024];
        
        int read = eq.Read(buffer, 0, 1024);
        
        Assert.Equal(1024, read);
        for (int i = 0; i < 1024; i++)
        {
            Assert.Equal(0.5f, buffer[i], 5);
        }
    }

    [Fact]
    public void Read_WithCancellationDuringFilterUpdate_ThrowsOperationCanceledException()
    {
        var source = new ConstantSampleProvider(0.5f, 1024);
        var eq = new EqualizerSampleProvider(source, bandCount: 10);
        var buffer = new float[1024];
        
        // Set a gain to mark filters as dirty, forcing UpdateAllFilters to run
        eq.SetBandGain(0, 6.0f);
        
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately
        
        Assert.Throws<OperationCanceledException>(() => eq.Read(buffer, 0, 1024, cts.Token));
    }

    [Fact]
    public void Read_WithCancellationBetweenBlocks_ThrowsOperationCanceledException()
    {
        var source = new ConstantSampleProvider(0.5f, 1024);
        var eq = new EqualizerSampleProvider(source, bandCount: 3);
        var buffer = new float[1024];
        
        var cts = new CancellationTokenSource();
        // Cancel after first block (256 samples)
        Task.Run(async () =>
        {
            await Task.Delay(10);
            cts.Cancel();
        });
        
        Assert.Throws<OperationCanceledException>(() => eq.Read(buffer, 0, 1024, cts.Token));
    }

    [Fact]
    public void Read_AfterCancellation_StateIsNotCorrupted()
    {
        var source = new ConstantSampleProvider(0.5f, 2048);
        var eq = new EqualizerSampleProvider(source, bandCount: 3);
        var buffer = new float[2048];
        
        eq.SetBandGain(1, 12.0f);
        
        var cts = new CancellationTokenSource();
        Task.Run(async () =>
        {
            await Task.Delay(10);
            cts.Cancel();
        });
        
        try
        {
            eq.Read(buffer, 0, 2048, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        
        // Verify that subsequent reads work normally and state is intact
        eq.SetBandGain(2, -6.0f); // Should not throw
        var buffer2 = new float[1024];
        int read = eq.Read(buffer2, 0, 1024);
        Assert.Equal(1024, read);
        
        // Verify that the equalizer actually processes samples (values change due to gain)
        // With 12dB gain on band 1, a constant 0.5 input will be amplified.
        Assert.True(buffer2[0] != 0.5f || eq.Bypass, "Samples should be processed by EQ");
    }

    [Fact]
    public void Builder_Build_WithBands_ReturnsEqualizerWithCorrectBandCount()
    {
        var source = new ConstantSampleProvider(0.5f, 1024);
        var eq = new EqualizerBuilder()
            .AddBand(1000f, 6.0f, 1.0f)
            .AddLowShelf(200f, 3.0f, 0.7f)
            .AddHighShelf(8000f, -4.0f, 0.7f)
            .Build(source);

        Assert.Equal(3, eq.BandCount);
        
        var buffer = new float[1024];
        int read = eq.Read(buffer, 0, 1024);
        Assert.Equal(1024, read);
    }

    [Fact]
    public void Builder_Build_WithEmptyBands_ThrowsInvalidOperationException()
    {
        var source = new ConstantSampleProvider(0.5f, 1024);
        var builder = new EqualizerBuilder();
        
        Assert.Throws<InvalidOperationException>(() => builder.Build(source));
    }

    [Fact]
    public void Builder_IsReusable()
    {
        var source1 = new ConstantSampleProvider(0.5f, 1024);
        var source2 = new ConstantSampleProvider(0.5f, 1024);
        
        var builder = new EqualizerBuilder()
            .AddBand(500f, 10.0f, 1.0f);
            
        var eq1 = builder.Build(source1);
        Assert.Equal(1, eq1.BandCount);
        
        // Reuse builder
        var eq2 = builder
            .AddPeak(1000f, -5.0f, 1.0f)
            .Build(source2);
            
        Assert.Equal(1, eq2.BandCount);
    }

    [Fact]
    public void Builder_Build_WithInvalidFrequency_ThrowsArgumentOutOfRangeException()
    {
        var source = new ConstantSampleProvider(0.5f, 1024);
        var builder = new EqualizerBuilder()
            .AddBand(0f, 6.0f, 1.0f); // Invalid frequency
            
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.Build(source));
    }
}
