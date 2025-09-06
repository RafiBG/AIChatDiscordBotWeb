using AIChatDiscordBotWeb.Models;
using OllamaSharp;
using OllamaSharp.Models.Chat;
using System.Collections.Concurrent;

namespace AIChatDiscordBotWeb.Services
{
    public class OllamaService
    {
        private readonly OllamaApiClient _ollamaClient;
        private readonly string _model;
        private readonly ConcurrentDictionary<ulong, List<dynamic>> _memory = new();


        public OllamaService(EnvConfig config)
        {
            _ollamaClient = new OllamaApiClient(new Uri(config.LOCAL_HOST));
            _model = config.MODEL;
        }

        public OllamaApiClient Client => _ollamaClient;
        public string Model => _model;
    }
}
