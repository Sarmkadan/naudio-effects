using System.Text.Json;

namespace NAudioEffects
{
	/// <summary>
	/// JSON serialization helpers for <see cref="CompressorSampleProvider"/>.
	/// </summary>
	public static class CompressorSampleProviderJsonExtensions
	{
		// Cached options with camel-case naming. WriteIndented is overridden per call when needed.
		private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = false
		};

		/// <summary>
		/// Serializes the <see cref="CompressorSampleProvider"/> to a JSON string.
		/// </summary>
		/// <param name="value">The instance to serialize.</param>
		/// <param name="indented">If true, the output will be formatted with indentation.</param>
		/// <returns>A JSON representation of the instance.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
		public static string ToJson(this CompressorSampleProvider value, bool indented = false)
		{
			ArgumentNullException.ThrowIfNull(value);

			// If indentation is requested, clone the cached options and enable indentation.
			var options = indented ? new JsonSerializerOptions(_options) { WriteIndented = true } : _options;
			return JsonSerializer.Serialize(value, options);
		}

		/// <summary>
		/// Deserializes a JSON string into a <see cref="CompressorSampleProvider"/> instance.
		/// </summary>
		/// <param name="json">The JSON string.</param>
		/// <returns>The deserialized instance, or <c>null</c> if the JSON does not represent a valid object.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="json"/> is <c>null</c>.</exception>
		public static CompressorSampleProvider? FromJson(string json)
		{
			ArgumentNullException.ThrowIfNull(json);

			return JsonSerializer.Deserialize<CompressorSampleProvider>(json, _options);
		}

		/// <summary>
		/// Attempts to deserialize a JSON string into a <see cref="CompressorSampleProvider"/> instance.
		/// </summary>
		/// <param name="json">The JSON string.</param>
		/// <param name="value">When the method returns, contains the deserialized instance if successful; otherwise <c>null</c>.</param>
		/// <returns><c>true</c> if deserialization succeeded; otherwise <c>false</c>.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="json"/> is <c>null</c>.</exception>
		public static bool TryFromJson(string json, out CompressorSampleProvider? value)
		{
			ArgumentNullException.ThrowIfNull(json);

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
