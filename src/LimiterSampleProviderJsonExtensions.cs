using System.Text.Json;
using System.Text.Json.Serialization;
using NAudioEffects;

namespace NAudioEffects
{
    /// <summary>
    /// Provides extension methods for serializing and deserializing <see cref="LimiterSampleProvider"/> to and from JSON.
    /// </summary>
    public static class LimiterSampleProviderJsonExtensions
    {
        /// <summary>
        /// Serializes the LimiterSampleProvider to a JSON string.
        /// </summary>
        /// <param name="value">The LimiterSampleProvider to serialize.</param>
        /// <param name="indented">If true, the JSON will be indented for readability.</param>
        /// <returns>A JSON string representation of the LimiterSampleProvider.</returns>
        public static string ToJson(this LimiterSampleProvider value, bool indented = false)
        {
            var options = indented ? new JsonSerializerOptions { WriteIndented = true } : null;
            return JsonSerializer.Serialize(value, options);
        }

        /// <summary>
        /// Deserializes a JSON string to a LimiterSampleProvider instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>A LimiterSampleProvider instance, or null if the JSON is invalid.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="json"/> is null or empty.</exception>
        public static LimiterSampleProvider? FromJson(string json)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);
            return JsonSerializer.Deserialize<LimiterSampleProvider>(json);
        }

        /// <summary>
        /// Attempts to deserialize a JSON string to a LimiterSampleProvider instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">When this method returns, contains the LimiterSampleProvider instance if the JSON was valid, otherwise null.</param>
        /// <returns>true if the JSON was successfully deserialized; otherwise, false.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="json"/> is null or empty.</exception>
        public static bool TryFromJson(string json, out LimiterSampleProvider? value)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);
            try
            {
                value = FromJson(json);
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
