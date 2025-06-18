using System.Text.Json.Serialization;
using System.Collections.Generic; // Potentially for SafetyRatings etc.

namespace AICommandPrompt.Services.GeminiApiDtos
{
    public class GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiContent Content { get; set; }

        [JsonPropertyName("finishReason")]
        public string FinishReason { get; set; }

        // [JsonPropertyName("safetyRatings")]
        // public List<GeminiSafetyRating> SafetyRatings { get; set; } // For later

        // [JsonPropertyName("citationMetadata")]
        // public GeminiCitationMetadata CitationMetadata { get; set; } // For later

        [JsonPropertyName("groundingMetadata")]
        [JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public GeminiGroundingMetadata GroundingMetadata { get; set; }
    }

    public class GeminiGroundingMetadata // Based on example structure
    {
        [JsonPropertyName("searchEntryPoint")]
        [JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public GeminiSearchEntryPoint SearchEntryPoint { get; set; }

        // Assuming webSearchQueries might also be part of grounding metadata if needed
        // [JsonPropertyName("webSearchQueries")]
        // [JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        // public List<string> WebSearchQueries { get; set; }
    }

    public class GeminiSearchEntryPoint
    {
        [JsonPropertyName("renderedContent")]
        [JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string RenderedContent { get; set; }

        // [JsonPropertyName("sdkBlob")] // If needed for structured search results
        // [JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        // public string SdkBlob { get; set; }
    }
}
