using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace AIChatDiscordBotWeb.SlashCommands
{
    public class Games : ApplicationCommandModule
    {
        // Define an enum for choices.
        // DSharpPlus automatically turns these into a dropdown for the user.
        public enum RpsChoice
        {
            [ChoiceName("Rock")]
            Rock = 1,
            [ChoiceName("Paper")]
            Paper = 2,
            [ChoiceName("Scissors")]
            Scissors = 3
        }

        [SlashCommand("rps", "Play a game of Rock Paper Scissors!")]
        public async Task RpsCommand(InteractionContext ctx, 
            [Option("choice", "Your move")] RpsChoice userChoice)
        {
            var random = new Random();
            RpsChoice botChoice = (RpsChoice)random.Next(1, 4);

            string resultMessage;
            DiscordColor embedColor;

            if (userChoice == botChoice)
            {
                resultMessage = "It's a **Tie**! 🤝";
                embedColor = DiscordColor.Gray;
            }
            else if ((userChoice == RpsChoice.Rock && botChoice == RpsChoice.Scissors) ||
                     (userChoice == RpsChoice.Paper && botChoice == RpsChoice.Rock) ||
                     (userChoice == RpsChoice.Scissors && botChoice == RpsChoice.Paper))
            {
                resultMessage = "You **Won**! 🎉";
                embedColor = DiscordColor.Green;
            }
            else
            {
                resultMessage = "You **Lost**! 💀";
                embedColor = DiscordColor.Red;
            }

            var embed = new DiscordEmbedBuilder
            {
                Title = "Rock Paper Scissors",
                Description = $"**You:** {userChoice}\n**Bot:** {botChoice}\n\n{resultMessage}",
                Color = embedColor
            };

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, 
                new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }
    }
}