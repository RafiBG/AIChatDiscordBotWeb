using OllamaSharp.Models.Chat;
using System.Collections.Concurrent;

namespace AIChatDiscordBotWeb.Services
{
    public class ChatMemoryService
    {
        private readonly ConcurrentDictionary<ulong, List<Message>> _memory = new();

        public void AddUserMessage(ulong userId, string username, string userMessage, string systemMessage)
        {
            if (!_memory.ContainsKey(userId))
            {
                _memory[userId] = new List<Message>
                {
                    new Message { Role = "system", Content = systemMessage }
                };
            }

            // Add user message with username included
            _memory[userId].Add(new Message { Role = "user", Content = $"{username}: {userMessage}" });

            // Keep history small (system + 10 last messages)
            if (_memory[userId].Count > 11)
            {
                _memory[userId].RemoveAt(1);
            }
        }

        public void AddAssistantMessage(ulong userId, string response)
        {
            if (_memory.ContainsKey(userId))
            {
                _memory[userId].Add(new Message { Role = "assistant", Content = response });
            }
        }

        public List<Message> GetUserMessages(ulong userId, string systemMessage)
        {
            if (!_memory.ContainsKey(userId))
            {
                _memory[userId] = new List<Message>
                {
                    new Message { Role = "system", Content = systemMessage }
                };
            }
            return _memory[userId];
        }

        public void ClearUserHistory(ulong userId)
        {
            _memory.TryRemove(userId, out _);
        }

        public void ResetAll()
        {
            _memory.Clear();
        }
    }
}
