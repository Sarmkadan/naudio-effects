using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using Xunit;

namespace NAudioEffects.Tests
{
    public class EffectChainPresetsTests
    {
        private readonly ConstantSampleProvider _testSource;

        public EffectChainPresetsTests()
        {
            _testSource = new ConstantSampleProvider(0.5f, 2);
        }

        [Fact]
        public void VocalPolish_ThrowsPresetLoadException_WhenPresetFileMissing()
        {
            // Arrange - temporarily rename the preset file
            var presetPath = Path.Combine(AppContext.BaseDirectory, "presets", "VocalPolish.json");
            var backupPath = presetPath + ".bak";

            if (File.Exists(presetPath))
            {
                File.Move(presetPath, backupPath);
            }

            try
            {
                // Act & Assert
                Assert.Throws<PresetLoadException>(() => EffectChainPresets.VocalPolish(_testSource));
            }
            finally
            {
                // Cleanup - restore the file
                if (File.Exists(backupPath))
                {
                    if (File.Exists(presetPath))
                    {
                        File.Delete(presetPath);
                    }
                    File.Move(backupPath, presetPath);
                }
            }
        }

        [Fact]
        public void VocalPolish_ThrowsPresetLoadException_WhenPresetFileMalformed()
        {
            // Arrange - temporarily corrupt the preset file
            var presetPath = Path.Combine(AppContext.BaseDirectory, "presets", "VocalPolish.json");
            var backupContent = File.ReadAllText(presetPath);

            try
            {
                File.WriteAllText(presetPath, "{ invalid json }");

                // Act & Assert
                Assert.Throws<PresetLoadException>(() => EffectChainPresets.VocalPolish(_testSource));
            }
            finally
            {
                // Cleanup - restore the file
                File.WriteAllText(presetPath, backupContent);
            }
        }

        [Fact]
        public void LoFi_ThrowsPresetLoadException_WhenPresetFileMissing()
        {
            // Arrange - temporarily rename the preset file
            var presetPath = Path.Combine(AppContext.BaseDirectory, "presets", "LoFi.json");
            var backupPath = presetPath + ".bak";

            if (File.Exists(presetPath))
            {
                File.Move(presetPath, backupPath);
            }

            try
            {
                // Act & Assert
                Assert.Throws<PresetLoadException>(() => EffectChainPresets.LoFi(_testSource));
            }
            finally
            {
                // Cleanup - restore the file
                if (File.Exists(backupPath))
                {
                    if (File.Exists(presetPath))
                    {
                        File.Delete(presetPath);
                    }
                    File.Move(backupPath, presetPath);
                }
            }
        }

        [Fact]
        public void LoFi_ThrowsPresetLoadException_WhenPresetFileMalformed()
        {
            // Arrange - temporarily corrupt the preset file
            var presetPath = Path.Combine(AppContext.BaseDirectory, "presets", "LoFi.json");
            var backupContent = File.ReadAllText(presetPath);

            try
            {
                File.WriteAllText(presetPath, "{ invalid json }");

                // Act & Assert
                Assert.Throws<PresetLoadException>(() => EffectChainPresets.LoFi(_testSource));
            }
            finally
            {
                // Cleanup - restore the file
                File.WriteAllText(presetPath, backupContent);
            }
        }

        [Fact]
        public void Podcast_ThrowsPresetLoadException_WhenPresetFileMissing()
        {
            // Arrange - temporarily rename the preset file
            var presetPath = Path.Combine(AppContext.BaseDirectory, "presets", "Podcast.json");
            var backupPath = presetPath + ".bak";

            if (File.Exists(presetPath))
            {
                File.Move(presetPath, backupPath);
            }

            try
            {
                // Act & Assert
                Assert.Throws<PresetLoadException>(() => EffectChainPresets.Podcast(_testSource));
            }
            finally
            {
                // Cleanup - restore the file
                if (File.Exists(backupPath))
                {
                    if (File.Exists(presetPath))
                    {
                        File.Delete(presetPath);
                    }
                    File.Move(backupPath, presetPath);
                }
            }
        }

