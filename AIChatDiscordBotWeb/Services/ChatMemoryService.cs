using AIChatDiscordBotWeb.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Collections.Concurrent;

namespace AIChatDiscordBotWeb.Services
{

    /// <summary>
    /// Service to manage short chat memory for users. It stores recent messages in memory.
    /// It prunes old messages to keep the history size manageable. Messages are lost on restart.
    /// </summary>
    public class ChatMemoryService
    {

        private readonly EnvConfig _config;
        private int _maxHistorySize;

        public ChatMemoryService(EnvConfig config)
        {
            _config = config;
            _maxHistorySize = config.SHORT_MEMORY;
        }
        private readonly ConcurrentDictionary<ulong, ChatHistory> _userHistories = new();

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
            // Ensure systemMessage is valid
            if (string.IsNullOrWhiteSpace(systemMessage))
            {
                systemMessage = _config.SYSTEM_MESSAGE;
            }

            // Retrieve the user's conversation history (which contains User/Assistant messages)
            var history = _userHistories.GetOrAdd(userId, _ => new ChatHistory());

            var historyWithSystemMessage = new ChatHistory();
            historyWithSystemMessage.AddSystemMessage(systemMessage);

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

                if (userAndAssistantMessage.Count > _maxHistorySize)
                {
                    int messagesRemove = userAndAssistantMessage.Count - _maxHistorySize;

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