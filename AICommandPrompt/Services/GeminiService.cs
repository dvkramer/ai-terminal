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
        private const string DefaultModelName = "gemini-2.5-flash"; // Using "latest" for flash model

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

            var systemPromptText = @"You are an AI Agent. Your primary goal is to assist the user by executing PowerShell commands based on their requests in a chat conversation. You operate within a continuous, multi-step agentic loop.

Core Interaction Flow:
1. Analyze the user's request and the conversation history.
2. Decide on an action: 'speak', 'execute_powershell', or 'error'.
3. Structure your response *exclusively* in the plain text format detailed below.

Action Format:
IMPORTANT: Every single response you generate, without exception, MUST begin with ACTION: on the very first line, followed by the action type (e.g., 'speak', 'execute_powershell', or 'error'). This is the most critical instruction. Failure to start your response with ACTION: on the first line will cause a system failure.

Ensure each marker is on a new line. Provide *only this structured block* in your response. Do not include any other conversational text, greetings, or explanations outside of this structure.

ACTION: [action_type]

If action_type is 'speak':
TEXT: [Your message to the user. This can be a question, information, or a status update.]

If action_type is 'execute_powershell':
COMMAND: [The single, complete, and executable PowerShell command.]
REASON: [Briefly, why you are executing this command in relation to the user's request or previous outputs.]

If action_type is 'error':
TEXT: [Explanation of why you cannot proceed, e.g., request is ambiguous, unsafe, or previous command failed critically and you cannot recover.]

Agentic Loop and Multi-Step Tasks:
You are not limited to a single action per user request. Many user goals will require a sequence of commands.
When you issue an 'execute_powershell' action, the system will execute your command and then provide you with a 'SystemExecutionResult'. This result will include any standard output, errors, and a success/failure status.
You MUST analyze this 'SystemExecutionResult' carefully to determine the next step. Based on this analysis, you might:
    a. Issue another 'execute_powershell' command if the task requires further steps.
    b. 'speak' to the user with a summary, a result, or to ask for clarification.
    c. 'error' if the task cannot be completed or if a critical failure occurred.
Plan for tasks that may require multiple commands. Use the output of previous commands to inform subsequent ones.

User-Friendly Command Summaries:
After an 'execute_powershell' action and its 'SystemExecutionResult' are processed by you, your next action should typically be 'speak'. In this 'speak' action, provide a concise, user-friendly summary of what the command did or found (e.g., 'I have successfully created the project folder.' or 'The file you asked for is located at...'). This keeps the user informed.
However, you have the discretion to bypass this summary if the task clearly and logically requires an immediate follow-up 'execute_powershell' action. For example, if you run a command to check for a file's existence and the next logical step is to immediately create it if it doesn't exist, you might skip summarizing the 'file not found' output and proceed directly to the creation command.

General Guidelines:
- Always consider the overall user goal implied by the entire conversation history.
- When generating PowerShell commands, aim for single, complete, and executable commands. Do not provide explanations or commentary within the COMMAND: field itself.
- If a command fails, analyze the error message in the 'SystemExecutionResult' to decide if you can recover by trying a different command, modifying the command, or if you need to 'speak' to the user for clarification or 'error' out.
";

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

                        var lines = processedAiRawText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        string actionLine = lines.FirstOrDefault(l => l.StartsWith("ACTION:", StringComparison.OrdinalIgnoreCase))?.Substring("ACTION:".Length).Trim().ToLowerInvariant();

                        if (string.IsNullOrWhiteSpace(actionLine))
                        {
                            if (!string.IsNullOrWhiteSpace(processedAiRawText))
                            {
                                // No ACTION: found, but there's text. Treat as implicit speak.
                                agentResponse.ActionType = "speak";
                                agentResponse.TextResponse = processedAiRawText.Trim(); // Use the fully processed text
                            }
                            else
                            {
                                // No ACTION: and no text, this is genuinely an empty/malformed response from AI.
                                agentResponse.ActionType = "error_malformed_response";
                                agentResponse.Error = "AI_MALFORMED_EMPTY_RESPONSE";
                                agentResponse.TextResponse = aiRawText; // Provide original raw text for debugging
                            }
                        }
                        else // An ACTION: line was found
                        {
                            agentResponse.ActionType = actionLine;
                            switch (actionLine)
                            {
                                case "speak":
                                    {
                                        int textLineStartIndex = -1;
                                        for (int i = 0; i < lines.Length; i++)
                                        {
                                            if (lines[i].TrimStart().StartsWith("TEXT:", StringComparison.OrdinalIgnoreCase))
                                            {
                                                textLineStartIndex = i;
                                                break;
                                            }
                                        }

                                        if (textLineStartIndex != -1)
                                        {
                                            var sb = new StringBuilder();
                                            // Extract text from the "TEXT:" line itself
                                            string firstPartOfText = lines[textLineStartIndex].Substring(lines[textLineStartIndex].IndexOf("TEXT:", StringComparison.OrdinalIgnoreCase) + "TEXT:".Length).TrimStart();
                                            sb.Append(firstPartOfText); // Append first part, then newlines for subsequent full lines

                                            // Append subsequent lines until another major keyword or end of lines
                                            for (int i = textLineStartIndex + 1; i < lines.Length; i++)
                                            {
                                                string currentLineTrimmed = lines[i].TrimStart();
                                                if (currentLineTrimmed.StartsWith("ACTION:", StringComparison.OrdinalIgnoreCase) ||
                                                    currentLineTrimmed.StartsWith("COMMAND:", StringComparison.OrdinalIgnoreCase) ||
                                                    currentLineTrimmed.StartsWith("REASON:", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    break; // Stop if another major keyword is found
                                                }
                                                // Only append if it's not the first line of text to ensure correct newline placement
                                                if (sb.Length > 0 || !string.IsNullOrWhiteSpace(firstPartOfText) ) sb.AppendLine();
                                                sb.Append(lines[i]);
                                            }
                                            agentResponse.TextResponse = sb.ToString().TrimEnd('\r', '\n');
                                        }
                                        else // No "TEXT:" line found, but ACTION was "speak"
                                        {
                                            agentResponse.TextResponse = processedAiRawText.Trim();
                                        }

                                        // If TextResponse ended up being just "ACTION: speak" (e.g. implicit speak fell through or TEXT: was empty)
                                        if (agentResponse.TextResponse.Equals($"ACTION: {actionLine}", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(agentResponse.TextResponse))
                                        {
                                            agentResponse.TextResponse = $"AI initiated 'speak' action but provided no subsequent TEXT: content.";
                                        }
                                    }
                                    break;
                                case "execute_powershell":
                                    agentResponse.PowerShellCommand = lines.FirstOrDefault(l => l.StartsWith("COMMAND:", StringComparison.OrdinalIgnoreCase))?.Substring("COMMAND:".Length).Trim();
                                    agentResponse.Reasoning = lines.FirstOrDefault(l => l.StartsWith("REASON:", StringComparison.OrdinalIgnoreCase))?.Substring("REASON:".Length).Trim();
                                    // Multi-line for REASON (similar to TEXT) could be added if needed, but current prompt implies brief.
                                    if (string.IsNullOrWhiteSpace(agentResponse.PowerShellCommand))
                                    {
                                        agentResponse.ActionType = "error";
                                        agentResponse.Error = "AI chose to execute a command but the command was missing in the response.";
                                        agentResponse.TextResponse = processedAiRawText; // Provide processed text for debugging
                                    }
                                    break;
                                case "error":
                                    {
                                        int textLineStartIndex = -1;
                                        for (int i = 0; i < lines.Length; i++)
                                        {
                                            if (lines[i].TrimStart().StartsWith("TEXT:", StringComparison.OrdinalIgnoreCase))
                                            {
                                                textLineStartIndex = i;
                                                break;
                                            }
                                        }

                                        if (textLineStartIndex != -1)
                                        {
                                            var sb = new StringBuilder();
                                            string firstPartOfText = lines[textLineStartIndex].Substring(lines[textLineStartIndex].IndexOf("TEXT:", StringComparison.OrdinalIgnoreCase) + "TEXT:".Length).TrimStart();
                                            sb.Append(firstPartOfText);

                                            for (int i = textLineStartIndex + 1; i < lines.Length; i++)
                                            {
                                                string currentLineTrimmed = lines[i].TrimStart();
                                                if (currentLineTrimmed.StartsWith("ACTION:", StringComparison.OrdinalIgnoreCase) ||
                                                    currentLineTrimmed.StartsWith("COMMAND:", StringComparison.OrdinalIgnoreCase) ||
                                                    currentLineTrimmed.StartsWith("REASON:", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    break;
                                                }
                                                if (sb.Length > 0 || !string.IsNullOrWhiteSpace(firstPartOfText) ) sb.AppendLine();
                                                sb.Append(lines[i]);
                                            }
                                            agentResponse.TextResponse = sb.ToString().TrimEnd('\r', '\n');
                                        }
                                        else // No "TEXT:" line found, but ACTION was "error"
                                        {
                                            agentResponse.TextResponse = processedAiRawText.Trim();
                                        }

                                        // If TextResponse ended up being just "ACTION: error" or empty
                                        if (agentResponse.TextResponse.Equals($"ACTION: {actionLine}", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(agentResponse.TextResponse))
                                        {
                                            agentResponse.TextResponse = $"AI initiated 'error' action but provided no subsequent TEXT: content.";
                                        }
                                        agentResponse.Error = agentResponse.TextResponse;
                                    }
                                    break;
                                default: // Unknown action type specified
                                    agentResponse.ActionType = "error";
                                    agentResponse.Error = $"AI specified an unknown action type: '{actionLine}'. Raw response: {processedAiRawText.Substring(0, Math.Min(processedAiRawText.Length, 100))}";
                                    agentResponse.TextResponse = processedAiRawText;
                                    break;
                            }
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
