using AIChatDiscordBotWeb.Models;
using AIChatDiscordBotWeb.Tools;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AIChatDiscordBotWeb.Services
{
    /// <summary>
    /// It handles the setup and configuration of the Semantic Kernel,
    /// The connection to the Ollama local LLM server, and the registration of various tools
    ///</summary>>
    public class AIConnectionService
    {
        private readonly Kernel _kernel;
        private readonly string _model;
        private readonly KernelMemoryService _memoryService;

        public IKernelMemory Memory => _memoryService.RawMemory;

        // Accept KernelMemoryService as a dependency instead of building memory here
        public AIConnectionService(EnvConfig config, KernelMemoryService memoryService)
        {
            _model = config.MODEL;
            //string ollamaUrl = $"http://localhost:{config.LOCAL_HOST}"; //old one that i used only to enter the numbers to connect server
            string ollamaUrl = config.LOCAL_HOST;
            _memoryService = memoryService;

            // Configure HttpClient for long timeouts (if needed)
            var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5) // Adjust: 3-10 mins for large models
            };

            var builder = Kernel.CreateBuilder();

            // To OpenAI connector with Ollama's OpenAI-compatible endpoint.
            // This works with Ollama same PC or remote server in the same network.
            // Also will probably work with ChatGPT, Gemini and other OpenAI-compatible endpoints.
            builder.AddOpenAIChatCompletion(
                modelId: _model,
                apiKey: config.API_KEY, // Required but ignored by Ollama
                endpoint: new Uri(ollamaUrl),
                httpClient: httpClient
            );
            //// Old Ollama connector but it still works [25.12.2025/dd.mm.yyyy]
            //// Initialize Chat Kernel Memory
            //var builder = Kernel.CreateBuilder();
            //builder.AddOllamaChatCompletion(
            //    modelId: _model,
            //    endpoint: new Uri(ollamaUrl)
            //);

            // Register Tools
            builder.Plugins.AddFromType<TimeTool>();
            builder.Plugins.AddFromObject(new SerperSearchTool(config.SERPER_API_KEY), "SerperSearchTool");
            builder.Plugins.AddFromObject(new ComfyUITool(config.COMFYUI_API), "ComfyUITool");
            builder.Plugins.AddFromObject(new VectorMemoryTool(_memoryService), "LongTermMemory");
            builder.Plugins.AddFromObject(new MusicGenTool(config.MUSIC_GENERATION_API), "MusicGenTool");

            _kernel = builder.Build();
        }

        public Kernel Kernel => _kernel;
        public IChatCompletionService ChatService => _kernel.GetRequiredService<IChatCompletionService>();
        public string Model => _model;
    }
}