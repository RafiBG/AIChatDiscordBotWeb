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
    public class SemanticKernelService
    {
        private readonly Kernel _kernel;
        private readonly string _model;
        private readonly KernelMemoryService _memoryService;

        public IKernelMemory Memory => _memoryService.RawMemory;

        // Accept KernelMemoryService as a dependency instead of building memory here
        public SemanticKernelService(EnvConfig config, KernelMemoryService memoryService)
        {
            _model = config.MODEL;
            string ollamaUrl = $"http://localhost:{config.LOCAL_HOST}";
            _memoryService = memoryService;

            // Initialize Chat Kernel Memory
            var builder = Kernel.CreateBuilder();
            builder.AddOllamaChatCompletion(
                modelId: _model,
                endpoint: new Uri(ollamaUrl)
            );

            // Register Tools
            builder.Plugins.AddFromType<TimeTool>();
            builder.Plugins.AddFromObject(new SerperSearchTool(config.SERPER_API_KEY), "SerperSearchTool");
            builder.Plugins.AddFromObject(new ComfyUITool(config.COMFYUI_API), "ComfyUITool");
            builder.Plugins.AddFromObject(new VectorMemoryTool(_memoryService), "LongTermMemory");

            _kernel = builder.Build();
        }

        public Kernel Kernel => _kernel;
        public IChatCompletionService ChatService => _kernel.GetRequiredService<IChatCompletionService>();
        public string Model => _model;
    }
}