using System;
using System.Text.Json;

namespace NAudioEffects
{
    /// <summary>
    /// Provides System.Text.Json based serialization helpers for <see cref="EqualizerSampleProvider"/>.
    /// </summary>
    public static class EqualizerSampleProviderJsonExtensions
    {
        // Cached options using camel-case naming.
        private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Serializes the <see cref="EqualizerSampleProvider"/> instance to a JSON string.
        /// </summary>
        /// <param name="value">The instance to serialize.</param>
        /// <param name="indented">If <c>true</c>, the output JSON will be formatted with indentation.</param>
        /// <returns>A JSON representation of <paramref name="value"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
        public static string ToJson(this EqualizerSampleProvider value, bool indented = false)
            => JsonSerializer.Serialize(value, indented ? new JsonSerializerOptions(_options) { WriteIndented = true } : _options);

        /// <summary>
        /// Deserializes a JSON string into a <see cref="EqualizerSampleProvider"/> instance.
        /// </summary>
        /// <param name="json">The JSON string representing a <see cref="EqualizerSampleProvider"/>.</param>
        /// <returns>The deserialized <see cref="EqualizerSampleProvider"/>, or <c>null</c> if the JSON does not represent a value.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is <c>null</c> or empty.</exception>
        /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be mapped to <see cref="EqualizerSampleProvider"/>.</exception>
        public static EqualizerSampleProvider? FromJson(string json)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);
            return JsonSerializer.Deserialize<EqualizerSampleProvider>(json, _options);
        }

        /// <summary>
        /// Attempts to deserialize a JSON string into a <see cref="EqualizerSampleProvider"/> instance.
        /// </summary>
        /// <param name="json">The JSON string representing a <see cref="EqualizerSampleProvider"/>.</param>
        /// <param name="value">
        /// When this method returns, contains the deserialized <see cref="EqualizerSampleProvider"/> if the operation succeeded;
        /// otherwise, <c>null</c>.
        /// </param>
        /// <returns><c>true</c> if deserialization succeeded; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is <c>null</c> or empty.</exception>
        public static bool TryFromJson(string json, out EqualizerSampleProvider? value)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);

            try
            {
                value = JsonSerializer.Deserialize<EqualizerSampleProvider>(json, _options);
                return true;
            }
            catch (JsonException)
            {
                value = null;
                return false;
            }
        }
    }
}
