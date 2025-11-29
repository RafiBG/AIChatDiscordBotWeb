using AIChatDiscordBotWeb.Models;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI.Ollama;
using Microsoft.KernelMemory.DocumentStorage.DevTools;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Microsoft.KernelMemory.MemoryStorage.DevTools;

namespace AIChatDiscordBotWeb.Services
{
    /// <summary>
    /// Provides long-term vector memory management for the bot using Semantic Kernel Memory. This service configures
    /// and exposes persistent memory storage and retrieval capabilities for user-specific data.
    /// </summary>

    public class KernelMemoryService
    {
        private readonly IKernelMemory _memory;

        public IKernelMemory RawMemory => _memory;
        public KernelMemoryService(EnvConfig config)
        {
            string longMemoryStorageDir = "BotMemoryStorage";

            if (!Directory.Exists(longMemoryStorageDir))
            {
                Directory.CreateDirectory(longMemoryStorageDir);
                Console.WriteLine($"Created persistent memory directory: {longMemoryStorageDir}");

            }

            // Ensures memories are saved to the "BotMemoryStorage" folder on your hard drive.
            _memory = new KernelMemoryBuilder()
                .WithOllamaTextGeneration(new OllamaConfig
                {
                    Endpoint = $"http://localhost:{config.LOCAL_HOST}",
                    TextModel = new OllamaModelConfig(config.MODEL)
                })
                .WithOllamaTextEmbeddingGeneration(new OllamaConfig
                {
                    Endpoint = $"http://localhost:{config.LOCAL_HOST}",
                    EmbeddingModel = new OllamaModelConfig("embeddinggemma:300m")
                })
                .WithSimpleFileStorage(new SimpleFileStorageConfig
                {
                    Directory = longMemoryStorageDir,
                    StorageType = FileSystemTypes.Disk
                })
                .WithSimpleVectorDb(new SimpleVectorDbConfig
                {
                    Directory = longMemoryStorageDir,
                    StorageType = FileSystemTypes.Disk
                })
                .Build<MemoryServerless>();
        }

        public async Task SaveMemoryAsync(string userId, string fact)
        {
            // Tag the memory with "user_id" so we don't mix up different users
            var tags = new TagCollection { { "user_id", userId }, { "type", "fact" } };

            // Use a generic document ID so specific facts don't overwrite each other unless intended
            await _memory.ImportTextAsync(fact, tags: tags);
        }

        public async Task<string> PullMemoryAsync(string userId, string question)
        {
            // Only look at memories belonging to THIS user
            var filter = new MemoryFilter().ByTag("user_id", userId);

            // MinRelevance 0.6 ensures we don't hallucinate if no memory is found
            // Try lowering this value to 0.5 or 0.4 to allow for less exact matches!
            var answer = await _memory.AskAsync(question, filter: filter, minRelevance: 0.4);

            if (answer.NoResult) return null;
            return answer.Result;
        }
    }
}