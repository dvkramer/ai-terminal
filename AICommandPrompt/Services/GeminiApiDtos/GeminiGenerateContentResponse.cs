using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AICommandPrompt.Services.GeminiApiDtos
{
    public class GeminiGenerateContentResponse
    {
        [JsonPropertyName("candidates")]
        public List<GeminiCandidate> Candidates { get; set; }

        [JsonPropertyName("promptFeedback")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GeminiPromptFeedback PromptFeedback { get; set; }
    }
}
