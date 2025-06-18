using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AICommandPrompt.Services.GeminiApiDtos
{
    public class GeminiContent
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } // "user" or "model"

        [JsonPropertyName("parts")]
        public List<GeminiPart> Parts { get; set; }
    }
}
