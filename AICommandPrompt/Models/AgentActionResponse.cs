namespace AICommandPrompt.Models
{
    public class AgentActionResponse
    {
        public string ActionType { get; set; } // e.g., "speak", "execute_powershell", "error"
        public string TextResponse { get; set; } // Content for AI to say
        public string PowerShellCommand { get; set; } // Command to execute
        public string Reasoning { get; set; } // AI's reasoning for the action (optional)
        public string Error { get; set; } // For errors from Gemini or internal agent logic
    }
}
