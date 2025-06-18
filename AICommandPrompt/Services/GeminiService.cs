using AICommandPrompt.Models; // For AgentActionResponse (method return type) and ChatMessage (method input type)
using AICommandPrompt.Services.GeminiApiDtos; // For the new DTOs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json; // For PostAsJsonAsync and ReadFromJsonAsync
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization; // Added for JsonIgnoreCondition
using System.Threading.Tasks;

namespace AICommandPrompt.Services
{
    public class GeminiService
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        private const string GeminiApiBaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/";
        private const string DefaultModelName = "gemini-1.5-flash-latest"; // Using "latest" for flash model

        public async Task<AgentActionResponse> GetAgentActionAsync(List<ChatMessage> conversationHistory, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return new AgentActionResponse { ActionType = "error", Error = "Gemini API Key was not provided to the service." };
            }
            if (conversationHistory == null || !conversationHistory.Any())
            {
                return new AgentActionResponse { ActionType = "error", Error = "Conversation history cannot be empty." };
            }

            var systemPromptText = @"You are an AI Agent. Your goal is to assist the user by executing PowerShell commands based on their requests in a chat conversation. You will analyze the conversation history, including the user's latest message and the output of any previously executed commands, to determine the next best action.

You must choose one of the following actions: 'speak', 'execute_powershell', or 'error'.
Structure your response *exclusively* in the following plain text format, ensuring each marker is on a new line:
ACTION: [action_type]
(If action_type is 'speak')
TEXT: [Your message to the user. This can be a question, information, or a status update.]
(If action_type is 'execute_powershell')
COMMAND: [The single, complete PowerShell command to execute.]
REASON: [Briefly, why you are executing this command in relation to the user's request or previous outputs.]
(If action_type is 'error')
TEXT: [Explanation of why you cannot proceed, e.g., request is ambiguous, unsafe, or previous command failed critically and you cannot recover.]

Provide *only this structured block* in your response. Do not include any other conversational text, greetings, or explanations outside of this structure.

When the conversation history includes a 'SystemExecutionResult:', this indicates the output of a command you previously decided to run. Analyze this output carefully (both standard output and any errors) to determine:
1. If the command was successful and achieved its part of the user's goal.
2. If any information from the output is needed for subsequent commands or to answer the user.
3. If the command failed and how to proceed (e.g., try a different command, or ask the user for clarification, or report an error if you cannot recover).

Always consider the overall user goal implied by the entire conversation history, especially when deciding on follow-up actions after a command execution. If a task requires multiple commands, plan and execute them one by one, using the output of the previous command to inform the next.
When generating PowerShell commands, aim for single, complete, and executable commands. Do not provide explanations or commentary within the COMMAND: field itself.";

            var geminiRequest = new GeminiGenerateContentRequest
            {
                SystemInstruction = new GeminiSystemInstruction
                {
                    Parts = new List<GeminiPart> { new GeminiPart { Text = systemPromptText } }
                },
                Contents = new List<GeminiContent>()
                // Optionally, configure tools if needed:
                // Tools = new List<GeminiTool> { new GeminiTool { GoogleSearch = new GoogleSearchTool() } }
            };

            // Convert ChatMessage history to GeminiContent format
            foreach (var message in conversationHistory)
            {
                string role = (message.Sender == MessageSender.User) ? "user" : "model";
                // Refine role for system/tool messages if Gemini API supports it explicitly, otherwise map to "model" or "user"
                // For now, simplifying: AI messages (including command outputs fed back) are "model" role.
                // The prompt itself instructs Gemini on how to interpret "SystemExecutionResult:" prefixes.
                geminiRequest.Contents.Add(new GeminiContent
                {
                    Role = role,
                    Parts = new List<GeminiPart> { new GeminiPart { Text = message.Text } }
                });
            }

            string apiUrl = $"{GeminiApiBaseUrl}{DefaultModelName}:generateContent?key={apiKey}";

            try
            {
                // HttpClient.DefaultRequestHeaders.Clear(); // Clear old headers if any, though static client might persist them
                // HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                // No explicit Authorization bearer token needed if using API key in URL for Gemini's generateContent

                HttpResponseMessage httpResponse = await HttpClient.PostAsJsonAsync(apiUrl, geminiRequest, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });

                if (httpResponse.IsSuccessStatusCode)
                {
                    GeminiGenerateContentResponse apiResponse = await httpResponse.Content.ReadFromJsonAsync<GeminiGenerateContentResponse>();
                    string aiRawText = apiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                    var agentResponse = new AgentActionResponse();
                    if (string.IsNullOrWhiteSpace(aiRawText))
                    {
                        agentResponse.ActionType = "error";
                        agentResponse.Error = "AI returned an empty or malformed response text.";
                    }
                    else
                    {
                        // Preprocess to remove "Thinking:" lines
                        var rawLines = aiRawText.Split(new[] { '\n', '\r' }, StringSplitOptions.None); // Keep all lines initially
                        var filteredLines = rawLines.Where(line => !line.TrimStart().StartsWith("Thinking:", StringComparison.OrdinalIgnoreCase)).ToList();
                        var processedAiRawText = string.Join("\n", filteredLines);

                        // Basic parsing logic uses the processed text
                        var lines = processedAiRawText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        string actionLine = lines.FirstOrDefault(l => l.StartsWith("ACTION:"))?.Substring("ACTION:".Length).Trim().ToLowerInvariant();

                        agentResponse.ActionType = actionLine;

                        switch (actionLine)
                        {
                            case "speak":
                                agentResponse.TextResponse = lines.FirstOrDefault(l => l.StartsWith("TEXT:"))?.Substring("TEXT:".Length).Trim() ?? aiRawText;
                                break;
                            case "execute_powershell":
                                agentResponse.PowerShellCommand = lines.FirstOrDefault(l => l.StartsWith("COMMAND:"))?.Substring("COMMAND:".Length).Trim();
                                agentResponse.Reasoning = lines.FirstOrDefault(l => l.StartsWith("REASON:"))?.Substring("REASON:".Length).Trim();
                                if (string.IsNullOrWhiteSpace(agentResponse.PowerShellCommand))
                                {
                                    agentResponse.ActionType = "error"; // Demote to error if command is missing
                                    agentResponse.Error = "AI chose to execute a command but the command was missing in the response.";
                                    agentResponse.TextResponse = aiRawText; // Provide raw text for debugging
                                }
                                break;
                            case "error":
                                agentResponse.TextResponse = lines.FirstOrDefault(l => l.StartsWith("TEXT:"))?.Substring("TEXT:".Length).Trim() ?? aiRawText;
                                agentResponse.Error = agentResponse.TextResponse; // Use TextResponse as Error detail
                                break;
                            default:
                                agentResponse.ActionType = "error"; // Or treat as "speak" with raw text
                                agentResponse.Error = $"AI response format was unexpected. Received raw text: {aiRawText.Substring(0, Math.Min(aiRawText.Length, 100))}";
                                agentResponse.TextResponse = aiRawText;
                                break;
                        }
                    }
                    return agentResponse;
                }
                else // Non-success HTTP status
                {
                    string errorJson = await httpResponse.Content.ReadAsStringAsync();
                    try
                    {
                        GeminiErrorContainer errorContainer = JsonSerializer.Deserialize<GeminiErrorContainer>(errorJson);
                        return new AgentActionResponse { ActionType = "error", Error = $"Gemini API Error: {errorContainer?.Error?.Message} (Code: {errorContainer?.Error?.Code}, Status: {errorContainer?.Error?.Status})" };
                    }
                    catch (JsonException) // If the error response itself isn't the expected JSON
                    {
                        return new AgentActionResponse { ActionType = "error", Error = $"Gemini API request failed: {httpResponse.StatusCode} - {httpResponse.ReasonPhrase}. Response: {errorJson.Substring(0, Math.Min(errorJson.Length, 200))}" };
                    }
                }
            }
            catch (HttpRequestException httpEx)
            {
                return new AgentActionResponse { ActionType = "error", Error = $"Network error connecting to Gemini API: {httpEx.Message}" };
            }
            catch (JsonException jsonEx) // Errors from request serialization or unexpected response deserialization
            {
                 return new AgentActionResponse { ActionType = "error", Error = $"JSON processing error: {jsonEx.Message}" };
            }
            catch (Exception ex)
            {
                return new AgentActionResponse { ActionType = "error", Error = $"An unexpected error occurred while contacting Gemini API: {ex.Message}" };
            }
        }
    }
}
