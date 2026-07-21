#nullable enable

using NAudio.Wave;

namespace NAudioEffects
{
    /// <summary>
    /// Factory methods for creating common audio effect chains and presets.
    /// These presets compose multiple effects (compressor, EQ, gate, limiter) over an ISampleProvider source.
    /// </summary>
    public static class EffectChainPresets
    {
        /// <summary>
        /// Creates a Vocal Polish preset chain.
        ///
        /// This preset enhances vocal clarity and presence by:
        /// 1. Applying gentle noise gating to reduce background noise
        /// 2. Using a 5-band EQ to boost presence frequencies (2-5kHz) and reduce muddiness (100-200Hz)
        /// 3. Applying light compression to even out vocal dynamics
        /// 4. Adding a limiter to prevent clipping
        ///
        /// Ideal for: podcast vocals, voiceovers, singing vocals in mixes
        /// </summary>
        /// <param name="source">The input audio source</param>
        /// <returns>An ISampleProvider with the complete effect chain applied</returns>
        public static ISampleProvider VocalPolish(ISampleProvider source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            // Step 1: Noise gate to reduce background noise and breaths below threshold
            var noiseGate = new NoiseGateSampleProvider(source)
            {
                ThresholdDb = -42.0f,  // Moderate gate threshold
                AttackMs = 5.0f,      // Fast attack for quick gating
                ReleaseMs = 100.0f,   // Medium release to avoid abrupt cuts
                HoldMs = 20.0f        // Short hold time
            };

            // Step 2: 5-band EQ for vocal enhancement
            var eq = new EqualizerSampleProvider(noiseGate, bandCount: 5)
            {
                // Band 0: ~100Hz (sub-bass/mud) - slight cut
                // Band 1: ~300Hz (bass) - slight cut
                // Band 2: ~1kHz (body) - slight boost for presence
                // Band 3: ~3kHz (clarity) - significant boost for vocal presence
                // Band 4: ~8kHz (air) - moderate boost for brightness
            };

            // Configure EQ bands
            eq.SetBandGain(0, -1.5f);  // Reduce muddiness
            eq.SetBandGain(1, -1.0f);  // Reduce boxiness
            eq.SetBandGain(2, 1.5f);   // Add body
            eq.SetBandGain(3, 3.0f);   // Boost clarity and presence
            eq.SetBandGain(4, 2.0f);   // Add air and brightness

            // Step 3: Compressor to even out vocal dynamics
            var compressor = new CompressorSampleProvider(eq)
            {
                ThresholdDb = -18.0f,  // Moderate threshold for vocals
                Ratio = 3.0f,         // 3:1 compression ratio
                AttackMs = 15.0f,     // Medium-fast attack
                ReleaseMs = 150.0f,  // Medium release
                MakeupGainDb = 3.0f   // Compensate for gain reduction
            };

            // Step 4: Limiter to prevent clipping
            var limiter = new LimiterSampleProvider(compressor)
            {
                CeilingDb = -0.3f,    // Stay 0.3dB below 0dBFS
                AttackMs = 1.0f,      // Very fast attack for brickwall limiting
                ReleaseMs = 50.0f     // Medium-fast release
            };

            return limiter;
        }

