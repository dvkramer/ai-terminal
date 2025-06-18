using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AICommandPrompt.Services.GeminiApiDtos
{
    public class GeminiGenerateContentRequest
    {
        [JsonPropertyName("contents")]
        public List<GeminiContent> Contents { get; set; }

        [JsonPropertyName("system_instruction")]
        [JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public GeminiSystemInstruction SystemInstruction { get; set; }

        [JsonPropertyName("tools")]
        [JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public List<GeminiTool> Tools { get; set; }

        // TODO: Add SafetySettings, GenerationConfig if needed later
        // [JsonPropertyName("safetySettings")]
        // public List<GeminiSafetySetting> SafetySettings { get; set; }

        // [JsonPropertyName("generationConfig")]
        // public GeminiGenerationConfig GenerationConfig { get; set; }
    }
}
