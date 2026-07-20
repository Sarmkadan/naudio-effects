using Xunit;

namespace NAudioEffects;

public class EnvelopeFollowerTests
{
    [Fact]
    public void Constructor_WithDefaultParameters_InitializesCorrectly()
    {
        // Arrange & Act
        var follower = new EnvelopeFollower();

        // Assert
        Assert.Equal(0f, follower.Envelope);
    }

    [Fact]
    public void Constructor_WithCustomParameters_InitializesCorrectly()
    {
        // Arrange & Act
        var follower = new EnvelopeFollower(10f, 500f, 48000);

        // Assert
        Assert.Equal(0f, follower.Envelope);
    }

    [Fact]
    public void SetParameters_UpdatesCoefficients()
    {
        // Arrange
        var follower = new EnvelopeFollower();
        var initialAttack = follower.GetType().GetField("_attackCoefficient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(follower);
        var initialRelease = follower.GetType().GetField("_releaseCoefficient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(follower);

        // Act
        follower.SetParameters(50f, 100f, 44100);

        // Assert
        var newAttack = follower.GetType().GetField("_attackCoefficient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(follower);
        var newRelease = follower.GetType().GetField("_releaseCoefficient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(follower);

        Assert.NotEqual(initialAttack, newAttack);
        Assert.NotEqual(initialRelease, newRelease);
    }

    [Fact]
    public void Process_WithZeroSamples_EnvelopeDecaysToZero()
    {
        // Arrange
        var follower = new EnvelopeFollower(10f, 10f, 44100);
        var buffer = new float[100];

        // Act - fill with zeros
        follower.Process(buffer, 0, buffer.Length);

        // Assert - envelope should decay to near zero
        Assert.True(follower.Envelope < 0.001f);
    }

    [Fact]
    public void Process_WithPositiveSamples_EnvelopeRises()
    {
        // Arrange
        var follower = new EnvelopeFollower(1f, 200f, 44100);
        var buffer = new float[1000];

        // Fill with positive samples (0.5 amplitude)
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = 0.5f;
        }

        // Act
        follower.Process(buffer, 0, buffer.Length);

        // Assert - envelope should rise toward 0.5
        Assert.True(follower.Envelope > 0.4f);
        Assert.True(follower.Envelope < 0.6f);
    }

    [Fact]
    public void Process_WithNegativeSamples_EnvelopeRectifiesToPositive()
    {
        // Arrange
        var follower = new EnvelopeFollower(1f, 200f, 44100);
        var buffer = new float[1000];

        // Fill with negative samples (-0.3 amplitude)
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = -0.3f;
        }

        // Act
        follower.Process(buffer, 0, buffer.Length);

        // Assert - envelope should rectify to positive 0.3
        Assert.True(follower.Envelope > 0.25f);
        Assert.True(follower.Envelope < 0.35f);
    }

    [Fact]
    public void Process_WithRisingInput_EnvelopeIncreasesPerAttack()
    {
        // Arrange
        var follower = new EnvelopeFollower(10f, 200f, 44100);
        var buffer = new float[100];

        // Start with low amplitude
        for (int i = 0; i < 50; i++)
        {
            buffer[i] = 0.1f;
        }

        // Then increase to high amplitude
        for (int i = 50; i < 100; i++)
        {
            buffer[i] = 0.8f;
        }

        // Act
        follower.Process(buffer, 0, buffer.Length);

        // Assert - envelope should be closer to the higher amplitude
        Assert.True(follower.Envelope > 0.3f);
    }

    [Fact]
    public void Process_WithFallingInput_EnvelopeDecaysPerRelease()
    {
        // Arrange
        var follower = new EnvelopeFollower(1f, 50f, 44100);
        var buffer = new float[200];

        // Start with high amplitude
        for (int i = 0; i < 100; i++)
        {
            buffer[i] = 0.8f;
        }

        // Then drop to zero
        Array.Fill(buffer, 0f, 100, 100);

        // Act
        follower.Process(buffer, 0, buffer.Length);

        // Assert - envelope should decay but not immediately reach zero
        Assert.True(follower.Envelope < 0.8f);
        Assert.True(follower.Envelope > 0.01f);
    }

    [Fact]
    public void Process_WithMixedSamples_TracksEnvelopeCorrectly()
    {
        // Arrange
        var follower = new EnvelopeFollower(5f, 100f, 44100);
        var buffer = new float[1000];

        // Create a sine wave pattern
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = (float)Math.Sin(i * 0.1f) * 0.5f;
        }

        // Act
        follower.Process(buffer, 0, buffer.Length);

        // Assert - envelope should track the absolute value of the sine wave
        Assert.True(follower.Envelope > 0.4f);
        Assert.True(follower.Envelope < 0.6f);
    }

    [Fact]
    public void CalculateCoefficient_WithZeroTimeConstant_ReturnsZero()
    {
        // Arrange
        var follower = new EnvelopeFollower();

        // Act
        var result = follower.GetType().GetMethod("CalculateCoefficient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(follower, new object[] { 0f, 44100 });

        // Assert
        Assert.Equal(0f, result);
    }

    [Fact]
    public void CalculateCoefficient_WithPositiveTimeConstant_ReturnsValidCoefficient()
    {
        // Arrange
        var follower = new EnvelopeFollower();

        // Act
        var result = follower.GetType().GetMethod("CalculateCoefficient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(follower, new object[] { 10f, 44100 });

        // Assert
        Assert.IsType<float>(result);
        Assert.True((float)result > 0f);
        Assert.True((float)result < 1f);
    }
}
