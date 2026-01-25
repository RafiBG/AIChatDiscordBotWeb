using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace AIChatDiscordBotWeb.SlashCommands
{
    public class HelpAndAbout : ApplicationCommandModule
    {
        [SlashCommand("help", "Show all commands")]
        public async Task HelpAsync(InteractionContext ctx)
        {
            await ctx.DeferAsync();
            string botName = ctx.Client.CurrentUser.Username;

            var embed = new DiscordEmbedBuilder
            {
                Title = "AI Bot Commands",
                Description =
                "**Slash Features**\n\n" +
                "**/ask** - Main command for everything. Ask questions, analyze files, read documents, inspect images, generate images, generate music, create code, get summaries, translate text, or let the bot remember things.\n" +
                "**/ask_multi** - Ask three different AIs same question and get summarie of all the answers.\n" +
                "**/forgetme** - Clear your personal conversation memory only.\n" +
                "**/reset** - Reset all conversations and bot context.\n" +
                "**/help** - Show this help message.\n\n" +
                "**Voice Features**\n\n" +
                "This bot can talk in voice channels using a second helper bot. Use the commands below when the helper bot is added.\n" +
                "**/join** - The talking bot joins your current voice channel and can talk with you.\n" +
                "**/leave** - The talking bot leaves the voice channel.\n\n" +
                "**Group Chat Features**\n\n" +
                $"The AI can be forced to answer when mentioned with tag @{botName} and the question, reply his answer or just chat and the AI will decide if he needs to join the chat.\n" +
                "All the features from **/ask** can be used in the group chats with the AI \n" +
                "**(forget)** - Use this word in group ai chats to make the bot forget the current conversation.\n\n",
                Color = DiscordColor.White
            };

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        [SlashCommand("about", "About the AI bot")]
        public async Task AboutAsync(InteractionContext ctx)
        {
            await ctx.DeferAsync();
            string botName = ctx.Client.CurrentUser.Username;

            var embed = new DiscordEmbedBuilder
            {
                Title = $"About {botName}",
                Description =
                $"{botName} is a local AI powered Discord bot built with ASP.NET Core. " +
                "It supports chat, file analysis, image generation, and multiple AI models.\n\n" +
                "Author: Blue Diamond (Rafi)\n" +
                "Project: https://github.com/RafiBG/AIChatDiscordBotWeb",
                Color = DiscordColor.Purple
            };

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }
    }
}
