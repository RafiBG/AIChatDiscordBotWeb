using AIChatDiscordBotWeb.Models;
using AIChatDiscordBotWeb.Services;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using OllamaSharp.Models.Chat;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace AIChatDiscordBotWeb.SlashCommadns
{
    public class AIChat : ApplicationCommandModule
    {
        private readonly OllamaService _ollama;
        private readonly string _systemMessage;
        private readonly List<ulong> _allowedChannels = new();

        // Per-user locks to avoid same user sending multiple concurrent requests
        private static readonly ConcurrentDictionary<ulong, SemaphoreSlim> _userLocks = new();
        private static readonly TimeSpan ModelTimeout = TimeSpan.FromSeconds(60);

        public AIChat(OllamaService ollama ,EnvConfig config)
        {
            _ollama = ollama;
            _systemMessage = config.SYSTEM_MESSAGE;
            _allowedChannels = config.ALLOWED_CHANNEL_IDS;
        }

        [SlashCommand("ask" , "Ask something to the AI")]
        public async Task AskAsync(InteractionContext ctx, [Option("message", "Your message")] string message)
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

                string Username = ctx.User.Username;
                var chatRequest = new ChatRequest
                {
                    Model = _ollama.Model,
                    Messages = new List<Message>
                    {
                        new Message {Role = "system", Content = _systemMessage},
                        new Message {Role = "user", Content = $"{Username}: {message}"}
                    }
                };

            string aifullRespone = "";
           
            await foreach (var resp in _ollama.Client.ChatAsync(chatRequest))
            {
               if (resp.Message?.Content != null)
               {
                    aifullRespone += resp.Message.Content;
               }
            }

                // Fallback in case Ollama gave nothing
                if (string.IsNullOrEmpty(aifullRespone))
                {
                    aifullRespone = "Error: No respone from AI";
                    Console.WriteLine("Error: No respone from Ollama");
                }

                Console.WriteLine($"Raw response \n\n {aifullRespone}");

                // Remove <think>...</think> from thinking models response
                string aiCleanedResponse = Regex.Replace(aifullRespone, @"<think>.*?</think>", "", RegexOptions.Singleline);

                Console.WriteLine($"\n{DateTime.Now}");
                Console.WriteLine($"=============\n {Username} asked:\n{message}\n=============\n");
                Console.WriteLine($"-----------------\n AI {_ollama.Model} responded: {aiCleanedResponse}\n-----------------\n");

                var embed = new DiscordEmbedBuilder
                {
                    Author = new DiscordEmbedBuilder.EmbedAuthor
                    {
                        Name = $"{message}",
                        IconUrl = ctx.User.AvatarUrl
                    },
                    Title = $"Model:  {_ollama.Model} \n\nResponse",
                    Description = aiCleanedResponse,
                    Color = DiscordColor.CornflowerBlue,
                };

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
            }
            // When AI is done accept new request. Even if there is error it should accept again shortly
            finally { userLock.Release(); }
        }

        [SlashCommand("help", "Show all commands")]
        public async Task HelpAsync(InteractionContext ctx)
        {
            await ctx.DeferAsync();

            var embed = new DiscordEmbedBuilder
            {
                Title = "AI Bot Commands",
                Description = "**/ask** - Talk to the AI\n" +
                              "**/help** - Show this help",
                //Description = "**/ask** - Talk to the AI\n" +
                //              "**/forgetme** - Forget your chat only\n" +
                //              "**/reset** - Reset all chats\n" +
                //              "**/help** - Show this help",
                Color = DiscordColor.White,
            };

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }
    }
}
