using AIChatDiscordBotWeb.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Collections.Concurrent;

/// <summary>
/// Service to manage short chat memory for Slack users. It stores recent messages in memory.
/// It prunes old messages to keep the history size manageable. Messages are lost on restart.
/// </summary>

namespace AIChatDiscordBotWeb.Services
{
    public class SlackMemoryService
    {
        private readonly EnvConfig _config;
        private readonly ConcurrentDictionary<string, ChatHistory> _userHistories = new();
        private int MaxHistorySize = 10;

        public SlackMemoryService(EnvConfig config)
        {
            _config = config;
        }

        public void AddSlackMessage(string userId, ChatMessageContent message)
        {
            var history = _userHistories.GetOrAdd(userId, _ => new ChatHistory());
            history.Add(message);
            PruneHistory(userId);
        }

        public void AddSlackAssistantMessage(string userId, string response)
        {
            var history = _userHistories.GetOrAdd(userId, _ => new ChatHistory());
            history.Add(new ChatMessageContent(AuthorRole.Assistant, response));
            PruneHistory(userId);
        }

        public ChatHistory GetUserMessages(string userId, string systemMessage)
        {
            if (string.IsNullOrWhiteSpace(systemMessage))
            {
                systemMessage = _config.SYSTEM_MESSAGE;
            }

            var history = _userHistories.GetOrAdd(userId, _ => new ChatHistory());

            var historyWithSystemMessage = new ChatHistory();
            historyWithSystemMessage.AddSystemMessage(systemMessage);

            foreach (var message in history.Where(m => m.Role != AuthorRole.System))
            {
                historyWithSystemMessage.Add(message);
            }

            return historyWithSystemMessage;
        }

        private void PruneHistory(string userId)
        {
            if (_userHistories.TryGetValue(userId, out var history))
            {
                var userAndAssistantMessages = history
                    .Where(m => m.Role == AuthorRole.User || m.Role == AuthorRole.Assistant)
                    .ToList();

                if (userAndAssistantMessages.Count > MaxHistorySize)
                {
                    int messagesToRemove = userAndAssistantMessages.Count - MaxHistorySize;
                    var messageKeep = userAndAssistantMessages.Skip(messagesToRemove).ToList();

                    var newHistory = new ChatHistory();
                    // We re-add messages to the history object
                    foreach (var msg in messageKeep)
                    {
                        newHistory.Add(msg);
                    }

                    _userHistories[userId] = newHistory;
                }
            }
        }

        public void ClearUserHistory(string userId) => _userHistories.TryRemove(userId, out _);
    }
}