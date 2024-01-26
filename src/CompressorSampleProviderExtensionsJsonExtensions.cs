using System;
using System.Text.Json;

namespace NAudioEffects
{
    /// <summary>
    /// Provides System.Text.Json based serialization helpers for <see cref="CompressorSampleProvider"/>,
    /// which is the type extended by <see cref="CompressorSampleProviderExtensions"/>.
    /// </summary>
    public static class CompressorSampleProviderExtensionsJsonExtensions
    {
        // Cached options with camel-case naming.
        private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Serializes the <see cref="CompressorSampleProvider"/> to a JSON string.
        /// </summary>
        /// <param name="value">The instance to serialize.</param>
        /// <param name="indented">If true, the output will be formatted with indentation.</param>
        /// <returns>A JSON representation of the instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
        public static string ToJson(this CompressorSampleProvider value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);

            var options = indented 
                ? new JsonSerializerOptions(_options) { WriteIndented = true } 
                : _options;
                
            return JsonSerializer.Serialize(value, options);
        }

        /// <summary>
        /// Deserializes a JSON string into a <see cref="CompressorSampleProvider"/> instance.
        /// </summary>
        /// <param name="json">The JSON string.</param>
        /// <returns>The deserialized instance.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
        /// <exception cref="JsonException">Thrown when the JSON is invalid.</exception>
        public static CompressorSampleProvider? FromJson(string json)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);

            return JsonSerializer.Deserialize<CompressorSampleProvider>(json, _options);
        }

        /// <summary>
        /// Attempts to deserialize a JSON string into a <see cref="CompressorSampleProvider"/> instance.
        /// </summary>
        /// <param name="json">The JSON string.</param>
        /// <param name="value">When the method returns, contains the deserialized instance if successful; otherwise <c>null</c>.</param>
        /// <returns><c>true</c> if deserialization succeeded; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
        public static bool TryFromJson(string json, out CompressorSampleProvider? value)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);

            try
            {
                value = JsonSerializer.Deserialize<CompressorSampleProvider>(json, _options);
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
