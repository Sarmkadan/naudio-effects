using System.Text.Json;
using System.Text.Json.Serialization;

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
            return JsonSerializer.Deserialize<LimiterSampleProvider>(json);
        }

        public static bool TryFromJson(string json, out LimiterSampleProvider? value)
        {
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
