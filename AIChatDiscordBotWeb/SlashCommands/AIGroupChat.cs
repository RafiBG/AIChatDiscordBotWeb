using AIChatDiscordBotWeb.Models;
using AIChatDiscordBotWeb.Services;
using AIChatDiscordBotWeb.SlashCommadns;
using AIChatDiscordBotWeb.Tools;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

namespace AIChatDiscordBotWeb.SlashCommands
{
    public class AIGroupChat
    {
        private readonly AIConnectionService _kernelService;
        private readonly string _systemMessage;
        private readonly List<ulong> _allowedChannels;
        private readonly ChatMemoryService _chatMemory;
        private readonly EnvConfig _config;

        private static readonly ConcurrentDictionary<ulong, SemaphoreSlim> _channelLocks = new();
        private static readonly ConcurrentDictionary<ulong, DateTime> _lastResponseTime = new(); // Cooldown tracking

        public AIGroupChat(AIConnectionService kernelService, EnvConfig config, ChatMemoryService chatMemory)
        {
            _kernelService = kernelService;
            _systemMessage = config.SYSTEM_MESSAGE;
            _allowedChannels = config.ALLOWED_GROUP_CHANNEL_IDS;
            _chatMemory = chatMemory;
            _config = config;
        }

        public async Task OnMessageCreated(DiscordClient discordClient, MessageCreateEventArgs messageEvent)
        {
            string logPrefix = $"[{DateTime.UtcNow} | {messageEvent.Channel.Name}]";

            if (messageEvent.Author.IsBot) return;  // If other bot send message do nothing

            if (_allowedChannels.Count > 0 && !_allowedChannels.Contains(messageEvent.Channel.Id))
            {
                return;
            }

            var channelLock = _channelLocks.GetOrAdd(messageEvent.Channel.Id, _ => new SemaphoreSlim(1, 1));

            if (!await channelLock.WaitAsync(500))
            {
                return;
            }

            // Get bot display name and ID
            string botName = messageEvent.Guild != null
                 ? (await messageEvent.Guild.GetMemberAsync(discordClient.CurrentUser.Id)).DisplayName
                 : discordClient.CurrentUser.Username;
            ulong botId = discordClient.CurrentUser.Id;

            // Delete short memory by user
            if (messageEvent.Message.Content.StartsWith("(forget)"))
            {
                _chatMemory.ClearUserHistory(messageEvent.Channel.Id);
                await messageEvent.Channel.SendMessageAsync("**Short memory cleared for this channel.**");
                channelLock.Release();
                return;
            }

            bool shouldRespond = false;
            string forcedORChoice = null;

            // If this is direct reply to bot's message then respond
            if (messageEvent.Message.ReferencedMessage?.Author.Id == botId)
            {
                shouldRespond = true;
                forcedORChoice = "[FORCED]";
            }
            // If the bot is mentioned/tagged respond
            else if (messageEvent.Message.MentionedUsers.Any(u => u.Id == botId))
            {
                shouldRespond = true;
                forcedORChoice = "[FORCED]";
            }
            // If it has certain keywords / phrases then respond
            //else
            //{
            //    string cleanContent = messageEvent.Message.Content.ToLower().Trim();
            //    string botNameLower = botName.ToLower();

            //    string[] aiKeywords = { "ai", "bot", "assistant", botNameLower };
            //    string[] requestIndicators = { "help", "what", "how", "tell me", "can you", "will you", "do you", "is", "are", "please", "?" };

            //    bool hasAIKeyword = aiKeywords.Any(kw => cleanContent.Contains(kw));
            //    bool hasRequest = requestIndicators.Any(ri => cleanContent.Contains(ri));

            //    // Respond if it contains AI keyword AND is more than just "hey"/"hi"
            //    if (hasAIKeyword)
            //    {
            //        if (cleanContent == "hey" || cleanContent == "hi" || cleanContent == "hello")
            //        {
            //            shouldRespond = false; // too vague
            //        }
            //        else
            //        {
            //            shouldRespond = true;
            //            forcedORChoice = "[FORCED]";
            //        }
            //    }
            //    // Also respond to standalone questions (even without "ai" keyword)
            //    else if (cleanContent.Contains("?") ||
            //             requestIndicators.Any(ri => cleanContent.StartsWith(ri) || cleanContent.Contains($" {ri}")))
            //    {
            //        shouldRespond = true;
            //        forcedORChoice = "[FORCED]";
            //    }
            //}

            // If still unsure, fall back to AI decision agent
            if (!shouldRespond)
            {
                string excerpt = GetLastChatMessages(messageEvent.Channel.Id);
                string currentMessage = $"{messageEvent.Author.Username}: {messageEvent.Message.Content}";
                string fullContextForDecision = excerpt + "\n" + currentMessage;

                shouldRespond = await AiWantsToRespond(fullContextForDecision, botName);
                forcedORChoice = " [CHOICE] ";
            }

            // Log decision for debugging
            Console.WriteLine($"{logPrefix} {forcedORChoice} '{messageEvent.Message.Content}' Respond: {shouldRespond}");

            // Release the lock if AI will not asnwer
            if (!shouldRespond)
            {
                channelLock.Release();
                return;
            }

            // Cooldown: max 1 response per 1 second per channel
            var now = DateTime.UtcNow;
            var lastResponse = _lastResponseTime.GetValueOrDefault(messageEvent.Channel.Id, DateTime.MinValue);
            if ((now - lastResponse).TotalSeconds < 1)
            {
                //Console.WriteLine($"{logPrefix} [COOLDOWN] Skipping response.");
                channelLock.Release();
                return;
            }
            _lastResponseTime[messageEvent.Channel.Id] = now;

            //Console.WriteLine($"{logPrefix} AI decided to respond.");

            DiscordMessage botReply = null;

            try
            {
                string finalMessage = messageEvent.Message.Content;
                string fileContent = null;
                byte[] imageBytes = null;

                // Attachment processing
                if (messageEvent.Message.Attachments.Count > 0)
                {
                    var attachment = messageEvent.Message.Attachments.First();

                    if (attachment.MediaType != null && attachment.MediaType.StartsWith("image/"))
                    {
                        using var http = new HttpClient();
                        imageBytes = await http.GetByteArrayAsync(attachment.Url);
                        Console.WriteLine($"**Attachment found:** Image '{attachment.FileName}' ({attachment.MediaType})");
                    }
                    else if (attachment.FileName.EndsWith(".txt"))
                    {
                        using var http = new HttpClient();
                        var bytes = await http.GetByteArrayAsync(attachment.Url);
                        fileContent = Encoding.UTF8.GetString(bytes);
                        finalMessage += $"\n\n[Attached file content {attachment.FileName}]:\n {fileContent}";
                        Console.WriteLine($"**Attachment found:** Text file '{attachment.FileName}'. Content appended to prompt.");
                    }
                    else if (attachment.FileName.EndsWith(".pdf"))
                    {
                        using var http = new HttpClient();
                        var bytes = await http.GetByteArrayAsync(attachment.Url);
                        fileContent = AIChat.ExtractPdfText(bytes);
                        finalMessage += $"\n\n[Attached file content {attachment.FileName}]:\n {fileContent}";
                        Console.WriteLine($"**Attachment found:** PDF file '{attachment.FileName}'. Content appended to prompt.");
                    }
                    else if (attachment.FileName.EndsWith(".docx"))
                    {
                        using var http = new HttpClient();
                        var bytes = await http.GetByteArrayAsync(attachment.Url);
                        fileContent = AIChat.ExtractDocxText(bytes);
                        finalMessage += $"\n\n[Attached file content {attachment.FileName}]:\n {fileContent}";
                        Console.WriteLine($"**Attachment found:** DOCX file '{attachment.FileName}'. Content appended to prompt.");
                    }
                    else
                    {
                        Console.WriteLine($"**Attachment found:** Unsupported file type '{attachment.FileName}' ({attachment.MediaType}). Ignored.");
                    }
                }

                // Update memory
                var userMessageContent = new ChatMessageContent(AuthorRole.User, finalMessage);
                userMessageContent.Items[0] = new TextContent($"{messageEvent.Author.Username}: {finalMessage}");

                if (imageBytes != null)
                {
                    string contentType = messageEvent.Message.Attachments[0].MediaType ?? "image/jpeg";
                    userMessageContent.Items.Add(new ImageContent(new ReadOnlyMemory<byte>(imageBytes), contentType));
                }

                //var usernameAndMessage = $"{messageEvent.Author.Username}: {messageEvent.Message.Content}";
                _chatMemory.AddMessage(messageEvent.Channel.Id, userMessageContent);

                // Send "Thinking" placeholder
                var builder = new DiscordMessageBuilder()
                    .WithContent("*Thinking...*")
                    .WithReply(messageEvent.Message.Id);

                botReply = await messageEvent.Channel.SendMessageAsync(builder);

                // Start streaming response
                string groupSystemMessage = _systemMessage;
                groupSystemMessage += "\n[You are in a group chat and other people can join in the talk. This is context dont response to that.]";
                var history = _chatMemory.GetUserMessages(messageEvent.Channel.Id, groupSystemMessage);
                var chatService = _kernelService.ChatService;
                var ollamaSettings = new OllamaPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() };

                StringBuilder sb = new StringBuilder();
                string aiFullResponse = "";
                var lastEdit = DateTime.UtcNow;
                bool stopStreamingUpdates = false;

                await foreach (var content in chatService.GetStreamingChatMessageContentsAsync(history, ollamaSettings, _kernelService.Kernel))
                {
                    if (content.Content != null)
                    {
                        sb.Append(content.Content);
                        aiFullResponse = sb.ToString();

                        if (aiFullResponse.Length > 1950 && !stopStreamingUpdates)
                        {
                            stopStreamingUpdates = true;
                        }

                        if (!stopStreamingUpdates && (DateTime.UtcNow - lastEdit).TotalMilliseconds >= 900)
                        {
                            lastEdit = DateTime.UtcNow;
                            await botReply.ModifyAsync(aiFullResponse + " ...");
                        }
                    }
                }

                // Final cleanup and sending
                aiFullResponse = sb.ToString();
                if (string.IsNullOrWhiteSpace(aiFullResponse))
                {
                    aiFullResponse = "Error: No response.";
                    Console.WriteLine($"{logPrefix} [ERROR]: AI model returned an empty response.");
                }

                string aiCleanedResponse = Regex.Replace(aiFullResponse, @"<think>.*?</think>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                aiCleanedResponse += VectorMemoryTool.memorySavedOrPulled;

                // Add sources (web links)
                var serperLinks = SerperSearchTool.LatestLinks;
                if (serperLinks != null && serperLinks.Count > 0)
                {
                    var linksText = string.Join("\n", serperLinks.Select(link => $"{link}"));
                    aiCleanedResponse += $"\n\n**Sources:**\n{linksText}";
                }

                // If image is generated via ComfyUI, attach it
                AttachComfyUIImageAsync(botReply, aiCleanedResponse);

                _chatMemory.AddAssistantMessage(messageEvent.Channel.Id, aiFullResponse);

                // Message splitting and final send
                var chunks = SplitMessage(aiCleanedResponse, 1950).ToList();

                if (chunks.Count > 0)
                {
                    await botReply.ModifyAsync(chunks[0]);
                }

                for (int i = 1; i < chunks.Count; i++)
                {
                    await messageEvent.Channel.SendMessageAsync(chunks[i]);
                }

                serperLinks?.Clear();
                VectorMemoryTool.memorySavedOrPulled = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR]: {ex.Message}\n{ex.StackTrace}");
                try
                {
                    await botReply?.ModifyAsync("**Error. Please try again later.**")!;
                }
                catch { /* ignore */ }
            }
            finally
            {
                channelLock.Release();
            }
        }

