#nullable enable

using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NAudio.Wave;

namespace NAudioEffects
{
    /// <summary>
    /// Exception thrown when a preset file fails to load.
    /// </summary>
    public class PresetLoadException : Exception
    {
        /// <summary>
        /// Gets the name of the preset that failed to load.
        /// </summary>
        public string PresetName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PresetLoadException"/> class.
        /// </summary>
        /// <param name="presetName">The name of the preset that failed to load.</param>
        /// <param name="innerException">The exception that caused the preset load to fail.</param>
        public PresetLoadException(string presetName, Exception innerException)
            : base($"Failed to load preset '{presetName}'.", innerException)
        {
            PresetName = presetName;
        }
    }

    /// <summary>
    /// Factory methods for creating common audio effect chains and presets.
    /// These presets compose multiple effects (compressor, EQ, gate, limiter) over an ISampleProvider source.
    /// </summary>
    public class EffectChainPresets
    {
        private static readonly Action<ILogger, string, int, Exception?> PresetLoaded =
            LoggerMessage.Define<string, int>(
                LogLevel.Information,
                new EventId(1, nameof(PresetLoaded)),
                "Loaded preset {PresetName} with {EffectCount} effects.");

        private static readonly Action<ILogger, string, Exception?> PresetLoadFailed =
            LoggerMessage.Define<string>(
                LogLevel.Error,
                new EventId(2, nameof(PresetLoadFailed)),
                "Failed to load preset {PresetName}.");

        private readonly ILogger<EffectChainPresets> _logger;

        /// <summary>
        /// Initializes a preset factory.
        /// </summary>
        /// <param name="logger">The logger to use, or <see langword="null"/> to disable logging.</param>
        public EffectChainPresets(ILogger<EffectChainPresets>? logger = null)
        {
            _logger = logger ?? NullLogger<EffectChainPresets>.Instance;
        }

        /// <summary>
        /// Creates a Vocal Polish preset chain by loading settings from a JSON file.
        /// </summary>
        /// <remarks>
        /// This preset enhances vocal clarity and presence by:
        /// 1. Applying gentle noise gating to reduce background noise
        /// 2. Using a 5-band EQ to boost presence frequencies (2-5kHz) and reduce muddiness (100-200Hz)
        /// 3. Applying light compression to even out vocal dynamics
        /// 4. Adding a limiter to prevent clipping
        ///
        /// Ideal for: podcast vocals, voiceovers, singing vocals in mixes
        /// </remarks>
        /// <param name="source">The input audio source</param>
        /// <returns>An ISampleProvider with the complete effect chain applied</returns>
        /// <exception cref="PresetLoadException">Thrown when the preset file is missing, malformed, or references an unknown effect type.</exception>
        public ISampleProvider VocalPolish(ISampleProvider source)
        {
            return CreateVocalPolish(source, _logger);
        }

        /// <summary>
        /// Creates a Vocal Polish preset chain, optionally writing structured log entries.
        /// </summary>
        public static ISampleProvider VocalPolish(
            ISampleProvider source,
            ILogger<EffectChainPresets>? logger = null)
        {
            return CreateVocalPolish(source, logger ?? NullLogger<EffectChainPresets>.Instance);
        }

        private static ISampleProvider CreateVocalPolish(
            ISampleProvider source,
            ILogger<EffectChainPresets> logger)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            try
            {
                var preset = LoadPreset<VocalPolishPreset>("VocalPolish");
                var chain = BuildVocalPolishChain(source, preset);
                PresetLoaded(logger, "VocalPolish", 4, null);
                return chain;
            }
            catch (Exception ex)
            {
                var exception = ex as PresetLoadException ?? new PresetLoadException("VocalPolish", ex);
                PresetLoadFailed(logger, "VocalPolish", exception);
                throw exception;
            }
        }

