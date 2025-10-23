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

        public void AddAssistantMessage(ulong userId, string response)
        {
            var history = _userHistories.GetOrAdd(userId, _ => new ChatHistory());
            history.Add(new ChatMessageContent(AuthorRole.Assistant, response));
            PruneHistory(userId);
        }

        public ChatHistory GetUserMessages(ulong userId, string systemMessage)
        {
            // Retrieve the user's conversation history (which contains User/Assistant messages)
            var history = _userHistories.GetOrAdd(userId, _ => new ChatHistory());

            var historyWithSystemMessage = new ChatHistory(systemMessage);

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

                    // Add the system message first if it was present
                    if (systemMessage != null)
                    {
                        newHistory.Add(systemMessage);
                    }

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