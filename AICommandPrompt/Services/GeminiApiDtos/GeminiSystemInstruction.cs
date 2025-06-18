using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AICommandPrompt.Services.GeminiApiDtos
{
    public class GeminiSystemInstruction
    {
        [JsonPropertyName("parts")]
        public List<GeminiPart> Parts { get; set; }
    }
}
