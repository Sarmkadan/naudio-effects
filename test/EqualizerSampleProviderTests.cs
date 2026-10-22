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

    private class ArraySampleProvider : ISampleProvider
    {
        private readonly float[] _samples;
        private int _position;

        public ArraySampleProvider(float[] samples, int sampleRate = 44100)
        {
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
            _samples = samples;
        }

        public WaveFormat WaveFormat { get; }

        public int Read(float[] buffer, int offset, int count)
        {
            int toRead = Math.Min(count, _samples.Length - _position);
            if (toRead <= 0) return 0;

            Array.Copy(_samples, _position, buffer, offset, toRead);
            _position += toRead;
            return toRead;
        }
    }

    private static float[] CreateSine(int length, float frequency, int sampleRate = 44100)
    {
        var samples = new float[length];
        for (int i = 0; i < length; i++)
        {
            samples[i] = 0.5f * MathF.Sin(2f * MathF.PI * frequency * i / sampleRate);
        }
        return samples;
    }

    private static float[] ReadAll(ISampleProvider provider, int length)
    {
        var output = new float[length];
        int read = provider.Read(output, 0, length);
        Assert.Equal(length, read);
        return output;
    }

    private static readonly EqualizerBand[] TwoBands =
    {
        new(200f, 6f, 0.7f),
        new(1000f, -9f, 1.4f, EqualizerFilterType.HighShelf)
    };

    [Fact]
    public void Constructor_WithEmptyBandList_ThrowsArgumentException()
    {
        var source = new ConstantSampleProvider(0.5f, 1024);

        Assert.Throws<ArgumentException>(() => new EqualizerSampleProvider(source, Array.Empty<EqualizerBand>()));
    }

    [Fact]
    public void Constructor_WithNullBandList_ThrowsArgumentNullException()
    {
        var source = new ConstantSampleProvider(0.5f, 1024);

        Assert.Throws<ArgumentNullException>(() => new EqualizerSampleProvider(source, (System.Collections.Generic.IReadOnlyList<EqualizerBand>)null!));
    }

    [Theory]
    [InlineData(0f, 0f, 1f)]
    [InlineData(22050f, 0f, 1f)]
    [InlineData(1000f, 25f, 1f)]
    [InlineData(1000f, 0f, 0f)]
    public void Constructor_WithInvalidBand_ThrowsArgumentOutOfRangeException(float frequency, float gainDb, float q)
    {
        var source = new ConstantSampleProvider(0.5f, 1024);
        var bands = new[] { new EqualizerBand(frequency, gainDb, q) };

        Assert.Throws<ArgumentOutOfRangeException>(() => new EqualizerSampleProvider(source, bands));
    }

    [Fact]
    public void Constructor_WithInvalidBandAtEightKilohertz_ThrowsForNyquist()
    {
        // The default 12 kHz top band is above Nyquist for 8 kHz audio.
        var source = new ConstantSampleProvider(0.5f, 1024, sampleRate: 8000);

        Assert.Throws<ArgumentOutOfRangeException>(() => new EqualizerSampleProvider(source, bandCount: 5));
    }

    [Fact]
    public void Constructor_WithValidBands_ExposesBandsAndCount()
    {
        var source = new ConstantSampleProvider(0.5f, 1024);

        var eq = new EqualizerSampleProvider(source, TwoBands);

        Assert.Equal(2, eq.BandCount);
        Assert.Equal(TwoBands[0], eq.GetBand(0));
        Assert.Equal(TwoBands[1], eq.GetBand(1));
    }

    [Fact]
    public void UpdateBand_WithValidBand_ReplacesSettings()
    {
        var source = new ConstantSampleProvider(0.5f, 1024);
        var eq = new EqualizerSampleProvider(source, TwoBands);
        var replacement = new EqualizerBand(500f, 3f, 2f);

        eq.UpdateBand(1, replacement);

        Assert.Equal(replacement, eq.GetBand(1));
    }

    [Fact]
    public void UpdateBand_WithInvalidBandIndex_ThrowsArgumentOutOfRangeException()
    {
        var source = new ConstantSampleProvider(0.5f, 1024);
        var eq = new EqualizerSampleProvider(source, TwoBands);

        Assert.Throws<ArgumentOutOfRangeException>(() => eq.UpdateBand(2, new EqualizerBand(500f, 3f)));
        Assert.Throws<ArgumentOutOfRangeException>(() => eq.UpdateBand(-1, new EqualizerBand(500f, 3f)));
    }

    [Theory]
    [InlineData(0f, 3f, 1f)]
    [InlineData(-50f, 3f, 1f)]
    [InlineData(22050f, 3f, 1f)]
    [InlineData(500f, 30f, 1f)]
    [InlineData(500f, -30f, 1f)]
    [InlineData(500f, 3f, 0f)]
    [InlineData(500f, 3f, -1f)]
    public void UpdateBand_WithInvalidSettings_ThrowsAndLeavesBandUnchanged(float frequency, float gainDb, float q)
    {
        var source = new ConstantSampleProvider(0.5f, 1024);
        var eq = new EqualizerSampleProvider(source, TwoBands);

        Assert.Throws<ArgumentOutOfRangeException>(() => eq.UpdateBand(0, new EqualizerBand(frequency, gainDb, q)));

        Assert.Equal(TwoBands[0], eq.GetBand(0));
        Assert.Equal(TwoBands[1], eq.GetBand(1));
    }

    [Fact]
    public void SetBandGain_WithInvalidQ_ThrowsAndLeavesBandUnchanged()
    {
        var source = new ConstantSampleProvider(0.5f, 1024);
        var eq = new EqualizerSampleProvider(source, TwoBands);

        Assert.Throws<ArgumentOutOfRangeException>(() => eq.SetBandGain(0, 6f, EqualizerFilterType.LowShelf, 0f));

        Assert.Equal(TwoBands[0], eq.GetBand(0));
    }

    [Fact]
    public void SetBandGain_WithOutOfRangeGain_ThrowsAndLeavesBandUnchanged()
    {
        var source = new ConstantSampleProvider(0.5f, 1024);
        var eq = new EqualizerSampleProvider(source, TwoBands);

        Assert.Throws<ArgumentOutOfRangeException>(() => eq.SetBandGain(1, 24.5f));

        Assert.Equal(TwoBands[1], eq.GetBand(1));
    }

    [Fact]
    public void SetBandFrequency_WithInvalidFrequency_ThrowsAndLeavesBandUnchanged()
    {
        var source = new ConstantSampleProvider(0.5f, 1024);
        var eq = new EqualizerSampleProvider(source, TwoBands);

        Assert.Throws<ArgumentOutOfRangeException>(() => eq.SetBandFrequency(0, 30000f));

        Assert.Equal(TwoBands[0], eq.GetBand(0));
    }

    [Fact]
    public void UpdateBand_WithInvalidSettings_LeavesPreviousCoefficientsIntact()
    {
        // Two identical equalizers process the same signal. One receives a rejected update;
        // its output must still match the untouched one sample-for-sample.
        float[] signal = CreateSine(2048, 200f);
        var reference = new EqualizerSampleProvider(new ArraySampleProvider(signal), TwoBands);
        var rejected = new EqualizerSampleProvider(new ArraySampleProvider(signal), TwoBands);

        Assert.Throws<ArgumentOutOfRangeException>(() => rejected.UpdateBand(0, new EqualizerBand(200f, 40f, 0.7f)));

        float[] expected = ReadAll(reference, signal.Length);
        float[] actual = ReadAll(rejected, signal.Length);
        Assert.Equal(expected, actual);

        // The filter must actually be shaping the signal, otherwise the comparison proves nothing.
        Assert.NotEqual(signal, expected);
    }

    [Fact]
    public void UpdateBand_AfterProcessing_WithRejectedUpdate_KeepsFilterAppliedToNextBlock()
    {
        // Process a block so the filters are built, then send a rejected update, then process the next block.
        float[] signal = CreateSine(4096, 200f);
        var reference = new EqualizerSampleProvider(new ArraySampleProvider(signal), TwoBands);
        var rejected = new EqualizerSampleProvider(new ArraySampleProvider(signal), TwoBands);

        ReadAll(reference, 2048);
        ReadAll(rejected, 2048);
        Assert.Throws<ArgumentOutOfRangeException>(() => rejected.UpdateBand(1, new EqualizerBand(1000f, 0f, -2f)));

        Assert.Equal(ReadAll(reference, 2048), ReadAll(rejected, 2048));
    }

    [Fact]
    public void UpdateBand_WithValidSettings_ChangesOutput()
    {
        float[] signal = CreateSine(2048, 200f);
        var unchanged = new EqualizerSampleProvider(new ArraySampleProvider(signal), TwoBands);
        var updated = new EqualizerSampleProvider(new ArraySampleProvider(signal), TwoBands);

        updated.UpdateBand(0, new EqualizerBand(200f, -12f, 0.7f));

        Assert.NotEqual(ReadAll(unchanged, signal.Length), ReadAll(updated, signal.Length));
    }
}