        [Fact]
        public void Podcast_ThrowsPresetLoadException_WhenPresetFileMalformed()
        {
            // Arrange - temporarily corrupt the preset file
            var presetPath = Path.Combine(AppContext.BaseDirectory, "presets", "Podcast.json");
            var backupContent = File.ReadAllText(presetPath);

            try
            {
                File.WriteAllText(presetPath, "{ invalid json }");

                // Act & Assert
                Assert.Throws<PresetLoadException>(() => EffectChainPresets.Podcast(_testSource));
            }
            finally
            {
                // Cleanup - restore the file
                File.WriteAllText(presetPath, backupContent);
            }
        }

        [Fact]
        public void VocalPolish_ReturnsNotNull_WhenPresetFileExists()
        {
            // Act
            var result = EffectChainPresets.VocalPolish(_testSource);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void LoFi_ReturnsNotNull_WhenPresetFileExists()
        {
            // Act
            var result = EffectChainPresets.LoFi(_testSource);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void Podcast_ReturnsNotNull_WhenPresetFileExists()
        {
            // Act
            var result = EffectChainPresets.Podcast(_testSource);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void VocalPolish_LogsPresetNameAndEffectCount_WhenLoadSucceeds()
        {
            var logger = new FakeLogger<EffectChainPresets>();
            var presets = new EffectChainPresets(logger);

            var result = presets.VocalPolish(_testSource);

            Assert.NotNull(result);
            var entry = Assert.Single(logger.Entries);
            Assert.Equal(LogLevel.Information, entry.Level);
            Assert.Equal("VocalPolish", entry.State["PresetName"]);
            Assert.Equal(4, entry.State["EffectCount"]);
            Assert.Null(entry.Exception);
        }

        [Fact]
        public void VocalPolish_LogsException_WhenLoadFails()
        {
            var presetPath = Path.Combine(AppContext.BaseDirectory, "presets", "VocalPolish.json");
            var backupContent = File.ReadAllText(presetPath);
            var logger = new FakeLogger<EffectChainPresets>();
            var presets = new EffectChainPresets(logger);

            try
            {
                File.WriteAllText(presetPath, "{ invalid json }");

                var exception = Assert.Throws<PresetLoadException>(() => presets.VocalPolish(_testSource));

                var entry = Assert.Single(logger.Entries);
                Assert.Equal(LogLevel.Error, entry.Level);
                Assert.Equal("VocalPolish", entry.State["PresetName"]);
                Assert.Same(exception, entry.Exception);
            }
            finally
            {
                File.WriteAllText(presetPath, backupContent);
            }
        }

        [Fact]
        public void VocalPolish_ThrowsArgumentNullException_WhenSourceIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => EffectChainPresets.VocalPolish(null));
        }

        [Fact]
        public void LoFi_ThrowsArgumentNullException_WhenSourceIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => EffectChainPresets.LoFi(null));
        }

        [Fact]
        public void Podcast_ThrowsArgumentNullException_WhenSourceIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => EffectChainPresets.Podcast(null));
        }

        // Simple test stub for ISampleProvider
        private class ConstantSampleProvider : ISampleProvider
        {
            private readonly float _value;
            private readonly int _channels;

            public ConstantSampleProvider(float value, int channels = 1)
            {
                _value = value;
                _channels = channels;
            }

            public WaveFormat WaveFormat => new WaveFormat(44100, 32, _channels);

            public int Read(float[] buffer, int offset, int count)
            {
                for (int i = 0; i < count; i++)
                {
                    buffer[offset + i] = _value;
                }
                return count;
            }
        }

        private sealed class FakeLogger<T> : ILogger<T>
        {
            public List<LogEntry> Entries { get; } = new List<LogEntry>();

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                var structuredState = ((IEnumerable<KeyValuePair<string, object?>>)(object)state!)
                    .ToDictionary(item => item.Key, item => item.Value);
                Entries.Add(new LogEntry(logLevel, structuredState, exception));
            }
        }

        private sealed record LogEntry(
            LogLevel Level,
            IReadOnlyDictionary<string, object?> State,
            Exception? Exception);
    }
}
