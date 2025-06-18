namespace AICommandPrompt.Services
{
    public class PowerShellExecutionResult
    {
        public string StandardOutput { get; set; }
        public string ErrorOutput { get; set; }
        public bool HadErrors { get; set; }
    }
}
