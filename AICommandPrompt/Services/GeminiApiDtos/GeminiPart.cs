using System.Text.Json.Serialization;

namespace AICommandPrompt.Services.GeminiApiDtos
{
    public class GeminiPart
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        // For future use with inline data, not fully implemented in this step
        // [JsonPropertyName("inlineData")]
        // public GeminiInlineData InlineData { get; set; }
    }

    // public class GeminiInlineData // Placeholder for future
    // {
    //     [JsonPropertyName("mimeType")]
    //     public string MimeType { get; set; }
    //     [JsonPropertyName("data")]
    //     public string Data { get; set; } // Base64 encoded
    // }
}
