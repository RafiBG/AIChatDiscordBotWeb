using AIChatDiscordBotWeb.Models;
using AIChatDiscordBotWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using System.Text.RegularExpressions;

namespace AIChatDiscordBotWeb.Controllers
{
    // Define the data structure Python is sending (matches the payload from main.py)
    public class PythonConnContoller
    {
        public string userId { get; set; }
        public string guildId { get; set; }
        public string channelId { get; set; }
        public string transcription { get; set; }
    }

    // This controller implements the CSHARP_AI_ENDPOINT to Python code: 
    // "http://localhost:5000/api/process_transcription"
    [ApiController]
    [Route("api")]
    public class TalkConnectionController : ControllerBase
    {
        private readonly AIConnectionService _skService;
        private readonly ChatMemoryService _memoryService;
        private readonly EnvConfig _config; // For system message

        public TalkConnectionController(AIConnectionService skService, ChatMemoryService memoryService, EnvConfig config)
        {
            _skService = skService;
            _memoryService = memoryService;
            _config = config;
        }

        [HttpPost("process_transcription")]
        public async Task<IActionResult> ProcessTranscription([FromBody] PythonConnContoller payload)
        {
            if (!ulong.TryParse(payload.userId, out ulong discordUserId))
            {
                return BadRequest(new { aiResponse = "Error: Invalid User ID format." });
            }

            var userMessageContent = new ChatMessageContent
            {
                Role = AuthorRole.User
            };
            userMessageContent.Items.Add(new TextContent(payload.transcription));

            _memoryService.AddMessage(discordUserId, userMessageContent);

            //string TalkingSystemMessage = "You are a Discord AI assistant speaking in real time. Keep answers short (10–40 words)." +
            //    " Never use emojis." +
            //    "\r\nSerperSearchTool.serper_search(query) – for current events or info you don’t know." +
            //    "\r\nTimeTool.GetCurrentTime() – for local time when relevant." +
            //    "\r\nTimeTool.GetCurrentDate() – for current date in answers or searches." +
            //    "\r\nDo not make up info. Be concise.";

            // Debug
            //Console.WriteLine($"\nTalking system message: {_config.TALK_SYSTEM_MESSAGE}\n");

            // Retrieve the full history, including the system message
            //ChatHistory history = _memoryService.GetUserMessages(discordUserId, _config.SYSTEM_MESSAGE); This line works
            ChatHistory history = _memoryService.GetUserMessages(discordUserId, _config.TALK_SYSTEM_MESSAGE);

            // Local AI run
            try
            {
                var chatService = _skService.ChatService;

                var ollamaSettings = new OllamaPromptExecutionSettings
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                };

                var result = await chatService.GetChatMessageContentAsync(
                    history,
                    ollamaSettings,
                    _skService.Kernel
                );

                string aiFullResponse = result.Content;


                // Replicate the logic to remove thinking tags and handle links
                string aiCleanedResponse = Regex.Replace(aiFullResponse, @"<think>.*?</think>", "", RegexOptions.Singleline);

                // Save the full raw AI response (including tags) for memory tracking
                _memoryService.AddAssistantMessage(discordUserId, aiFullResponse);

                Console.WriteLine($"[C# SERVER] AI Response (Cleaned): {aiCleanedResponse}");

                // Return to Python bot the response and status 
                return Ok(new
                {
                    aiResponse = aiCleanedResponse,
                    status = "Completed"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[C# ERROR] AI Processing failed: {ex.Message}");
                return StatusCode(500, new
                {
                    aiResponse = $"Error: The local AI failed to generate a response. Details: {ex.Message}",
                    status = "Error"
                });
            }
        }
    }
}