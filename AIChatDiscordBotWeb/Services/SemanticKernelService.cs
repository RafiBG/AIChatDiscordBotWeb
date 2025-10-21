using AIChatDiscordBotWeb.Models;
using AIChatDiscordBotWeb.Tools;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AIChatDiscordBotWeb.Services
{
    public class SemanticKernelService
    {
        private readonly Kernel _kernel;
        private readonly string _model;
        //private readonly ConcurrentDictionary<ulong, List<dynamic>> _memory = new();
        public SemanticKernelService(EnvConfig config)
        {
            //http://localhost:11434
            _model = config.MODEL;
            var baseUrl = new Uri($"http://localhost:{config.LOCAL_HOST}");
            var builder = Kernel.CreateBuilder();
            builder.AddOllamaChatCompletion(
                modelId: _model,
                endpoint: baseUrl
            );
            // Adding tools/plugins for the AI chatbot to use 
            builder.Plugins.AddFromType<TimeTool>();
            builder.Plugins.AddFromObject(new SerperSearchTool(config.SERPER_API_KEY),"SerperSearchTool");

            _kernel = builder.Build();
        }

        public Kernel Kernel => _kernel;
        public IChatCompletionService ChatService => _kernel.GetRequiredService<IChatCompletionService>();
        public string Model => _model;
    }
}
