using System;
using System.Text.Json;

namespace NAudioEffects
{
    /// <summary>
    /// Provides System.Text.Json based serialization helpers for <see cref="SilenceDetector"/>.
    /// </summary>
    public static class SilenceDetectorJsonExtensions
    {
        // Cached options using camel‑case naming.
        private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Serializes the <see cref="SilenceDetector"/> instance to a JSON string.
        /// </summary>
        /// <param name="value">The instance to serialize.</param>
        /// <param name="indented">If <c>true</c>, the output JSON will be formatted with indentation.</param>
        /// <returns>A JSON representation of <paramref name="value"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
        public static string ToJson(this SilenceDetector value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);

            var options = indented
                ? new JsonSerializerOptions(_options) { WriteIndented = true }
                : _options;

            return JsonSerializer.Serialize(value, options);
        }

        /// <summary>
        /// Deserializes a JSON string into a <see cref="SilenceDetector"/> instance.
        /// </summary>
        /// <param name="json">The JSON string representing a <see cref="SilenceDetector"/>.</param>
        /// <returns>The deserialized <see cref="SilenceDetector"/>, or <c>null</c> if the JSON does not represent a value.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is <c>null</c> or empty.</exception>
        /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be mapped to <see cref="SilenceDetector"/>.</exception>
        public static SilenceDetector? FromJson(string json)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);
            return JsonSerializer.Deserialize<SilenceDetector>(json, _options);
        }

        /// <summary>
        /// Attempts to deserialize a JSON string into a <see cref="SilenceDetector"/> instance.
        /// </summary>
        /// <param name="json">The JSON string representing a <see cref="SilenceDetector"/>.</param>
        /// <param name="value">
        /// When this method returns, contains the deserialized <see cref="SilenceDetector"/> if the operation succeeded;
        /// otherwise, <c>null</c>.
        /// </param>
        /// <returns><c>true</c> if deserialization succeeded; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is <c>null</c> or empty.</exception>
        public static bool TryFromJson(string json, out SilenceDetector? value)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);

            try
            {
                value = JsonSerializer.Deserialize<SilenceDetector>(json, _options);
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
