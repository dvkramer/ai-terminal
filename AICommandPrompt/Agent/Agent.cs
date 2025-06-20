using AICommandPrompt.Models;
using AICommandPrompt.Services;
using System; // Required for string.IsNullOrWhiteSpace, DateTime
using System.Collections.Generic;
using System.Linq;
using System.Text; // Required for StringBuilder
using System.Threading.Tasks;

namespace AICommandPrompt.Agent
{
    public class Agent
    {
        private readonly GeminiService _geminiService;
        private readonly PowerShellService _powerShellService;
        private string _apiKey;

        // Internal conversation history for the agent's own reference
        private List<ChatMessage> _internalConversationHistory = new List<ChatMessage>();
        private const int MaxConversationHistoryToKeep = 20; // Max turns to keep in internal history (user + AI responses)

        public Agent(GeminiService geminiService, PowerShellService powerShellService)
        {
            _geminiService = geminiService;
            _powerShellService = powerShellService;
        }

        public void SetApiKey(string apiKey)
        {
            _apiKey = apiKey;
        }

        public void ClearInternalHistory()
        {
            _internalConversationHistory.Clear();
        }

        // This method now takes the latest user message and uses its internal history for context
        public async Task<List<ChatMessage>> ProcessUserMessageAsync(ChatMessage userMessage)
        {
            // Add current user message to internal history
            _internalConversationHistory.Add(userMessage);
            // Trim internal history if it's too long
            while(_internalConversationHistory.Count > MaxConversationHistoryToKeep)
            {
                _internalConversationHistory.RemoveAt(0);
            }

            var agentResponsesForUi = new List<ChatMessage>();

            if (string.IsNullOrEmpty(_apiKey))
            {
                var errorApiKeyMsg = new ChatMessage("Error: API Key not set for Agent. Please configure it in Settings.", MessageSender.AI, MessageDisplayType.ErrorText);
                agentResponsesForUi.Add(errorApiKeyMsg);
                // Do not add this specific UI error to internal history as it's a pre-condition failure, not part of AI interaction flow.
                return agentResponsesForUi;
            }

            int currentIteration = 0;
            const int maxIterations = 15;

            // Use a temporary list for the current processing cycle's history,
            // starting with a snapshot of the agent's internal history.
            // This list will be passed to Gemini and augmented with new AI/system messages within the loop.
            List<ChatMessage> currentProcessingHistory = new List<ChatMessage>(_internalConversationHistory);

            while (currentIteration < maxIterations)
            {
                currentIteration++;

                AgentActionResponse geminiDecision = await _geminiService.GetAgentActionAsync(currentProcessingHistory, _apiKey);

                // Handle potential errors from GeminiService itself (e.g. network, bad API key response from service)
                if (geminiDecision.ActionType == "error" && !string.IsNullOrWhiteSpace(geminiDecision.Error))
                {
                     var serviceErrorMsg = new ChatMessage(geminiDecision.Error, MessageSender.AI, MessageDisplayType.ErrorText);
                     agentResponsesForUi.Add(serviceErrorMsg);
                     _internalConversationHistory.Add(serviceErrorMsg); // Log this error to overall history
                     // currentProcessingHistory.Add(serviceErrorMsg); // Also add to current cycle's history
                     break; // Critical error from service, stop processing
                }


                switch (geminiDecision.ActionType?.ToLowerInvariant())
                {
                    case "speak":
                        var aiSpeakMessage = new ChatMessage(geminiDecision.TextResponse ?? "AI decided to speak but provided no text.", MessageSender.AI, MessageDisplayType.NormalText);
                        agentResponsesForUi.Add(aiSpeakMessage);
                        _internalConversationHistory.Add(aiSpeakMessage);
                        // currentProcessingHistory.Add(aiSpeakMessage); // No need to add to currentProcessingHistory, as "speak" ends the loop.
                        goto endLoop; // Exit loop as AI is speaking to user

                    case "execute_powershell":
                        if (string.IsNullOrWhiteSpace(geminiDecision.PowerShellCommand))
                        {
                            var errorNoCmdMsg = new ChatMessage("AI decided to execute a command but didn't provide one.", MessageSender.AI, MessageDisplayType.ErrorText);
                            agentResponsesForUi.Add(errorNoCmdMsg);
                            _internalConversationHistory.Add(errorNoCmdMsg);
                            // currentProcessingHistory.Add(errorNoCmdMsg);
                            goto endLoop; // Exit loop on this error
                        }

                        if (!string.IsNullOrWhiteSpace(geminiDecision.Reasoning))
                        {
                            var reasoningMsg = new ChatMessage($"Thinking: {geminiDecision.Reasoning}", MessageSender.AI, MessageDisplayType.AIStatus);
                            agentResponsesForUi.Add(reasoningMsg);
                            _internalConversationHistory.Add(reasoningMsg);
                            currentProcessingHistory.Add(reasoningMsg); // Add reasoning to context for next potential iteration
                        }

                        var commandToRun = geminiDecision.PowerShellCommand;
                        var commandLogMsg = new ChatMessage($"Executing: {commandToRun}", MessageSender.AI, MessageDisplayType.CommandLog);
                        agentResponsesForUi.Add(commandLogMsg);
                        _internalConversationHistory.Add(commandLogMsg);
                        currentProcessingHistory.Add(commandLogMsg); // Add command to context

                        PowerShellExecutionResult psResult = await _powerShellService.ExecuteCommandAsync(commandToRun);

                        string uiOutputText;
                        MessageDisplayType uiOutputDisplayType = MessageDisplayType.CommandLog;
                        ChatMessage historyMessageForGemini;

                        if (psResult.HadErrors)
                        {
                            uiOutputText = $"Execution of '{commandToRun}' FAILED.\nStandard Output:\n{psResult.StandardOutput}\nErrors:\n{psResult.ErrorOutput}";
                            uiOutputDisplayType = MessageDisplayType.ErrorText;
                            // For Gemini, be very explicit and include both streams clearly labeled
                            historyMessageForGemini = new ChatMessage($"Execution of '{commandToRun}' FAILED. Standard Output was: \"{psResult.StandardOutput?.Trim()}\". Error Output was: \"{psResult.ErrorOutput?.Trim()}\".", MessageSender.AI, MessageDisplayType.ErrorText);
                        }
                        else
                        {
                            uiOutputText = $"Output for '{commandToRun}':\n{psResult.StandardOutput}";
                             if (string.IsNullOrWhiteSpace(psResult.StandardOutput))
                            {
                                uiOutputText = $"Command '{commandToRun}' executed successfully with no output.";
                            }
                            historyMessageForGemini = new ChatMessage($"Execution of '{commandToRun}' SUCCEEDED. Output: \"{psResult.StandardOutput?.Trim()}\"", MessageSender.AI, MessageDisplayType.CommandLog);
                        }

                        // Message for UI display
                        var uiOutputMsg = new ChatMessage(uiOutputText, MessageSender.AI, uiOutputDisplayType);
                        agentResponsesForUi.Add(uiOutputMsg);

                        // Add the potentially more detailed/structured message to internal and processing history
                        _internalConversationHistory.Add(historyMessageForGemini);
                        currentProcessingHistory.Add(historyMessageForGemini);

                        if (currentIteration >= maxIterations) {
                            var maxIterationMsg = new ChatMessage("Reached max automated steps for this request. Please provide further instructions if needed.", MessageSender.AI, MessageDisplayType.AIStatus);
                            agentResponsesForUi.Add(maxIterationMsg);
                            _internalConversationHistory.Add(maxIterationMsg);
                            goto endLoop; // Exit loop
                        }
                        // Loop continues: output is now part of currentProcessingHistory for the next GetAgentActionAsync call
                        break; // Breaks switch, continues while loop

                    case "error_malformed_response":
                        // This case is specifically for when GeminiService returns ActionType="error_malformed_response"
                        // and Error="AI_MALFORMED_EMPTY_RESPONSE", indicating the AI's response was unusable.
                        // geminiDecision.TextResponse here contains the original aiRawText from GeminiService.

                        var uiMalformedMsg = new ChatMessage($"AI response was malformed or empty. The AI will attempt to retry. Original raw response: {geminiDecision.TextResponse}", MessageSender.AI, MessageDisplayType.ErrorText);
                        agentResponsesForUi.Add(uiMalformedMsg);
                        _internalConversationHistory.Add(uiMalformedMsg); // Log this to internal history for completeness.
                        currentProcessingHistory.Add(uiMalformedMsg); // Add to current processing history for AI's context for retry

                        var aiFeedbackMsg = new ChatMessage("SystemFeedback: Your previous response was empty or malformed. This is a critical issue. Ensure your *very next* response strictly adheres to the required format, starting with ACTION: on the first line, followed by necessary fields like TEXT:, COMMAND:, or REASON:. Do not include any other conversational text or apologies before the ACTION: line.", MessageSender.AI, MessageDisplayType.AIStatus);
                        _internalConversationHistory.Add(aiFeedbackMsg);
                        currentProcessingHistory.Add(aiFeedbackMsg); // Add to current processing history for AI's next attempt

                        if (currentIteration >= maxIterations) {
                            var maxIterationErrorMsg = new ChatMessage("Reached max automated steps after AI response error. AI failed to provide a valid response. Please provide further instructions or try a different approach.", MessageSender.AI, MessageDisplayType.ErrorText);
                            agentResponsesForUi.Add(maxIterationErrorMsg);
                            _internalConversationHistory.Add(maxIterationErrorMsg);
                            goto endLoop; // Exit loop if max iterations reached
                        }
                        break; // Breaks switch, continues while loop to let AI retry

                    case "error": // For other errors explicitly decided by the AI in its response, or other service errors not caught by the initial check.
                         string geminiErrorText = string.IsNullOrWhiteSpace(geminiDecision.TextResponse) ? geminiDecision.Error : geminiDecision.TextResponse;
                         if (string.IsNullOrWhiteSpace(geminiErrorText)) geminiErrorText = "AI reported an unspecified error.";
                         var geminiActionErrorMsg = new ChatMessage(geminiErrorText, MessageSender.AI, MessageDisplayType.ErrorText);
                         agentResponsesForUi.Add(geminiActionErrorMsg);
                         _internalConversationHistory.Add(geminiActionErrorMsg);
                         // currentProcessingHistory.Add(geminiActionErrorMsg); // No need to add to current processing, loop terminates
                         goto endLoop; // Exit loop on general AI error

                    default: // Includes unknown action types or if ActionType is null after GeminiService processing
                        string unknownActionErrorText = $"AI system returned an unknown or unspecified action type ('{geminiDecision.ActionType}').";
                        if (!string.IsNullOrWhiteSpace(geminiDecision.TextResponse)) // This might be the original aiRawText if parsing failed at GeminiService
                             unknownActionErrorText += $" AI Response content: {geminiDecision.TextResponse}";
                        else if (!string.IsNullOrWhiteSpace(geminiDecision.Error)) // This might be an error code like AI_MALFORMED_EMPTY_RESPONSE if it somehow fell through
                             unknownActionErrorText += $" Details: {geminiDecision.Error}";

                        var unknownActionMsg = new ChatMessage(unknownActionErrorText, MessageSender.AI, MessageDisplayType.ErrorText);
                        agentResponsesForUi.Add(unknownActionMsg);
                        _internalConversationHistory.Add(unknownActionMsg);
                        // currentProcessingHistory.Add(unknownActionMsg); // No need to add to current processing, loop terminates
                        goto endLoop; // Exit loop
                }
            }
            endLoop:; // Label to break out of the while loop
            return agentResponsesForUi;
        }
    }
}
