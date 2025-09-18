using AIChatDiscordBotWeb.Models;
using AIChatDiscordBotWeb.Services;
using DocumentFormat.OpenXml.Packaging;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using OllamaSharp.Models.Chat;
using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace AIChatDiscordBotWeb.SlashCommadns
{
    public class AIChat : ApplicationCommandModule
    {
        private readonly OllamaService _ollama;
        private readonly string _systemMessage;
        private readonly List<ulong> _allowedChannels = new();
        private readonly ChatMemoryService _chatMemory;

        // Per-user locks to avoid same user sending multiple concurrent requests
        private static readonly ConcurrentDictionary<ulong, SemaphoreSlim> _userLocks = new();
        private string givenFile;

        //private static readonly TimeSpan ModelTimeout = TimeSpan.FromSeconds(60);

        public AIChat(OllamaService ollama, EnvConfig config, ChatMemoryService chatMemory)
        {
            _ollama = ollama;
            _systemMessage = config.SYSTEM_MESSAGE;
            _allowedChannels = config.ALLOWED_CHANNEL_IDS;
            _chatMemory = chatMemory;
        }

        [SlashCommand("ask", "Ask something to the AI")]
        public async Task AskAsync(InteractionContext ctx, 
            [Option("message", "Your message")] string message,
            [Option("attachment", "Optional file to read like pdf,txt,docx")] DiscordAttachment attachment = null)
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

                if (attachment != null)
                {
                    givenFile = $"Given file to read: {attachment.FileName}";
                    using var http = new HttpClient();
                    var bytes = await http.GetByteArrayAsync(attachment.Url);

                    if (attachment.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                    {
                        fileContent = Encoding.UTF8.GetString(bytes);
                    }
                    else if (attachment.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        fileContent = ExtractPdfText(bytes);
                    }
                    else if (attachment.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                    {
                        fileContent = ExtractDocxText(bytes);
                    }
                    else
                    {
                        fileContent = $"Unsopported file type: {attachment.FileName}";
                    }
                }

                string finalMessage = message;

                if (!string.IsNullOrWhiteSpace(fileContent))
                {
                    finalMessage += $"\n\n[Attached file content {attachment.FileName}]:\n {fileContent}";
                    Console.WriteLine($"\nGiven file to read: {attachment.FileName}");
                    //// Can download the file in console manually (interesting)
                    //Console.WriteLine($"User file URL given to AI: {attachment.Url}");
                }

                _chatMemory.AddUserMessage(userId, username, finalMessage, _systemMessage);

                var chatRequest = new ChatRequest
                {
                    Model = _ollama.Model,
                    Messages = _chatMemory.GetUserMessages(userId, _systemMessage)
                };

                string aifullRespone = "";

                var embedEmpty = new DiscordEmbedBuilder
                {
                    Author = new DiscordEmbedBuilder.EmbedAuthor
                    {
                        Name = $"{message}",
                        IconUrl = ctx.User.AvatarUrl
                    },
                    Title = $"Model:  {_ollama.Model} \n {givenFile} \n\nResponse",
                    Description = "Thinking...",
                    Color = DiscordColor.CornflowerBlue,
                };
                var sendMessage = await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embedEmpty));
                var sb = new StringBuilder();
                var lastEdit = DateTime.UtcNow;


                await foreach (var resp in _ollama.Client.ChatAsync(chatRequest))
                {
                    if (resp.Message?.Content != null)
                    {
                        sb.Append(resp.Message.Content);
                        aifullRespone = sb.ToString();
                        // Throttle edits
                        if ((DateTime.UtcNow - lastEdit).TotalMilliseconds >= 800)
                        {
                            lastEdit = DateTime.UtcNow;

                            var embedUpdate = new DiscordEmbedBuilder
                            {
                                Author = new DiscordEmbedBuilder.EmbedAuthor
                                {
                                    Name = message,
                                    IconUrl = ctx.User.AvatarUrl
                                },
                                Title = $"Model: {_ollama.Model}\n {givenFile}\n\nResponse",
                                Description = aifullRespone,
                                Color = DiscordColor.CornflowerBlue
                            };

                            await sendMessage.ModifyAsync(embed: embedUpdate.Build());
                        }
                    }
                }
                aifullRespone = sb.ToString();
                // Fallback in case Ollama gave nothing
                if (string.IsNullOrEmpty(aifullRespone))
                {
                    aifullRespone = "Error: No respone from AI";
                    Console.WriteLine("Error: No respone from Ollama");
                }

                Console.WriteLine($"Raw response (check thinking models) \n\n {aifullRespone}");

                // Remove <think>...</think> from thinking models response
                string aiCleanedResponse = Regex.Replace(aifullRespone, @"<think>.*?</think>", "", RegexOptions.Singleline);

                _chatMemory.AddAssistantMessage(userId, aiCleanedResponse);

                Console.WriteLine($"\n{DateTime.Now}");
                Console.WriteLine($"=============\n {username} asked:\n{message}\n=============\n");
                Console.WriteLine($"-----------------\n AI {_ollama.Model} responded: \n{aiCleanedResponse}\n-----------------\n");

                var embedFinal = new DiscordEmbedBuilder
                {
                    Author = new DiscordEmbedBuilder.EmbedAuthor
                    {
                        Name = $"{message}",
                        IconUrl = ctx.User.AvatarUrl
                    },
                    Title = $"Model:  {_ollama.Model} \n {givenFile} \n\nResponse",
                    Description = aiCleanedResponse,
                    Color = DiscordColor.CornflowerBlue,
                };

                await sendMessage.ModifyAsync(embed: embedFinal.Build());
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
                Description = "**/ask** - Talk to the AI\n" +
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
    }
}