        /// <summary>
        /// Creates a Lo-Fi preset chain by loading settings from a JSON file.
        /// </summary>
        /// <remarks>
        /// This preset applies lo-fi/retro effects by:
        /// 1. Applying heavy noise gating to simulate tape hiss reduction
        /// 2. Using a 4-band EQ to reduce high frequencies and add lo-fi character
        /// 3. Applying bit crushing to reduce bit depth and sample rate effects
        /// 4. Adding a limiter to control output level
        ///
        /// Ideal for: lo-fi hip-hop, retro game audio, vintage sound design
        /// </remarks>
        /// <param name="source">The input audio source</param>
        /// <returns>An ISampleProvider with the complete lo-fi effect chain applied</returns>
        /// <exception cref="PresetLoadException">Thrown when the preset file is missing, malformed, or references an unknown effect type.</exception>
        public ISampleProvider LoFi(ISampleProvider source)
        {
            return CreateLoFi(source, _logger);
        }

        /// <summary>
        /// Creates a Lo-Fi preset chain, optionally writing structured log entries.
        /// </summary>
        public static ISampleProvider LoFi(
            ISampleProvider source,
            ILogger<EffectChainPresets>? logger = null)
        {
            return CreateLoFi(source, logger ?? NullLogger<EffectChainPresets>.Instance);
        }

