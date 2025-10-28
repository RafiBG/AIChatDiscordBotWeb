using AIChatDiscordBotWeb.Models;
using AIChatDiscordBotWeb.Services;
using AIChatDiscordBotWeb.Tools;
using DocumentFormat.OpenXml.Packaging;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Ollama;
using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace AIChatDiscordBotWeb.SlashCommadns
{
    public class AIChat : ApplicationCommandModule
    {
        private readonly SemanticKernelService _kernelService;
        private readonly string _systemMessage;
        private readonly List<ulong> _allowedChannels = new();
        private readonly ChatMemoryService _chatMemory;
        private readonly EnvConfig _config;

        // Per-user locks to avoid same user sending multiple concurrent requests
        private static readonly ConcurrentDictionary<ulong, SemaphoreSlim> _userLocks = new();
        private string givenFile;
        private string givenImage;
        private string webLinks;
        //private string generatedImage;

        //private static readonly TimeSpan ModelTimeout = TimeSpan.FromSeconds(60);

        public AIChat(SemanticKernelService kernelService, EnvConfig config, ChatMemoryService chatMemory)
        {
            _kernelService = kernelService;
            _systemMessage = config.SYSTEM_MESSAGE;
            _allowedChannels = config.ALLOWED_CHANNEL_IDS;
            _chatMemory = chatMemory;
            _config = config;
        }

        [SlashCommand("ask", "Ask something to the AI")]
        public async Task AskAsync(InteractionContext ctx,
            [Option("message", "Your message")] string message,
            [Option("file", "Optional file to read like pdf,txt,docx")] DiscordAttachment file = null,
            [Option("image", "Optional image to see")] DiscordAttachment image = null)
        {   // Checks if user ask in allowed channel if there is one
            if (_allowedChannels.Count > 0 && !_allowedChannels.Contains(ctx.Channel.Id))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("This channel is not allowed for AI responses.")
                    .AsEphemeral());
                return;
            }

            // Per user lock
            var userId = ctx.User.Id;
            var userLock = _userLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));

            // If the same user already has a running request
            // It helps to not break the bot 
            if (!await userLock.WaitAsync(0))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("You already have a pending request. Please wait for it to finish or try again shortly.")
                    .AsEphemeral());
                return;
            }

            try
            {
                await ctx.DeferAsync();

                string username = ctx.User.Username;
                string fileContent = null;
                byte[] imageBytes = null;

                if (file != null)
                {
                    givenFile = $"Given file to read: {file.FileName}";
                    using var http = new HttpClient();
                    var bytes = await http.GetByteArrayAsync(file.Url);

                    if (file.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                    {
                        fileContent = Encoding.UTF8.GetString(bytes);
                    }
                    else if (file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        fileContent = ExtractPdfText(bytes);
                    }
                    else if (file.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                    {
                        fileContent = ExtractDocxText(bytes);
                    }
                    else
                    {
                        fileContent = $"Unsopported file type: {file.FileName}";
                    }
                }

                string finalMessage = message;

                if (!string.IsNullOrWhiteSpace(fileContent))
                {
                    finalMessage += $"\n\n[Attached file content {file.FileName}]:\n {fileContent}";
                    Console.WriteLine($"\nGiven file to read: {file.FileName}");
                    //// Can download the file in console manually (interesting)
                    //Console.WriteLine($"User file URL given to AI: {file.Url}");
                }

                var userMessageContent = new ChatMessageContent();

                userMessageContent.Items.Add(new TextContent(finalMessage));

                // If there's an image, download bytes and add an ImageContent part
                if (image != null)
                {
                    using var httpImage = new HttpClient();
                    imageBytes = await httpImage.GetByteArrayAsync(image.Url);
                    givenImage = image.Url;

                    string contentType = image.MediaType ?? "image/jpeg";
                    var imageContent = new ImageContent(new ReadOnlyMemory<byte>(imageBytes), contentType);

                    userMessageContent.Items.Add(imageContent);

                    Console.WriteLine($"\nGiven image to see: {image.FileName}");
                }

                _chatMemory.AddMessage(userId, userMessageContent);
                var history = _chatMemory.GetUserMessages(userId, _systemMessage);
                var chatService = _kernelService.ChatService;

                var ollamaSettings = new OllamaPromptExecutionSettings
                {
                    // Tells the model it can use any registered function/tool/plugin
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                };

                string aiFullResponse = "";

                var embedEmpty = new DiscordEmbedBuilder
                {
                    Author = new DiscordEmbedBuilder.EmbedAuthor
                    {
                        Name = Truncate(message, 247),
                        IconUrl = ctx.User.AvatarUrl
                    },
                    Title = $"Model:  {_kernelService.Model} \n {givenFile} \n\nResponse",
                    Description = "Thinking...",
                    Color = DiscordColor.CornflowerBlue,
                };
                var sendMessage = await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embedEmpty));
                var sb = new StringBuilder();
                var lastEdit = DateTime.UtcNow;


                await foreach (var content in chatService.GetStreamingChatMessageContentsAsync(history, ollamaSettings,_kernelService.Kernel))
                {
                    if (content.Content != null)
                    {
                        sb.Append(content.Content);
                        aiFullResponse = sb.ToString();
                        // Throttle edits
                        if ((DateTime.UtcNow - lastEdit).TotalMilliseconds >= 1100)
                        {
                            lastEdit = DateTime.UtcNow;

                            var embedUpdate = new DiscordEmbedBuilder
                            {
                                Author = new DiscordEmbedBuilder.EmbedAuthor
                                {
                                    Name = Truncate(message, 247),
                                    IconUrl = ctx.User.AvatarUrl
                                },
                                Title = $"Model: {_kernelService.Model}\n {givenFile}\n\nResponse",
                                Description = aiFullResponse,
                                Color = DiscordColor.CornflowerBlue
                            };

                            await sendMessage.ModifyAsync(embed: embedUpdate.Build());
                        }
                    }
                }
                aiFullResponse = sb.ToString();
                // Fallback in case Ollama gave nothing
                if (string.IsNullOrEmpty(aiFullResponse))
                {
                    aiFullResponse = "Error: No respone from AI";
                    Console.WriteLine("Error: No respone from Ollama");
                }

                Console.WriteLine($"Raw response (check thinking models) \n\n {aiFullResponse}");

                var serperLinks = SerperSearchTool.LatestLinks;

                if (serperLinks != null && serperLinks.Count > 0)
                {
                    var linksText = string.Join("\n", serperLinks.Select(link => $"{link}"));
                    webLinks = $"\n**Sources**\n {linksText}";
                }

                // Removes <think>...</think> from thinking models response
                string aiCleanedResponse = Regex.Replace(aiFullResponse, @"<think>.*?</think>", "", RegexOptions.Singleline);

                _chatMemory.AddAssistantMessage(userId, aiFullResponse);

                Console.WriteLine($"\n{DateTime.Now}");
                Console.WriteLine($"=============\n {username} asked:\n{message}\n=============\n");
                Console.WriteLine($"-----------------\n AI {_kernelService.Model} responded: \n{aiCleanedResponse}\n-----------------\n");

                var embedFinal = new DiscordEmbedBuilder
                {
                    Author = new DiscordEmbedBuilder.EmbedAuthor
                    {
                        Name = Truncate(message,247),
                        IconUrl = ctx.User.AvatarUrl
                    },
                    Title = $"Model: {_kernelService.Model}\n{givenFile}\n\nResponse",
                    Description = $"{aiCleanedResponse}\n{webLinks}",
                    Color = DiscordColor.CornflowerBlue
                };

                // Update message with the AI text first
                await sendMessage.ModifyAsync(embed: embedFinal.Build());

                if (ComfyUITool.IsImageGenerating)
                {
                    ComfyUITool.IsImageGenerating = false;

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            string outputFolder = @$"{_config.COMFYUI_IMAGE_PATH}";
                            Console.WriteLine($"[ComfyUI image] Watching for new image in: {outputFolder}");

                            // Remember the most recent file before generation starts
                            string lastKnown = Directory.GetFiles(outputFolder, "*.png")
                                .OrderByDescending(f => File.GetCreationTimeUtc(f))
                                .FirstOrDefault();

                            DateTime start = DateTime.UtcNow;
                            string latestImage = null;
                            int maxTimeWait = 270;
                            // Check every 3 sec for up to 270 sec/4.5 min
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
                                // Check every 3 seconds
                                await Task.Delay(3000);
                            }

                            if (string.IsNullOrEmpty(latestImage))
                            {
                                Console.WriteLine($"[Gen image] No new image found after {maxTimeWait}s.");
                                return;
                            }

                            string fileName = Path.GetFileName(latestImage);

                            var embedWithImage = new DiscordEmbedBuilder
                            {
                                Author = new DiscordEmbedBuilder.EmbedAuthor
                                {
                                    Name = Truncate(message, 247),
                                    IconUrl = ctx.User.AvatarUrl
                                },
                                Title = $"Model: {_kernelService.Model}\n{givenFile}\n\nResponse",
                                Description = $"{aiCleanedResponse}\n{webLinks}",
                                ImageUrl = $"attachment://{fileName}",
                                Color = DiscordColor.CornflowerBlue
                            };

                            // Edit the last message to include the image
                            await sendMessage.ModifyAsync(new DiscordMessageBuilder()
                                .AddEmbed(embedWithImage)
                                .AddFile(fileName, File.OpenRead(latestImage)));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error while attaching generated image: {ex.Message}");
                        }
                    });
                }

                serperLinks.Clear();
            }
            // When AI is done accept new request. Even if there is error it should accept again shortly
            finally { userLock.Release(); }
        }

        [SlashCommand("forgetme", "Forget my chat history")]
        public async Task ForgetMeAsync(InteractionContext ctx)
        {
            _chatMemory.ClearUserHistory(ctx.User.Id);
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                .WithContent("Your chat history has been cleared.")
                .AsEphemeral());
        }

        [SlashCommand("reset", "Reset all chats histories")]
        public async Task ResetAsync(InteractionContext ctx)
        {
            _chatMemory.ResetAll();
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                .WithContent("All chat histories have been reset.")
                .AsEphemeral());
        }

        [SlashCommand("help", "Show all commands")]
        public async Task HelpAsync(InteractionContext ctx)
        {
            await ctx.DeferAsync();

            var embed = new DiscordEmbedBuilder
            {
                Title = "AI Bot Commands",
                Description = "**/ask** - Ask the AI: Upload text (PDF, DOCX, TXT) or an image.\n" +
                              "**/forgetme** - Forget your chat only\n" +
                              "**/reset** - Reset all chats\n" +
                              "**/help** - Show this help",
                Color = DiscordColor.White,
            };

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        private string ExtractPdfText(byte[] pdfBytes)
        {
            using var ms = new MemoryStream(pdfBytes);
            using var pdf = PdfDocument.Open(ms);
            var sb = new StringBuilder();
            foreach (var page in pdf.GetPages())
            {
                sb.Append(page.Text);
            }
            return sb.ToString();
        }

        private string ExtractDocxText(byte[] docxBytes)
        {
            using var ms = new MemoryStream(docxBytes);
            using var wordDoc = WordprocessingDocument.Open(ms, false);
            var sb = new StringBuilder();
            foreach (var text in wordDoc.MainDocumentPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>())
    {
                sb.Append(text.Text);
            }
            return sb.ToString();
        }
        // If message is too long cut it 
        // This is done to not crash the app
        private string Truncate(string text, int maxLength = 250)
        {
            if (text.Length <= maxLength)
            {
                return text;
            }
            else
            {
                // Otherwise, cut it to the max length and add "..."
                return text.Substring(0, maxLength - 3) + "...";
            }
        }
    }
}
