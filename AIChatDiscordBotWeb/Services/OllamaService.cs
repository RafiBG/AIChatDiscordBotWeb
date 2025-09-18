using AIChatDiscordBotWeb.Models;
using OllamaSharp;
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
            //http://localhost:11434
            _ollamaClient = new OllamaApiClient(new Uri($"http://localhost:{config.LOCAL_HOST}"));
            _model = config.MODEL;
        }

        public OllamaApiClient Client => _ollamaClient;
        public string Model => _model;
    }
}
