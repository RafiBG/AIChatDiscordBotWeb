using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Collections.Concurrent;

namespace AIChatDiscordBotWeb.Services
{
    public class ChatMemoryService
    {
        private readonly ConcurrentDictionary<ulong, ChatHistory> _userHistories = new();

        private int MaxHistorySize = 10;

        public void AddMessage(ulong userId, ChatMessageContent message)
        {
            var history = _userHistories.GetOrAdd(userId, _ => new ChatHistory());

            history.Add(message);

            PruneHistory(userId);
        }

        //public void AddUserMessage(ulong userId, string username, string userMessage, string systemMessage)
        //{
        //    if (!_memory.ContainsKey(userId))
        //    {
        //        var history = new ChatHistory(systemMessage);
        //        _memory[userId] = history;
        //    }

        //    _memory[userId].AddUserMessage($"{username}: {userMessage}");

        //    // Keep history small (System message + 10 last user/assistant messages)
        //    const int maxMessages = 11;
        //    while (_memory[userId].Count > maxMessages)
        //    {
        //        // Remove the oldest non-system message (always at index 1 since system is 0)
        //        _memory[userId].RemoveAt(1);
        //    }
        //}

        public void AddAssistantMessage(ulong userId, string response)
        {
            var history = _userHistories.GetOrAdd(userId, _ => new ChatHistory());
            history.Add(new ChatMessageContent(AuthorRole.Assistant, response));
            PruneHistory(userId);
        }

        // Inside ChatMemoryService.cs

        public ChatHistory GetUserMessages(ulong userId, string systemMessage)
        {
            // Retrieve the user's conversation history (which contains User/Assistant messages)
            var history = _userHistories.GetOrAdd(userId, _ => new ChatHistory());

            // Create a new ChatHistory object with the System Message prepended.
            // This is the most crucial step for maintaining context/role.
            var historyWithSystemMessage = new ChatHistory(systemMessage); // This adds the system message

            // Append all existing User and Assistant messages from the stored history
            foreach (var message in history.Where(m => m.Role != AuthorRole.System))
            {
                historyWithSystemMessage.Add(message);
            }

            return historyWithSystemMessage;
        }

        private void PruneHistory(ulong userId)
        {
            if (_userHistories.TryGetValue(userId, out var history))
            {
                // 1. Find the System Message object in the current history (if it exists)
                var systemMessage = history.FirstOrDefault(m => m.Role == AuthorRole.System);

                // Only count User and Assistant messages to determine which to prune
                var userAndAssistantMessage = history
                    .Where(m => m.Role == AuthorRole.User || m.Role == AuthorRole.Assistant)
                    .ToList();

                if (userAndAssistantMessage.Count > MaxHistorySize)
                {
                    int messagesRemove = userAndAssistantMessage.Count - MaxHistorySize;

                    // Skip the oldest user/assistant messages
                    var messageKeep = userAndAssistantMessage
                        .Skip(messagesRemove)
                        .ToList();

                    var newHistory = new ChatHistory();

                    // 2. Always add the system message first if it was present
                    if (systemMessage != null)
                    {
                        newHistory.Add(systemMessage);
                    }

                    // 3. Add the kept user/assistant messages
                    foreach (var msg in messageKeep)
                    {
                        newHistory.Add(msg);
                    }

                    // Replace old history with the new pruned history
                    _userHistories[userId] = newHistory;
                }
            }
        }

        public void ClearUserHistory(ulong userId)
        {
            _userHistories.TryRemove(userId, out _);
        }

        public void ResetAll()
        {
            _userHistories.Clear();
        }
    }
}