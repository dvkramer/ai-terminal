using System.Text.Json.Serialization;

namespace AICommandPrompt.Services.GeminiApiDtos
{
    // Main container for the error response from Gemini API (when HTTP status is not 200 OK but body is JSON error)
    public class GeminiErrorContainer
    {
        [JsonPropertyName("error")]
        public GeminiErrorDetail Error { get; set; }
    }

    public class GeminiErrorDetail
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }
        // Details could be an object or array, but often not present or varies.
        // For simplicity, not including "details" here unless a specific structure is known and needed.
        // [JsonPropertyName("details")]
        // public object Details { get; set; }
    }
}