        /// <summary>
        /// Creates a Lo-Fi preset chain.
        ///
        /// This preset applies lo-fi/retro effects by:
        /// 1. Applying heavy noise gating to simulate tape hiss reduction
        /// 2. Using a 4-band EQ to reduce high frequencies and add lo-fi character
        /// 3. Applying bit crushing to reduce bit depth and sample rate effects
        /// 4. Adding a limiter to control output level
        ///
        /// Ideal for: lo-fi hip-hop, retro game audio, vintage sound design
        /// </summary>
        /// <param name="source">The input audio source</param>
        /// <returns>An ISampleProvider with the complete lo-fi effect chain applied</returns>
        public static ISampleProvider LoFi(ISampleProvider source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            // Step 1: Heavy noise gate to simulate tape recording characteristics
            var noiseGate = new NoiseGateSampleProvider(source)
            {
                ThresholdDb = -36.0f,  // Aggressive gating
                AttackMs = 10.0f,     // Medium-fast attack
                ReleaseMs = 200.0f,  // Slow release for tape-like character
                HoldMs = 50.0f       // Medium hold time
            };

            // Step 2: 4-band EQ for lo-fi character
            var eq = new EqualizerSampleProvider(noiseGate, bandCount: 4)
            {
                // Band 0: ~200Hz (bass) - slight boost for lo-fi warmth
                // Band 1: ~800Hz (midrange) - slight cut for lo-fi thickness
                // Band 2: ~3kHz (presence) - moderate cut for lo-fi muffle
                // Band 3: ~10kHz (highs) - heavy cut for tape hiss reduction
            };

            // Configure EQ bands
            eq.SetBandGain(0, 1.5f);   // Add lo-fi warmth
            eq.SetBandGain(1, -1.0f);  // Reduce midrange harshness
            eq.SetBandGain(2, -3.0f);  // Muffle presence slightly
            eq.SetBandGain(3, -12.0f); // Heavy high-cut for lo-fi effect

            // Step 3: Bit crusher for authentic lo-fi digital artifacts
            var bitCrusher = new BitCrusherSampleProvider(eq)
            {
                BitDepth = 8,     // 8-bit depth for authentic lo-fi
                HoldFactor = 4     // Downsample by factor of 4
            };

            // Step 4: Limiter to control output level
            var limiter = new LimiterSampleProvider(bitCrusher)
            {
                CeilingDb = -0.5f,    // Stay 0.5dB below 0dBFS
                AttackMs = 2.0f,      // Fast attack
                ReleaseMs = 100.0f    // Medium release
            };

            return limiter;
        }

        /// <summary>
        /// Creates a Podcast preset chain.
        ///
        /// This preset is optimized for spoken word content by:
        /// 1. Applying gentle noise gating to reduce background noise and breaths
        /// 2. Using a 3-band EQ to enhance speech intelligibility
        /// 3. Applying light compression to even out speech dynamics
        /// 4. Adding a limiter to prevent clipping
        ///
        /// Ideal for: podcasts, audiobooks, voiceovers, interviews
        /// </summary>
        /// <param name="source">The input audio source</param>
        /// <returns>An ISampleProvider with the complete podcast effect chain applied</returns>
        public static ISampleProvider Podcast(ISampleProvider source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            // Step 1: Gentle noise gate for speech
            var noiseGate = new NoiseGateSampleProvider(source)
            {
                ThresholdDb = -38.0f,  // Moderate threshold for speech
                AttackMs = 10.0f,     // Medium-fast attack
                ReleaseMs = 120.0f,  // Medium release
                HoldMs = 30.0f       // Medium hold time
            };

            // Step 2: 3-band EQ optimized for speech intelligibility
            var eq = new EqualizerSampleProvider(noiseGate, bandCount: 3)
            {
                // Band 0: ~150Hz (bass) - slight cut to reduce plosives and proximity effect
                // Band 1: ~2kHz (speech intelligibility) - significant boost for clarity
                // Band 2: ~8kHz (presence and air) - moderate boost for brightness
            };

            // Configure EQ bands
            eq.SetBandGain(0, -2.0f);  // Reduce muddiness
            eq.SetBandGain(1, 4.0f);   // Boost speech clarity
            eq.SetBandGain(2, 2.5f);   // Add air and presence

            // Step 3: Light compression for speech dynamics
            var compressor = new CompressorSampleProvider(eq)
            {
                ThresholdDb = -22.0f,  // Moderate threshold for speech
                Ratio = 2.5f,         // 2.5:1 compression ratio
                AttackMs = 20.0f,     // Medium attack for speech
                ReleaseMs = 200.0f,  // Medium release
                MakeupGainDb = 2.0f   // Compensate for gain reduction
            };

            // Step 4: Limiter to prevent clipping
            var limiter = new LimiterSampleProvider(compressor)
            {
                CeilingDb = -0.2f,    // Stay 0.2dB below 0dBFS
                AttackMs = 1.0f,      // Very fast attack
                ReleaseMs = 60.0f     // Medium-fast release
            };

            return limiter;
        }
    }
}
