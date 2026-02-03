using AIChatDiscordBotWeb.Models;
using AIChatDiscordBotWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using System.Text.RegularExpressions;

namespace AIChatDiscordBotWeb.Controllers
{
    [ApiController]
    [Route("api/slack")]
    public class SlackConnectionController : ControllerBase
    {
        private readonly AIConnectionService _skService;
        private readonly SlackMemoryService _slackMemoryService;
        private readonly EnvConfig _config;

        public SlackConnectionController(AIConnectionService skService, SlackMemoryService slackMemory, EnvConfig config)
        {
            _skService = skService;
            _slackMemoryService = slackMemory;
            _config = config;
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessSlackRequest([FromBody] SlackModel payload)
        {
            if (string.IsNullOrWhiteSpace(payload.userId))
            {
                return BadRequest(new { aiResponse = "Error: User ID is missing." });
            }

            try
            {

                // Add the incoming user message to memory
                var userMessage = new ChatMessageContent(AuthorRole.User, payload.transcription);
                _slackMemoryService.AddSlackMessage(payload.userId, userMessage);

                string slackSystemMessage = "You are a helpful Slack AI assistant. Keep responses brief and helpful.";

                // Get conversation history including the system prompt
                ChatHistory history = _slackMemoryService.GetUserMessages(payload.userId, slackSystemMessage);

                // Call the AI service (via Semantic Kernel)
                var ollamaSettings = new OllamaPromptExecutionSettings
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                };

                var result = await _skService.ChatService.GetChatMessageContentAsync(
                    history,
                    ollamaSettings,
                    _skService.Kernel
                );

                string rawResponse = result.Content ?? "";

                string cleanResponse = Regex.Replace(rawResponse, @"<think>.*?</think>", "", RegexOptions.Singleline);

                // Save the raw AI response to memory for context tracking
                _slackMemoryService.AddSlackAssistantMessage(payload.userId, rawResponse);

                //Console.WriteLine($"[SLACK SERVER] AI Response sent to {payload.userId}");

                return Ok(new
                {
                    aiResponse = cleanResponse,
                    status = "Success"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SLACK ERROR] Failure in ProcessSlackRequest: {ex.Message}");
                return StatusCode(500, new
                {
                    aiResponse = "No response. Please try again in a moment.",
                    status = "Error"
                });
            }
        }
    }
}