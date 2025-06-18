using System.Text.Json.Serialization;

namespace AICommandPrompt.Services.GeminiApiDtos
{
    public class GeminiTool
    {
        // As per example: "tools": [ { "googleSearch": {} } ]
        // This implies googleSearch is a property name.
        [JsonPropertyName("googleSearch")]
        [JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] // If no googleSearch tool is configured
        public GoogleSearchTool GoogleSearch { get; set; }
    }

    // Empty class as per example {}
    public class GoogleSearchTool
    {
    }
}