        private static ISampleProvider CreateLoFi(
            ISampleProvider source,
            ILogger<EffectChainPresets> logger)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            try
            {
                var preset = LoadPreset<LoFiPreset>("LoFi");
                var chain = BuildLoFiChain(source, preset);
                PresetLoaded(logger, "LoFi", 4, null);
                return chain;
            }
            catch (Exception ex)
            {
                var exception = ex as PresetLoadException ?? new PresetLoadException("LoFi", ex);
                PresetLoadFailed(logger, "LoFi", exception);
                throw exception;
            }
        }

        /// <summary>
        /// Creates a Podcast preset chain by loading settings from a JSON file.
        /// </summary>
        /// <remarks>
        /// This preset is optimized for spoken word content by:
        /// 1. Applying gentle noise gating to reduce background noise and breaths
        /// 2. Using a 3-band EQ to enhance speech intelligibility
        /// 3. Applying light compression to even out speech dynamics
        /// 4. Adding a limiter to prevent clipping
        ///
        /// Ideal for: podcasts, audiobooks, voiceovers, interviews
        /// </remarks>
        /// <param name="source">The input audio source</param>
        /// <returns>An ISampleProvider with the complete podcast effect chain applied</returns>
        /// <exception cref="PresetLoadException">Thrown when the preset file is missing, malformed, or references an unknown effect type.</exception>
        public ISampleProvider Podcast(ISampleProvider source)
        {
            return CreatePodcast(source, _logger);
        }

        /// <summary>
        /// Creates a Podcast preset chain, optionally writing structured log entries.
        /// </summary>
        public static ISampleProvider Podcast(
            ISampleProvider source,
            ILogger<EffectChainPresets>? logger = null)
        {
            return CreatePodcast(source, logger ?? NullLogger<EffectChainPresets>.Instance);
        }

        private static ISampleProvider CreatePodcast(
            ISampleProvider source,
            ILogger<EffectChainPresets> logger)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            try
            {
                var preset = LoadPreset<PodcastPreset>("Podcast");
                var chain = BuildPodcastChain(source, preset);
                PresetLoaded(logger, "Podcast", 4, null);
                return chain;
            }
            catch (Exception ex)
            {
                var exception = ex as PresetLoadException ?? new PresetLoadException("Podcast", ex);
                PresetLoadFailed(logger, "Podcast", exception);
                throw exception;
            }
        }

        #region Preset Loading

        private static TPreset LoadPreset<TPreset>(string presetName) where TPreset : class
        {
            var jsonFilePath = Path.Combine(AppContext.BaseDirectory, "presets", $"{presetName}.json");

            if (!File.Exists(jsonFilePath))
            {
                throw new FileNotFoundException($"Preset file not found: {jsonFilePath}", jsonFilePath);
            }

            try
            {
                var json = File.ReadAllText(jsonFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                return JsonSerializer.Deserialize<TPreset>(json, options)
                       ?? throw new InvalidOperationException($"Deserialized preset '{presetName}' is null.");
            }
            catch (IOException ex)
            {
                throw new PresetLoadException(presetName, ex);
            }
            catch (JsonException ex)
            {
                throw new PresetLoadException(presetName, ex);
            }
        }

        #endregion

        #region Vocal Polish Preset

        private static ISampleProvider BuildVocalPolishChain(ISampleProvider source, VocalPolishPreset preset)
        {
            // Step 1: Noise gate to reduce background noise and breaths below threshold
            var noiseGate = new NoiseGateSampleProvider(source)
            {
                ThresholdDb = preset.NoiseGate.ThresholdDb,
                AttackMs = preset.NoiseGate.AttackMs,
                ReleaseMs = preset.NoiseGate.ReleaseMs,
                HoldMs = preset.NoiseGate.HoldMs
            };

            // Step 2: 5-band EQ for vocal enhancement
            var eq = new EqualizerSampleProvider(noiseGate, bandCount: preset.Equalizer.BandCount)
            {
                // Band 0: ~100Hz (sub-bass/mud) - slight cut
                // Band 1: ~300Hz (bass) - slight cut
                // Band 2: ~1kHz (body) - slight boost for presence
                // Band 3: ~3kHz (clarity) - significant boost for vocal presence
                // Band 4: ~8kHz (air) - moderate boost for brightness
            };

            // Configure EQ bands
            for (int i = 0; i < preset.Equalizer.BandGains.Length && i < preset.Equalizer.BandCount; i++)
            {
                eq.SetBandGain(i, preset.Equalizer.BandGains[i]);
            }

            // Step 3: Compressor to even out vocal dynamics
            var compressor = new CompressorSampleProvider(eq)
            {
                ThresholdDb = preset.Compressor.ThresholdDb,
                Ratio = preset.Compressor.Ratio,
                AttackMs = preset.Compressor.AttackMs,
                ReleaseMs = preset.Compressor.ReleaseMs,
                MakeupGainDb = preset.Compressor.MakeupGainDb
            };

            // Step 4: Limiter to prevent clipping
            var limiter = new LimiterSampleProvider(compressor)
            {
                CeilingDb = preset.Limiter.CeilingDb,
                AttackMs = preset.Limiter.AttackMs,
                ReleaseMs = preset.Limiter.ReleaseMs
            };

            return limiter;
        }

        private class VocalPolishPreset
        {
            public NoiseGateSettings NoiseGate { get; set; } = null!;
            public EqualizerSettings Equalizer { get; set; } = null!;
            public CompressorSettings Compressor { get; set; } = null!;
            public LimiterSettings Limiter { get; set; } = null!;
        }

        #endregion

        #region Lo-Fi Preset

        private static ISampleProvider BuildLoFiChain(ISampleProvider source, LoFiPreset preset)
        {
            // Step 1: Heavy noise gate to simulate tape recording characteristics
            var noiseGate = new NoiseGateSampleProvider(source)
            {
                ThresholdDb = preset.NoiseGate.ThresholdDb,
                AttackMs = preset.NoiseGate.AttackMs,
                ReleaseMs = preset.NoiseGate.ReleaseMs,
                HoldMs = preset.NoiseGate.HoldMs
            };

            // Step 2: 4-band EQ for lo-fi character
            var eq = new EqualizerSampleProvider(noiseGate, bandCount: preset.Equalizer.BandCount)
            {
                // Band 0: ~200Hz (bass) - slight boost for lo-fi warmth
                // Band 1: ~800Hz (midrange) - slight cut for lo-fi thickness
                // Band 2: ~3kHz (presence) - moderate cut for lo-fi muffle
                // Band 3: ~10kHz (highs) - heavy cut for tape hiss reduction
            };

            // Configure EQ bands
            for (int i = 0; i < preset.Equalizer.BandGains.Length && i < preset.Equalizer.BandCount; i++)
            {
                eq.SetBandGain(i, preset.Equalizer.BandGains[i]);
            }

            // Step 3: Bit crusher for authentic lo-fi digital artifacts
            var bitCrusher = new BitCrusherSampleProvider(eq)
            {
                BitDepth = preset.BitCrusher.BitDepth,
                HoldFactor = preset.BitCrusher.HoldFactor
            };

            // Step 4: Limiter to control output level
            var limiter = new LimiterSampleProvider(bitCrusher)
            {
                CeilingDb = preset.Limiter.CeilingDb,
                AttackMs = preset.Limiter.AttackMs,
                ReleaseMs = preset.Limiter.ReleaseMs
            };

            return limiter;
        }

        private class LoFiPreset
        {
            public NoiseGateSettings NoiseGate { get; set; } = null!;
            public EqualizerSettings Equalizer { get; set; } = null!;
            public BitCrusherSettings BitCrusher { get; set; } = null!;
            public LimiterSettings Limiter { get; set; } = null!;
        }

        #endregion

        #region Podcast Preset

        private static ISampleProvider BuildPodcastChain(ISampleProvider source, PodcastPreset preset)
        {
            // Step 1: Gentle noise gate for speech
            var noiseGate = new NoiseGateSampleProvider(source)
            {
                ThresholdDb = preset.NoiseGate.ThresholdDb,
                AttackMs = preset.NoiseGate.AttackMs,
                ReleaseMs = preset.NoiseGate.ReleaseMs,
                HoldMs = preset.NoiseGate.HoldMs
            };

            // Step 2: 3-band EQ optimized for speech intelligibility
            var eq = new EqualizerSampleProvider(noiseGate, bandCount: preset.Equalizer.BandCount)
            {
                // Band 0: ~150Hz (bass) - slight cut to reduce plosives and proximity effect
                // Band 1: ~2kHz (speech intelligibility) - significant boost for clarity
                // Band 2: ~8kHz (presence and air) - moderate boost for brightness
            };

            // Configure EQ bands
            for (int i = 0; i < preset.Equalizer.BandGains.Length && i < preset.Equalizer.BandCount; i++)
            {
                eq.SetBandGain(i, preset.Equalizer.BandGains[i]);
            }

            // Step 3: Light compression for speech dynamics
            var compressor = new CompressorSampleProvider(eq)
            {
                ThresholdDb = preset.Compressor.ThresholdDb,
                Ratio = preset.Compressor.Ratio,
                AttackMs = preset.Compressor.AttackMs,
                ReleaseMs = preset.Compressor.ReleaseMs,
                MakeupGainDb = preset.Compressor.MakeupGainDb
            };

            // Step 4: Limiter to prevent clipping
            var limiter = new LimiterSampleProvider(compressor)
            {
                CeilingDb = preset.Limiter.CeilingDb,
                AttackMs = preset.Limiter.AttackMs,
                ReleaseMs = preset.Limiter.ReleaseMs
            };

            return limiter;
        }

        private class PodcastPreset
        {
            public NoiseGateSettings NoiseGate { get; set; } = null!;
            public EqualizerSettings Equalizer { get; set; } = null!;
            public CompressorSettings Compressor { get; set; } = null!;
            public LimiterSettings Limiter { get; set; } = null!;
        }

        #endregion

        #region Effect Settings DTOs

        private class NoiseGateSettings
        {
            public float ThresholdDb { get; set; }
            public float AttackMs { get; set; }
            public float ReleaseMs { get; set; }
            public float HoldMs { get; set; }
        }

        private class EqualizerSettings
        {
            public int BandCount { get; set; }
            public float[] BandGains { get; set; } = null!;
        }

        private class CompressorSettings
        {
            public float ThresholdDb { get; set; }
            public float Ratio { get; set; }
            public float AttackMs { get; set; }
            public float ReleaseMs { get; set; }
            public float MakeupGainDb { get; set; }
        }

        private class LimiterSettings
        {
            public float CeilingDb { get; set; }
            public float AttackMs { get; set; }
            public float ReleaseMs { get; set; }
        }

        private class BitCrusherSettings
        {
            public int BitDepth { get; set; }
            public int HoldFactor { get; set; }
        }

        #endregion
    }
}
