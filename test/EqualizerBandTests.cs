using System;
using Xunit;

namespace NAudioEffects.Tests;

public class EqualizerBandTests
{
    private const int SampleRate = 44100;

    [Fact]
    public void Validate_WithValidBand_DoesNotThrow()
    {
        var band = new EqualizerBand(1000f, 6f, 1.0f);

        band.Validate(SampleRate);
    }

    [Fact]
    public void Validate_WithZeroFrequency_ThrowsArgumentOutOfRangeException()
    {
        var band = new EqualizerBand(0f, 6f, 1.0f);

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => band.Validate(SampleRate));
        Assert.Equal(nameof(EqualizerBand.Frequency), ex.ParamName);
    }

    [Fact]
    public void Validate_WithNegativeFrequency_ThrowsArgumentOutOfRangeException()
    {
        var band = new EqualizerBand(-100f, 6f, 1.0f);

        Assert.Throws<ArgumentOutOfRangeException>(() => band.Validate(SampleRate));
    }

    [Fact]
    public void Validate_WithFrequencyAtNyquist_ThrowsArgumentOutOfRangeException()
    {
        var band = new EqualizerBand(SampleRate / 2f, 6f, 1.0f);

        Assert.Throws<ArgumentOutOfRangeException>(() => band.Validate(SampleRate));
    }

    [Fact]
    public void Validate_WithFrequencyAboveNyquist_ThrowsArgumentOutOfRangeException()
    {
        var band = new EqualizerBand(30000f, 6f, 1.0f);

        Assert.Throws<ArgumentOutOfRangeException>(() => band.Validate(SampleRate));
    }

    [Fact]
    public void Validate_WithFrequencyJustBelowNyquist_DoesNotThrow()
    {
        var band = new EqualizerBand(SampleRate / 2f - 1f, 6f, 1.0f);

        band.Validate(SampleRate);
    }

    [Fact]
    public void Validate_WithNaNFrequency_ThrowsArgumentOutOfRangeException()
    {
        var band = new EqualizerBand(float.NaN, 6f, 1.0f);

        Assert.Throws<ArgumentOutOfRangeException>(() => band.Validate(SampleRate));
    }

    [Theory]
    [InlineData(24f)]
    [InlineData(-24f)]
    [InlineData(0f)]
    public void Validate_WithGainWithinLimits_DoesNotThrow(float gainDb)
    {
        var band = new EqualizerBand(1000f, gainDb, 1.0f);

        band.Validate(SampleRate);
    }

    [Theory]
    [InlineData(24.01f)]
    [InlineData(-24.01f)]
    [InlineData(100f)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NaN)]
    public void Validate_WithGainOutsideLimits_ThrowsArgumentOutOfRangeException(float gainDb)
    {
        var band = new EqualizerBand(1000f, gainDb, 1.0f);

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => band.Validate(SampleRate));
        Assert.Equal(nameof(EqualizerBand.GainDb), ex.ParamName);
    }

    [Fact]
    public void Validate_WithZeroQ_ThrowsArgumentOutOfRangeException()
    {
        var band = new EqualizerBand(1000f, 6f, 0f);

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => band.Validate(SampleRate));
        Assert.Equal(nameof(EqualizerBand.Q), ex.ParamName);
    }

    [Fact]
    public void Validate_WithNegativeQ_ThrowsArgumentOutOfRangeException()
    {
        var band = new EqualizerBand(1000f, 6f, -0.5f);

        Assert.Throws<ArgumentOutOfRangeException>(() => band.Validate(SampleRate));
    }

    [Fact]
    public void Validate_UsesSampleRateForFrequencyLimit()
    {
        // 12 kHz is valid at 44.1 kHz but at or above Nyquist for 16 kHz audio.
        var band = new EqualizerBand(12000f, 0f, 1.0f);

        band.Validate(44100);
        Assert.Throws<ArgumentOutOfRangeException>(() => band.Validate(16000));
    }

    [Fact]
    public void Record_DefaultsToPeakingFilterWithUnityQ()
    {
        var band = new EqualizerBand(1000f, 3f);

        Assert.Equal(1.0f, band.Q);
        Assert.Equal(EqualizerFilterType.Peaking, band.FilterType);
    }
}
