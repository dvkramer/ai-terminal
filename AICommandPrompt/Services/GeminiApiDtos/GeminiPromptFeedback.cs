using System.Text.Json.Serialization;
using System.Collections.Generic; // Potentially for SafetyRatings

namespace AICommandPrompt.Services.GeminiApiDtos
{
    public class GeminiPromptFeedback
    {
        [JsonPropertyName("blockReason")]
        [JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string BlockReason { get; set; }

        // [JsonPropertyName("safetyRatings")]
        // public List<GeminiSafetyRating> SafetyRatings { get; set; } // For later
    }
}
