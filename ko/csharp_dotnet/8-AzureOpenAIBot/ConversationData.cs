using System.Collections.Generic;

namespace AzureOpenAIBot
{
    public class ConversationData
    {
        public List<string> UserPrompt { get; set; } = new List<string>();
        public List<string>BotPrompt { get; set; } = new List<string>();
    }
}
