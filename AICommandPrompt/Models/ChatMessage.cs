using System;

namespace AICommandPrompt.Models
{
    public enum MessageSender { User, AI }
    public enum MessageDisplayType { NormalText, CommandLog, ErrorText, AIStatus } // AIStatus for general AI messages, CommandLog for actual command logs

    public class ChatMessage
    {
        public string Text { get; set; }
        public MessageSender Sender { get; set; }
        public MessageDisplayType DisplayType { get; set; }
        public DateTime Timestamp { get; set; }

        // Constructor for simple text messages
        public ChatMessage(string text, MessageSender sender, MessageDisplayType displayType = MessageDisplayType.NormalText)
        {
            Text = text;
            Sender = sender;
            DisplayType = displayType;
            Timestamp = DateTime.Now;
        }
    }
}
