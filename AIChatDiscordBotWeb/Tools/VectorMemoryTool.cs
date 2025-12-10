using Microsoft.SemanticKernel;
using System.ComponentModel;
using AIChatDiscordBotWeb.Services;

namespace AIChatDiscordBotWeb.Tools
{
    public class VectorMemoryTool
    {
        private readonly KernelMemoryService _memoryService;
        public static string memorySavedOrPulled = null;

        public VectorMemoryTool(KernelMemoryService memoryService)
        {
            _memoryService = memoryService;
        }

        [KernelFunction("save_memory")]
        [Description("Saves a permanent fact, preference, or specific detail about the user. Use this when the user says 'remember that' or shares personal info.")]
        public async Task<string> SaveMemoryAsync(
            [Description("The fact or information to save.")] string info,
            // The User ID is retrieved from the system context in the main application flow
            [Description("The User ID provided in the system context.")] string userId)
        {
            Console.WriteLine("\n[Tool] Vector memory saved");
            await _memoryService.SaveIndividualMemoryAsync(userId, info);
            memorySavedOrPulled = "[Memory saved]";
            return "Memory saved successfully.";
        }

        [KernelFunction("recall_memory")]
        [Description("Searches long-term memory to find information about the user or uploaded documents.")]
        public async Task<string> RecallMemoryAsync(
            [Description("The specific question to look up.")] string query,
            // The User ID is retrieved from the system context in the main application flow
            [Description("The User ID provided in the system context.")] string userId)
        {
            Console.WriteLine("\n[Tool] Vector memory recalled");
            var answer = await _memoryService.PullIndividualMemoryAsync(userId, query);
            memorySavedOrPulled = "[Memory recalled)";
            // Gets aswer if there is relevant info, otherwise a default message. Its like IF statement
            return answer ?? "No relevant information found in memory.";
        }
    }
}
