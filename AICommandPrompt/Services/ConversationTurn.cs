namespace AICommandPrompt.Services
{
    public class ConversationTurn
    {
        public string UserRequest { get; set; }
        public string AssistantResponse { get; set; } // The PowerShell command generated
    }
}
