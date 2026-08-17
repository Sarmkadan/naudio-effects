using System.Text.Json;
using System.Text.Json.Serialization;
using NAudioEffects;

namespace NAudioEffects
{
    public static class LimiterSampleProviderJsonExtensions
    {
        public static string ToJson(this LimiterSampleProvider value, bool indented = false)
        {
            var options = indented ? new JsonSerializerOptions { WriteIndented = true } : null;
            return JsonSerializer.Serialize(value, options);
        }

        public static LimiterSampleProvider? FromJson(string json)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);
            return JsonSerializer.Deserialize<LimiterSampleProvider>(json);
        }

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
