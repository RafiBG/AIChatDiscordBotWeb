using AIChatDiscordBotWeb.Models;
using AIChatDiscordBotWeb.Services;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using OllamaSharp.Models.Chat;
using System.Text;

namespace AIChatDiscordBotWeb.SlashCommands
{
    public class MultiModel : ApplicationCommandModule
    {
        private readonly SemanticKernelService _semanticKernel;
        private readonly EnvConfig _config;

        public MultiModel(SemanticKernelService semanticKernel, EnvConfig config)
        {
            _semanticKernel = semanticKernel;
            _config = config;
        }

        [SlashCommand("ask_multi", "Ask multiple local AI models and combine their answers into one.")]
        public async Task AskMultiAsync(InteractionContext ctx,
            [Option("question", "The question you want to ask all models")] string question)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            try
            {
                var models = new[] { $"{_config.MULTI_MODEL_1}", $"{_config.MULTI_MODEL_2}", $"{_config.MULTI_MODEL_3}" };

                // Parallel asking of all models
                var tasks = models.Select(async modelName =>
                {
                    var builder = Kernel.CreateBuilder();
                    builder.AddOllamaChatCompletion(modelName, new Uri($"http://localhost:{_config.LOCAL_HOST}"));
                    var kernel = builder.Build();

                    var chatService = kernel.GetRequiredService<IChatCompletionService>();
                    var result = await chatService.GetChatMessageContentAsync(question);
                    return (Model: modelName, Response: result.Content);
                });

                var results = await Task.WhenAll(tasks);

                var sb = new StringBuilder();
                sb.AppendLine("You are a summarizer AI. Combine the following answers into a short, precise response.");
                sb.AppendLine($"Question: {question}\n");

                foreach (var r in results)
                {
                    sb.AppendLine($"{r.Model}: {r.Response}\n");
                }

                //  Final combined answer
                var chatServiceMain = _semanticKernel.ChatService;
                var summary = await chatServiceMain.GetChatMessageContentAsync(sb.ToString());

                var embed = new DiscordEmbedBuilder
                {
                    Author = new DiscordEmbedBuilder.EmbedAuthor
                    {
                        Name = Truncate(question, 247),
                        IconUrl = ctx.User.AvatarUrl
                    },
                    Title = $"Multi-Model Opinion",
                    Description = $"**Final Answer by {_config.MODEL}:**\n{summary.Content}",
                    Color = DiscordColor.Azure
                };

                foreach (var r in results)
                {
                    embed.AddField($"Model: {r.Model}\n", Truncate(r.Response, 325));
                }

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                Console.WriteLine($"@@@@@@@\nMulti-Model Opinion\n {summary.Content}\n@@@@@@@");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error {ex.Message}");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"Error: Something is wrong"));
            }
        }

        public string Truncate(string text, int maxLength = 250)
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