        private static IEnumerable<string> SplitMessage(string text, int chunkSize)
        {
            for (int i = 0; i < text.Length; i += chunkSize)
            {
                yield return text.Substring(i, Math.Min(chunkSize, text.Length - i));
            }
        }

        private async Task AttachComfyUIImageAsync(DiscordMessage botMessage, string aiResponse)
        {
            if (!ComfyUITool.IsImageGenerating) return;

            ComfyUITool.IsImageGenerating = false;

            _ = Task.Run(async () =>
            {
                try
                {
                    string outputFolder = @$"{_config.COMFYUI_IMAGE_PATH}";
                    Console.WriteLine($"[ComfyUI image] Watching for new image in: {outputFolder}");

                    string lastKnown = Directory.GetFiles(outputFolder, "*.png")
                        .OrderByDescending(f => File.GetCreationTimeUtc(f))
                        .FirstOrDefault();

                    DateTime start = DateTime.UtcNow;
                    string latestImage = null;
                    int maxTimeWait = 270;

                    while ((DateTime.UtcNow - start).TotalSeconds < maxTimeWait)
                    {
                        var files = Directory.GetFiles(outputFolder, "*.png", SearchOption.TopDirectoryOnly);
                        if (files.Length > 0)
                        {
                            var newest = files.OrderByDescending(f => File.GetCreationTimeUtc(f)).FirstOrDefault();
                            if (newest != lastKnown && File.Exists(newest))
                            {
                                latestImage = newest;
                                break;
                            }
                        }
                        await Task.Delay(3000);
                    }

                    if (string.IsNullOrEmpty(latestImage))
                    {
                        Console.WriteLine($"[ComfyUI image] No new image found after {maxTimeWait}s.");
                        return;
                    }

                    string fileName = Path.GetFileName(latestImage);

                    await botMessage.ModifyAsync(new DiscordMessageBuilder()
                        .WithContent(aiResponse)
                        .AddFile(fileName, File.OpenRead(latestImage)));

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ComfyUI image] Error attaching image: {ex.Message}");
                }
            });
        }

        // Kept for ambiguous fallback cases (e.g., "Thanks!" after bot spoke)
        private async Task<bool> AiWantsToRespond(string lastMessages, string botName)
        {
            string promptBotName = $"{botName}, AI, Bot, Assistant";

            string decisionPrompt = @" You are a decision agent. 
            Your ONLY job is to decide if the AI should respond to the **LAST message below**. 
            Ignore all earlier messages. 
            Focus ONLY on the last line. Output 'yes' or 'no'. CONTEXT: - 
            This is a Discord group chat. - The AI's name or nicknames: [" + promptBotName + @"] 
            RESPOND with 'yes' if **ANY** are true for the **LAST MESSAGE**: 
            1. It mentions the AI by name/nickname (e.g., AI, bot, assistant, """ + botName + @""") 
            2. It is a direct question (contains ?, or words like 'what', 'how', 'help', 'tell me') 
            3. It is a direct command/request (e.g., 'respond', 'talk to me', 'answer')  
            if: - The last message is just 'hi', 'hello', 'hey' with NO name/tag. - It says 'stop', 'shut up', etc.
            **LAST MESSAGE TO EVALUATE:** " + lastMessages;

            var kernel = _kernelService.Kernel;
            var chatService = _kernelService.ChatService;

            var history = new ChatHistory();
            history.AddSystemMessage(decisionPrompt);

            var settings = new OllamaPromptExecutionSettings
            {
                Temperature = 0.1f,
                TopP = 0.9f
            };

            try
            {
                var result = await chatService.GetChatMessageContentAsync(history, settings, kernel);
                string response = (result.Content ?? "").Trim().ToLower();

                if (response.StartsWith("yes") || response == "y") return true;
                if (response.StartsWith("no") || response == "n") return false;

                //Console.WriteLine($"[Decision Fallback] Unclear: '{response}' NO");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Decision Fallback] Error: {ex.Message} NO");
                return false;
            }
        }

        private string GetLastChatMessages(ulong channelId)
        {
            var history = _chatMemory.GetUserMessages(channelId, "").TakeLast(6);
            var sb = new StringBuilder();

            foreach (var message in history)
            {
                if (string.IsNullOrWhiteSpace(message.Content)) continue;

                string display = "Unknown";
                string actualContent = message.Content;

                if (message.Content.Contains(':'))
                {
                    var parts = message.Content.Split(new[] { ':' }, 2);
                    display = parts[0].Trim();
                    actualContent = parts.Length > 1 ? parts[1].Trim() : "";
                }
                else
                {
                    display = message.Role == AuthorRole.User ? "User" : "Assistant";
                }

                sb.AppendLine($"{display}: {actualContent}");
            }

            return sb.ToString().Trim();
        }
    }
}